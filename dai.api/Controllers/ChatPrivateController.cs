using dai.api.Hubs;
using dai.api.Services.ServiceExtension;
using dai.core.DTO.Chat;
using dai.core.Models;
using dai.core.Models.Entities;
using dai.dataAccess.DbContext;
using Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace dai.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatPrivateController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<UserModel> _userManager;
        private readonly IHubContext<ChatHub> hubContext;
        private readonly AzureBlobService _storageService;


        public ChatPrivateController(AppDbContext context, UserManager<UserModel> userManager, IHubContext<ChatHub> hubContext, AzureBlobService storageService)
        {
            _context = context;
            _userManager = userManager;
            this.hubContext = hubContext;
            _storageService = storageService;
        }

        private Guid? GetUserIdFromHeader()
        {
            if (Request.Headers.TryGetValue("UserId", out var userIdString) && Guid.TryParse(userIdString, out var userId))
            {
                Console.WriteLine($"UserId from header: {userId}");
                return userId;
            }
            Console.WriteLine("UserId is missing or invalid in header");
            return null;
        }


        private async Task<bool> IsUserInBoardAsync(Guid boardId, Guid userId)
        {
            Console.WriteLine($"Checking permissions for UserId: {userId}, BoardId: {boardId}");

            var board = await _context.Boards
                .Include(b => b.Workspace)
                .ThenInclude(w => w.User)
                .Include(b => b.Collaborators)
                .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null)
            {
                Console.WriteLine("Board not found");
                return false;
            }

            var isAuthorized = board.Workspace.UserId == userId || board.Collaborators.Any(c => c.User_Id == userId);
            Console.WriteLine($"IsAuthorized: {isAuthorized}");
            return isAuthorized;
        }

        [HttpGet("messages")]
        public async Task<IActionResult> GetPrivateMessages(Guid senderUserId, Guid receiverUserId, Guid boardId)
        {
            // Validate sender and receiver permissions
            if (!await IsUserInBoardAsync(boardId, senderUserId) || !await IsUserInBoardAsync(boardId, receiverUserId))
            {
                return Unauthorized(new { success = false, message = "You are not authorized to view this chat." });
            }

            // Fetch messages
            var privateMessages = await _context.ChatPrivate
                .Where(c => (c.SenderUserId == senderUserId && c.ReceiverUserId == receiverUserId) ||
                            (c.SenderUserId == receiverUserId && c.ReceiverUserId == senderUserId))
                .OrderBy(c => c.NotificationDateTime)
                .Select(c => new
                {
                    c.ChatPrivateId,
                    c.SenderUserId,
                    c.ReceiverUserId,
                    c.Message,
                    c.ImageChat,
                    NotificationDateTime = c.NotificationDateTime.ToString("HH:mm dd/MM/yyyy"),
                    SenderUser = new
                    {
                        c.SenderUser.Email,
                        c.SenderUser.AvatarImage,
                        c.SenderUser.FullName
                    },
                    ReceiverUser = new
                    {
                        c.ReceiverUser.Email,
                        c.ReceiverUser.AvatarImage,
                        c.ReceiverUser.FullName
                    }
                })
                .ToListAsync();

            return Ok(privateMessages);
        }

        // Start a private chat between two users
        [HttpPost("start")]
        public async Task<IActionResult> StartPrivateChat([FromBody] StartPrivateChatRequest model)
        {
            // Validate sender and receiver permissions
            if (!await IsUserInBoardAsync(model.BoardId, model.SenderUserId) ||
                !await IsUserInBoardAsync(model.BoardId, model.ReceiverUserId))
            {
                return Unauthorized(new { success = false, message = "You are not authorized to start this chat." });
            }

            // Check if a private chat already exists
            var existingChat = await _context.ChatPrivate
                .FirstOrDefaultAsync(c =>
                    (c.SenderUserId == model.SenderUserId && c.ReceiverUserId == model.ReceiverUserId) ||
                    (c.SenderUserId == model.ReceiverUserId && c.ReceiverUserId == model.SenderUserId));

            if (existingChat != null)
            {
                return Ok(new { success = true, chatPrivateId = existingChat.ChatPrivateId });
            }

            // Create a new private chat
            var newChat = new ChatPrivate
            {
                SenderUserId = model.SenderUserId,
                ReceiverUserId = model.ReceiverUserId,
                NotificationDateTime = DateTime.UtcNow,
                Message = "Private chat started"
            };

            _context.ChatPrivate.Add(newChat);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, chatPrivateId = newChat.ChatPrivateId });
        }

        // Send private message
        [HttpPost("send")]
        public async Task<IActionResult> SendPrivateMessage([FromForm] ChatPrivateViewModel model, IFormFile? file)
        {
            var senderUserId = GetUserIdFromHeader();
            if (senderUserId == null)
            {
                return Unauthorized(new { success = false, message = "User is not authenticated." });
            }

            // Validate sender and receiver permissions
            if (!await IsUserInBoardAsync(model.BoardId, senderUserId.Value) ||
                !await IsUserInBoardAsync(model.BoardId, model.ReceiverUserId))
            {
                return Unauthorized(new { success = false, message = "You are not authorized to send this message." });
            }

            var privateChat = new ChatPrivate
            {
                SenderUserId = senderUserId.Value,
                ReceiverUserId = model.ReceiverUserId,
                Message = model.Message ?? string.Empty,
                NotificationDateTime = DateTime.UtcNow
            };

            if (file != null)
            {
                try
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    using (var fileStream = file.OpenReadStream())
                    {
                        var fileUrl = await _storageService.UploadImageAsync(fileStream, "dainotecontainer", "private-chat", fileName);
                        privateChat.ImageChat = fileUrl;
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"File upload failed: {ex.Message}");
                }
            }

            _context.ChatPrivate.Add(privateChat);
            await _context.SaveChangesAsync();

            var senderUser = await _userManager.FindByIdAsync(senderUserId.Value.ToString());
            if (senderUser == null)
            {
                return StatusCode(500, "Sender user not found.");
            }

            // Notify receiver in real-time
            await hubContext.Clients.User(model.ReceiverUserId.ToString()).SendAsync("ReceivePrivateMessage", new
            {
                privateChat.ChatPrivateId,
                privateChat.SenderUserId,
                privateChat.ReceiverUserId,
                privateChat.Message,
                privateChat.ImageChat,
                privateChat.NotificationDateTime,
                SenderUser = new
                {
                    senderUser.Id,
                    senderUser.Email,
                    senderUser.FullName,
                    senderUser.AvatarImage
                }
            });

            return Ok(new
            {
                success = true,
                privateChat = new
                {
                    privateChat.ChatPrivateId,
                    privateChat.SenderUserId,
                    privateChat.ReceiverUserId,
                    privateChat.Message,
                    privateChat.ImageChat,
                    privateChat.NotificationDateTime,
                    SenderUser = new
                    {
                        senderUser.Id,
                        senderUser.Email,
                        senderUser.FullName,
                        senderUser.AvatarImage
                    }
                }
            });
        }
        public class StartPrivateChatRequest
        {
            public Guid SenderUserId { get; set; }
            public Guid ReceiverUserId { get; set; }
            public Guid BoardId { get; set; }
        }
    }
}
