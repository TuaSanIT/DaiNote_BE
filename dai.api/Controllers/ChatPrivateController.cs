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
        [HttpGet("messages")]
        public async Task<IActionResult> GetPrivateMessages(Guid senderUserId, Guid receiverUserId)
        {
            try
            {
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
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpPost("start")]
        public async Task<IActionResult> StartPrivateChat([FromBody] StartPrivateChatRequest model)
        {
            // Validate sender and receiver
            var senderUser = await _context.Users.FindAsync(model.SenderUserId);
            var receiverUser = await _context.Users.FindAsync(model.ReceiverUserId);

            if (senderUser == null || receiverUser == null)
            {
                return NotFound(new { success = false, message = "Sender or Receiver user not found." });
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

            // Create new private chat
            var newChat = new ChatPrivate
            {
                SenderUserId = model.SenderUserId,
                ReceiverUserId = model.ReceiverUserId,
                NotificationDateTime = DateTime.UtcNow,
                Message = "Private chat started" // Default message or log
            };

            _context.ChatPrivate.Add(newChat);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, chatPrivateId = newChat.ChatPrivateId });
        }

        // Send private message with optional file upload
        [HttpPost("send")]
        public async Task<IActionResult> SendPrivateMessage([FromForm] ChatPrivateViewModel model, IFormFile file)
        {
            var senderUser = await _userManager.GetUserAsync(User);

            // Validate sender
            if (senderUser == null)
            {
                return Unauthorized(new { success = false, message = "User not authenticated." });
            }

            // Validate receiver
            var receiverUser = await _userManager.FindByIdAsync(model.ReceiverUserId.ToString());
            if (receiverUser == null)
            {
                return NotFound(new { success = false, message = "Receiver user does not exist." });
            }

            // Build private message
            var privateChat = new ChatPrivate
            {
                SenderUserId = senderUser.Id,
                ReceiverUserId = model.ReceiverUserId,
                Message = model.Message ?? string.Empty,
                NotificationDateTime = DateTime.UtcNow
            };

            // Handle file upload
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

            // Save message to database
            _context.ChatPrivate.Add(privateChat);
            await _context.SaveChangesAsync();

            // Send message to the receiver via SignalR
            await hubContext.Clients.User(model.ReceiverUserId.ToString()).SendAsync("ReceivePrivateMessage", privateChat);

            return Ok(new { success = true, privateChat });
        }
        public class StartPrivateChatRequest
        {
            public Guid SenderUserId { get; set; }
            public Guid ReceiverUserId { get; set; }
        }
    }
}
