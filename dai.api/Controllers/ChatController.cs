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
using System.ComponentModel.DataAnnotations;

namespace dai.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : Controller
    {
        private readonly IHubContext<ChatHub> hubContext;
        private readonly AppDbContext dbContext;
        private readonly UserManager<UserModel> _userManager;
        private readonly AzureBlobService _storageService;

        public ChatController(AppDbContext dbContext, IHubContext<ChatHub> hubContext, UserManager<UserModel> userManager, AzureBlobService storageService)
        {
            this.hubContext = hubContext;
            this.dbContext = dbContext;
            _userManager = userManager;
            _storageService = storageService;
        }
        [HttpGet("messages/{chatRoomId}")]
        public async Task<IActionResult> GetMessages(Guid chatRoomId)
        {
            var messages = await dbContext.Chats
                .Where(c => c.ChatRoomDataId == chatRoomId)
                .OrderBy(c => c.NotificationDateTime)
                .Select(c => new
                {
                    c.ChatId,
                    c.UserId,
                    c.Message,
                    c.MessageType,
                    c.ImageChatRoom,
                    NotificationDateTime = c.NotificationDateTime.ToString("HH:mm dd/MM/yyyy"),
                    Avatar = c.Avatar,
                })
                .ToListAsync();

            return Ok(messages);
        }
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromForm] ChatViewModel model, IFormFile file)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized("User not authenticated.");

            var chatMessage = new Chat
            {
                UserId = user.Id,
                Message = model.Message ?? string.Empty,
                MessageType = file != null ? (IsImageFile(file) ? "image" : "file") : "text",
                NotificationDateTime = DateTime.UtcNow,
                Avatar = user.AvatarImage ?? "https://default-avatar.example.com/avatar.png",
                ChatRoomDataId = model.ChatRoomId
            };

            if (file != null)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var folderName = IsImageFile(file) ? "chat-images" : "chat-files";

                using (var fileStream = file.OpenReadStream())
                {
                    var fileUrl = await _storageService.UploadFileAsync(
                        fileStream, "dainotecontainer", folderName, fileName, file.ContentType);

                    chatMessage.ImageChatRoom = fileUrl; // Set the uploaded file/image URL
                }
            }

            dbContext.Chats.Add(chatMessage);
            await dbContext.SaveChangesAsync();

            await hubContext.Clients.Group(model.ChatRoomId.ToString())
                .SendAsync("ReceiveMessage", new
                {
                    chatMessage.UserId,
                    chatMessage.Message,
                    chatMessage.MessageType,
                    chatMessage.ImageChatRoom,
                    chatMessage.NotificationDateTime,
                    chatMessage.Avatar
                });

            return Ok(new { success = true, chatMessage });
        }

        private bool IsImageFile(IFormFile file)
        {
            var supportedTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            return supportedTypes.Contains(Path.GetExtension(file.FileName).ToLower());
        }

        
    }
}
