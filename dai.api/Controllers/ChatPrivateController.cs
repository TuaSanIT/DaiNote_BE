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
                var currentUser = await _userManager.GetUserAsync(User);
                string emailUserCurr = User.Identity.Name ?? "DefaultEmail@example.com";
                // Lấy tất cả tin nhắn riêng tư giữa hai người dùng với các điều kiện là senderUserId và receiverUserId
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
                        UserNameCurrent = emailUserCurr,
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

        [HttpPost("send")]
        public async Task<IActionResult> SendPrivateMessage(ChatPrivateViewModel model, IFormFile file)
        {
            var senderUser = await _userManager.GetUserAsync(User);
            if (ModelState.IsValid || file == null)
            {
                if (!string.IsNullOrEmpty(model.Message) && file == null || string.IsNullOrEmpty(model.Message) && file != null || !string.IsNullOrEmpty(model.Message) && file != null)
                {
                    var privateChat = new ChatPrivate
                    {
                        SenderUserId = senderUser.Id,
                        ReceiverUserId = model.ReceiverUserId, // model.ReceiverUserId là ID của người nhận
                        Message = !string.IsNullOrEmpty(model.Message) ? model.Message : "",
                        NotificationDateTime = DateTime.Now,
                    };

                    // Kiểm tra xem người nhận có tồn tại không
                    var receiverUser = await _userManager.FindByIdAsync(model.ReceiverUserId.ToString());
                    if (receiverUser == null)
                    {
                        return Json(new { success = false, message = "Người nhận không tồn tại." });
                    }
                    if (file != null)
                    {
                        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        using (var fileStream = file.OpenReadStream())
                        {
                            var imagePath = await _storageService.UploadImageAsync(fileStream, "dainotecontainer", "private-chat", fileName);
                            if (imagePath != null)
                            {
                                privateChat.ImageChat = imagePath;
                            }
                        }
                    }
                    // Lưu tin nhắn riêng tư vào cơ sở dữ liệu
                    _context.ChatPrivate.Add(privateChat);
                    await _context.SaveChangesAsync();
                    // Gửi tin nhắn riêng tư đến người nhận thông qua SignalR
                    await hubContext.Clients.All.SendAsync("ReceiveChatPrivateRealtime", privateChat);

                    return Json(new { success = true, privateChat });
                }
            }
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors) });
        }

    }
}
