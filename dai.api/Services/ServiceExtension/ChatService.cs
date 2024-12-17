using dai.api.Hubs;
using dai.api.Middleware;
using dai.core.DTO.Chat;
using dai.core.Models;
using dai.core.Models.Entities;
using dai.dataAccess.DbContext;
using Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace dai.api.Services.ServiceExtension
{
    public class ChatService
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly AppDbContext _dbContext;
        private readonly UserManager<UserModel> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AzureBlobService _blobService;

        public ChatService(AppDbContext dbContext, IHubContext<ChatHub> hubContext, UserManager<UserModel> userManager, IHttpContextAccessor httpContextAccessor, AzureBlobService blobService)
        {
            _dbContext = dbContext;
            _hubContext = hubContext;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _blobService = blobService;
        }
        public async Task<List<Chat>> GetMessagesAsync(Guid chatRoomId)
        {
            var messages = await _dbContext.Chats
                .Where(m => m.ChatRoomDataId == chatRoomId)
                .OrderBy(m => m.NotificationDateTime)
                .ToListAsync();

            return messages;
        }
        public async Task<Chat> SendGroupMessageAsync(ChatViewModel model, IFormFile file)
        {
            var currentUser = await _httpContextAccessor.HttpContext.GetUserAsync(_userManager);
            var fileUrl = file != null ? await UploadFileAsync(file, "chat-files") : null;

            var message = new Chat
            {
                UserId = currentUser.Id,
                Message = model.Message,
                MessageType = file != null ? "file" : "text",
                ImageChatRoom = fileUrl,
                NotificationDateTime = DateTime.UtcNow,
                ChatRoomDataId = model.ChatRoomId,
                Avatar = currentUser.AvatarImage ?? "https://default-avatar.example.com"
            };

            _dbContext.Chats.Add(message);
            await _dbContext.SaveChangesAsync();

            await _hubContext.Clients.Group(model.ChatRoomId.ToString()).SendAsync("ReceiveMessage", message);

            return message;
        }
        public async Task<ChatPrivate> SendPrivateMessageAsync(ChatPrivateViewModel model, IFormFile file)
        {
            var senderUser = await _httpContextAccessor.HttpContext.GetUserAsync(_userManager);
            var fileUrl = file != null ? await UploadFileAsync(file, "private-chat") : null;

            var message = new ChatPrivate
            {
                SenderUserId = senderUser.Id,
                ReceiverUserId = model.ReceiverUserId,
                Message = model.Message,
                ImageChat = fileUrl,
                NotificationDateTime = DateTime.UtcNow
            };

            _dbContext.ChatPrivate.Add(message);
            await _dbContext.SaveChangesAsync();

            await _hubContext.Clients.User(model.ReceiverUserId.ToString()).SendAsync("ReceivePrivateMessage", message);

            return message;
        }
        private async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            using (var stream = file.OpenReadStream())
            {
                return await _blobService.UploadFileAsync(stream, "dainotecontainer", folder, fileName, file.ContentType);
            }
        }
    }
}
