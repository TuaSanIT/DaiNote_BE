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
        [HttpGet("messages")]
        public async Task<IActionResult> GetMessages()
        {
            try
            {
                var listChatData = await dbContext.Chats.ToListAsync();
                var currentUser = await _userManager.GetUserAsync(User);
                string emailUserCurr = User.Identity.Name ?? "DefaultEmail@example.com";
                var chatDataWithUsers = await dbContext.Chats
                  .Include(c => c.Id)
                  .OrderBy(c => c.NotificationDateTime)
                  .ToListAsync();

                var formattedNotifications = chatDataWithUsers.Select(n => new
                {
                    n.ChatId,
                    n.UserId,
                    n.Message,
                    n.MessageType,
                    n.ImageChatRoom,
                    NotificationDateTime = n.NotificationDateTime.ToString("HH:mm dd/MM/yyyy"),
                    User = n.Id,
                    Email = n.Id.Email,
                    AvatarChat = n.Id.AvatarImage,
                    FullnameChat = n.Id.FullName,
                    UserNameCurrent = emailUserCurr,
                    ChatRoom = n.ChatRoomDataId,
                });


                return Ok(formattedNotifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpPost("send")]
        public async Task<IActionResult> SendMessages(ChatViewModel model, IFormFile file)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                if (ModelState.IsValid || file == null)
                {
                    if (!string.IsNullOrEmpty(model.Message) && file == null ||
                        string.IsNullOrEmpty(model.Message) && file != null ||
                        !string.IsNullOrEmpty(model.Message) && file != null)
                    {
                        var notification = new Chat
                        {
                            UserId = user.Id,
                            Message = !string.IsNullOrEmpty(model.Message) ? model.Message : "",
                            MessageType = "All",
                            NotificationDateTime = DateTime.Now,
                            Avatar = !string.IsNullOrEmpty(user.AvatarImage) ? user.AvatarImage : "https://i.pinimg.com/736x/0d/64/98/0d64989794b1a4c9d89bff571d3d5842.jpg",
                            ChatRoomDataId = model.ChatRoomId,
                        };

                        if (file != null)
                        {
                            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                            using (var fileStream = file.OpenReadStream())
                            {
                                var imagePath = await _storageService.UploadImageAsync(fileStream, "dainotecontainer", "chat-images", fileName);
                                if (imagePath != null)
                                {
                                    notification.ImageChatRoom = imagePath;
                                }
                            }
                        }

                        dbContext.Chats.Add(notification);
                        await dbContext.SaveChangesAsync();
                        await hubContext.Clients.All.SendAsync("ReceiveNotificationRealtime", notification);

                        return Json(new { success = true, notification });
                    }
                }
            }
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors) });
        }

    }
}
