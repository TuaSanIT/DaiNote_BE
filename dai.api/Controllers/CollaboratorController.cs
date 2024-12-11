using dai.api.Services.ServicesAPI;
using dai.core.DTO.Collaborator;
using dai.core.Models;
using dai.dataAccess.DbContext;
using dai.dataAccess.IRepositories;
using dai.dataAccess.Repositories;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;

namespace dai.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CollaboratorController : ControllerBase
    {
        private readonly ICollaboratorRepository _collaboratorRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;

        public CollaboratorController(ICollaboratorRepository collaboratorRepository, IUserRepository userRepository, IEmailService emailService)
        {
            _collaboratorRepository = collaboratorRepository;
            _userRepository = userRepository;
            _emailService = emailService;
        }

        // Lấy user id từ JWT token
        private Guid UserIdFromToken()
        {
            var userIdClaim = User.FindFirst("sub")?.Value;
            return Guid.TryParse(userIdClaim, out Guid userId) ? userId : Guid.Empty;
        }

        [HttpPost("invite")]
        public async Task<IActionResult> InviteCollaborator([FromBody] CollaboratorInvitationDTO invitationDTO)
        {
            if (invitationDTO?.Emails == null || invitationDTO.Emails.Count == 0)
            {
                return BadRequest(new { Message = "At least one email is required." });
            }

            var board = await _collaboratorRepository.GetBoardByIdAsync(invitationDTO.BoardId);
            if (board == null)
            {
                return NotFound(new { Message = "Board not found!" });
            }

            var senderUser = await _userRepository.GetUserByIdAsync(invitationDTO.SenderUserId);
            if (senderUser == null)
            {
                return Unauthorized(new { Message = "Sender not found in the database." });
            }

            var validEmails = new List<string>();
            var alreadyCollaborators = new List<string>();
            var invalidEmails = new List<string>();

            foreach (var email in invitationDTO.Emails)
            {
                if (IsValidEmail(email))
                {
                    var user = await _userRepository.GetUserByEmailAsync(email);
                    if (user != null)
                    {
                        var existingCollaborator = await _collaboratorRepository.GetCollaboratorAsync(user.Id, invitationDTO.BoardId);

                        if (existingCollaborator != null)
                        {
                            // Cho phép gửi lại nếu trạng thái là "Pending"
                            if (existingCollaborator.Permission != "Pending")
                            {
                                alreadyCollaborators.Add(email);
                            }
                            else
                            {
                                validEmails.Add(email); // Cho phép gửi lại lời mời
                            }
                        }
                        else
                        {
                            validEmails.Add(email);
                        }
                    }
                    else
                    {
                        invalidEmails.Add(email);
                    }
                }
                else
                {
                    invalidEmails.Add(email);
                }
            }

            if (alreadyCollaborators.Count > 0)
            {
                return BadRequest(new { Message = "Some users are already collaborators with active permissions.", AlreadyCollaborators = alreadyCollaborators });
            }

            if (validEmails.Count == 0)
            {
                return BadRequest(new { Message = "No valid emails to invite.", InvalidEmails = invalidEmails });
            }

            var invitationCode = Guid.NewGuid();
            var invitation = new CollaboratorInvitationModel
            {
                Invitaion_Code = invitationCode,
                SenderUserId = invitationDTO.SenderUserId,
                Emails = validEmails,
                Status = "Pending"
            };

            await _collaboratorRepository.CreateInvitationAsync(invitation);

            foreach (var email in validEmails)
            {
                var user = await _userRepository.GetUserByEmailAsync(email);
                var collaborator = new CollaboratorModel
                {
                    User_Id = user.Id,
                    Board_Id = invitationDTO.BoardId,
                    Invitation_Code = invitationCode,
                    Permission = "Pending"
                };

                // Cập nhật collaborator nếu đã tồn tại với trạng thái "Pending"
                await _collaboratorRepository.AddOrUpdateCollaboratorAsync(collaborator);
            }

            var emailTasks = validEmails
                .Select(email => _emailService.SendInvitationEmailAsync(email, board.Id, invitationCode.ToString(), invitationDTO.SenderUserId))
                .ToList();

            try
            {
                await Task.WhenAll(emailTasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Some invitations failed to send.", Error = ex.Message });
            }

            return Ok(new { Message = "Invitations sent successfully!" });
        }

        [HttpPost("check")]
        public async Task<IActionResult> CheckCollaborator([FromBody] CollaboratorCheckDTO checkDTO)
        {
            if (string.IsNullOrWhiteSpace(checkDTO.Email) || checkDTO.BoardId == Guid.Empty)
            {
                return BadRequest(new { Message = "Email and BoardId are required." });
            }

            // Kiểm tra email có tồn tại trong hệ thống không
            var user = await _userRepository.GetUserByEmailAsync(checkDTO.Email);
            if (user == null)
            {
                return NotFound(new { Message = "User not found in the system." });
            }

            // Kiểm tra xem user có phải là Collaborator của Board
            var collaborator = await _collaboratorRepository.GetCollaboratorByBoardIdAndUserIdAsync(checkDTO.BoardId, user.Id);
            if (collaborator != null)
            {
                if (collaborator.Permission == "Pending")
                {
                    return Ok(new
                    {
                        isCollaborator = false,
                        canBeInvited = true,
                        message = "User is in pending state and can be invited again."
                    });
                }

                return Ok(new
                {
                    isCollaborator = true,
                    canBeInvited = false,
                    message = "User is already a collaborator or has accepted the invitation."
                });
            }

            return Ok(new
            {
                isCollaborator = false,
                canBeInvited = true,
                message = "User is not a collaborator and can be invited."
            });
        }


        [HttpGet("check-user-in-board")]
        public async Task<IActionResult> CheckIfUserIsCollaborator(string code, Guid userId)
        {
            if (!Guid.TryParse(code, out Guid invitationCode))
            {
                return BadRequest("Invalid invitation code.");
            }

            var invitation = await _collaboratorRepository.GetInvitationByCodeAsync(invitationCode);
            if (invitation == null)
            {
                return NotFound("Invitation not found.");
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null || !invitation.Emails.Contains(user.Email))
            {
                return Unauthorized("You are not authorized to check this invitation.");
            }

            // Kiểm tra nếu người dùng đã là cộng tác viên
            var collaborator = await _collaboratorRepository.GetCollaboratorByInvitationCodeAndUserIdAsync(invitationCode, userId);
            if (collaborator != null && collaborator.Permission != "Pending")
            {
                return Ok(new
                {
                    isCollaborator = true,
                    boardId = collaborator.Board_Id
                });
            }

            return Ok(new { isCollaborator = false });
        }


        [HttpPost("accept-invitation")]
        public async Task<IActionResult> AcceptInvitation(string code, Guid userId)
        {
            if (!Guid.TryParse(code, out Guid invitationCode))
            {
                return BadRequest("Invalid invitation code.");
            }

            var invitation = await _collaboratorRepository.GetInvitationByCodeAsync(invitationCode);
            if (invitation == null)
            {
                return NotFound("Invitation not found.");
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null || !invitation.Emails.Contains(user.Email))
            {
                return Unauthorized("You are not authorized to accept this invitation.");
            }

            var collaborator = await _collaboratorRepository.GetCollaboratorByInvitationCodeAndUserIdAsync(invitationCode, userId);
            if (collaborator == null)
            {
                return NotFound("Collaborator not found.");
            }

            collaborator.Permission = "Editor";
            await _collaboratorRepository.UpdateCollaboratorAsync(collaborator);

            invitation.Status = "Accepted";
            await _collaboratorRepository.UpdateInvitationAsync(invitation);

            return Ok("Invitation accepted.");
        }

        [HttpGet("invitation-info")]
        public async Task<IActionResult> GetInvitationInfo(string code, Guid userId)
        {
            if (!Guid.TryParse(code, out Guid invitationCode))
            {
                return BadRequest("Invalid invitation code.");
            }

            var invitation = await _collaboratorRepository.GetInvitationByCodeAsync(invitationCode);
            if (invitation == null)
            {
                return NotFound("Invitation not found.");
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null || !invitation.Emails.Contains(user.Email))
            {
                return Unauthorized("You are not authorized to view this invitation.");
            }

            var collaborator = await _collaboratorRepository.GetCollaboratorByInvitationCodeAndUserIdAsync(invitationCode, userId);
            if (collaborator == null)
            {
                return NotFound("Collaborator not found.");
            }

            var board = await _collaboratorRepository.GetBoardByIdAsync(collaborator.Board_Id);
            if (board == null)
            {
                return NotFound("Board not found.");
            }

            var inviter = await _userRepository.GetUserByIdAsync(invitation.SenderUserId);

            return Ok(new
            {
                InvitationCode = invitation.Invitaion_Code,
                InviterName = inviter?.FullName ?? "Unknown",
                InviterEmail = inviter?.Email ?? "Unknown",
                BoardName = (await _collaboratorRepository.GetBoardByInvitationCodeAsync(invitationCode))?.Name ?? "Unknown",
                BoardId = board.Id,
                Status = invitation.Status
            });
        }

        //Nghia
        [HttpGet("{boardId}")]
        public async Task<IActionResult> GetCollaboratorsByBoardId(Guid boardId)
        {
            var collaborators = await _collaboratorRepository.GetCollaboratorsByBoardIdAsync(boardId);

            if (collaborators == null || !collaborators.Any())
            {
                return NotFound("No collaborators found for the specified board ID.");
            }

            return Ok(collaborators);
        }


        [HttpGet("is-collaborator")]
        public async Task<IActionResult> IsUserCollaborator(Guid boardId, Guid userId)
        {
            if (boardId == Guid.Empty || userId == Guid.Empty)
            {
                return BadRequest("Invalid boardId or userId");
            }

            var isCollaborator = await _collaboratorRepository.IsUserCollaboratorAsync(boardId, userId);

            return Ok(isCollaborator);
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var mailAddress = new MailAddress(email);
                return mailAddress.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public class CollaboratorCheckDTO
        {
            public string Email { get; set; }
            public Guid BoardId { get; set; }
        }

    }

}