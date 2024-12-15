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
        public ChatService(AppDbContext dbContext, IHubContext<ChatHub> hubContext, UserManager<UserModel> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _hubContext = hubContext;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }


        public async Task<List<Chat>> GetMessagesAsync()
        {
            try
            {
                var notifications = await _dbContext.Chats.ToListAsync();

                var formattedNotifications = notifications.Select(n => new Chat
                {
                    Id = n.Id,

                    Message = n.Message,
                    MessageType = n.MessageType,
                    NotificationDateTime = n.NotificationDateTime,
                    Avatar = n.Avatar
                }).ToList();

                return formattedNotifications;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task SendMessage(ChatViewModel model)
        {
            var currentUser = await _httpContextAccessor.HttpContext.GetUserAsync(_userManager);
            var notification = new Chat
            {

                Message = model.Message,
                MessageType = "All",
                NotificationDateTime = DateTime.Now,
                Avatar = currentUser.AvatarImage
            };

            _dbContext.Chats.Add(notification);
            await _dbContext.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", notification);
            await _hubContext.Clients.All.SendAsync("ReceiveNotificationRealtime", new List<Chat> { notification });

        }
    }
}
