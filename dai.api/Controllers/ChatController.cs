using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using dai.core.Models.Entities;
using dai.dataAccess.IRepositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using dai.api.Hubs;
using dai.dataAccess.DbContext;
using dai.api.Services.ServiceExtension;

namespace dai.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatRepository _repository;
        private readonly IHubContext<ChatHub> _chatHubContext;
        private readonly AppDbContext _context;
        private readonly AzureBlobService _azureBlobService;

        public ChatController(IChatRepository repository, IHubContext<ChatHub> chatHubContext, AppDbContext context, AzureBlobService azureBlobService)
        {
            _repository = repository;
            _chatHubContext = chatHubContext;
            _context = context;
            _azureBlobService = azureBlobService;
        }

        [HttpPost("room/send-message")]
        public async Task<IActionResult> SendRoomMessage([FromForm] RoomMessageRequest request)
        {
            string? attachmentUrl = null;

            if (request.File != null)
            {
                var fileName = $"{Guid.NewGuid()}_{request.File.FileName}";
                attachmentUrl = await _azureBlobService.UploadFileAsync(
                    request.File.OpenReadStream(),
                    "dainotecontainer",
                    "chat-room-files",
                    fileName,
                    request.File.ContentType);
            }

            var chat = new Chat
            {
                ChatId = Guid.NewGuid(),
                ChatRoomDataId = request.ChatRoomId,
                UserId = request.UserId,
                Message = request.Message,
                MessageType = string.IsNullOrEmpty(attachmentUrl) ? "text" : "file",
                NotificationDateTime = DateTime.UtcNow,
                ImageChatRoom = attachmentUrl
            };

            await _repository.CreateChatAsync(chat);

            await _chatHubContext.Clients.Group(request.ChatRoomId.ToString())
                .SendAsync("ReceiveRoomMessage", chat);

            return Ok(chat);
        }

        [HttpPost("private/send-message")]
        public async Task<IActionResult> SendPrivateMessage([FromForm] PrivateMessageRequest request)
        {
            string? attachmentUrl = null;
            string messageType = "text"; // Mặc định là văn bản

            try
            {
                if (request.File != null)
                {
                    var fileExtension = Path.GetExtension(request.File.FileName).ToLower();
                    var fileName = $"{Guid.NewGuid()}_{request.File.FileName}";
                    attachmentUrl = await _azureBlobService.UploadFileAsync(
                        request.File.OpenReadStream(),
                        "dainotecontainer",
                        "chat-private-files", // Đặt tất cả tệp vào thư mục chung
                        fileName,
                        request.File.ContentType);

                    messageType = "file"; // Tất cả đều là tệp
                }

                var chatPrivate = new ChatPrivate
                {
                    ChatPrivateId = Guid.NewGuid(),
                    SenderUserId = request.SenderId,
                    ReceiverUserId = request.ReceiverId,
                    Message = string.IsNullOrEmpty(request.Message) && attachmentUrl != null ? null : request.Message,
                    ImageChat = attachmentUrl, // Đặt URL tệp
                    NotificationDateTime = DateTime.UtcNow
                };

                await _repository.CreateChatPrivateAsync(chatPrivate);

                await _chatHubContext.Clients.User(request.ReceiverId.ToString())
                    .SendAsync("ReceivePrivateMessage", new
                    {
                        chatPrivate.SenderUserId,
                        chatPrivate.ReceiverUserId,
                        chatPrivate.Message,
                        chatPrivate.ImageChat,
                        chatPrivate.NotificationDateTime,
                        MessageType = messageType
                    });

                return Ok(new
                {
                    chatPrivate.SenderUserId,
                    chatPrivate.ReceiverUserId,
                    chatPrivate.Message,
                    chatPrivate.ImageChat,
                    chatPrivate.NotificationDateTime,
                    MessageType = messageType
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "An unexpected error occurred", Details = ex.Message });
            }
        }


        [HttpPost("room/create")]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
        {
            var existingRoom = await _repository.GetRoomByBoardIdAsync(request.BoardId);

            if (existingRoom != null)
            {
                return Ok(existingRoom);
            }

            var room = new ChatRoom
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
            };

            await _repository.CreateRoomAsync(room);

            var chatRoomUser = new ChatRoomUser
            {
                Id = Guid.NewGuid(),
                ChatRoomId = room.Id,
                UserId = request.BoardId
            };

            _context.ChatRoomUsers.Add(chatRoomUser);
            await _context.SaveChangesAsync();

            return Ok(room);
        }

        [HttpGet("room/messages/{chatRoomId}")]
        public async Task<IActionResult> GetRoomMessages(Guid chatRoomId)
        {
            var messages = await _repository.GetChatsByRoomIdAsync(chatRoomId);
            return Ok(messages);
        }

        [HttpGet("private/messages")]
        public async Task<IActionResult> GetPrivateMessages([FromQuery] Guid senderId, [FromQuery] Guid receiverId)
        {
            var messages = await _repository.GetChatPrivateMessagesAsync(senderId, receiverId);
            return Ok(messages);
        }

        [HttpPost("private/start")]
        public async Task<IActionResult> StartPrivateChat([FromBody] StartPrivateChatRequest request)
        {
            var sender = await _context.Users.FindAsync(request.SenderUserId);
            var receiver = await _context.Users.FindAsync(request.ReceiverUserId);

            if (sender == null || receiver == null)
            {
                return NotFound("Sender or Receiver not found.");
            }

            var existingChat = await _context.ChatPrivate
                .FirstOrDefaultAsync(cp =>
                    (cp.SenderUserId == request.SenderUserId && cp.ReceiverUserId == request.ReceiverUserId) ||
                    (cp.SenderUserId == request.ReceiverUserId && cp.ReceiverUserId == request.SenderUserId));

            if (existingChat != null)
            {
                return Ok(new { ChatPrivateId = existingChat.ChatPrivateId });
            }

            var chatPrivate = new ChatPrivate
            {
                ChatPrivateId = Guid.NewGuid(),
                SenderUserId = request.SenderUserId,
                ReceiverUserId = request.ReceiverUserId
            };

            _context.ChatPrivate.Add(chatPrivate);
            await _context.SaveChangesAsync();

            return Ok(new { ChatPrivateId = chatPrivate.ChatPrivateId });
        }
    }
    public class CreateRoomRequest
    {
        public string Name { get; set; }
        public Guid BoardId { get; set; }
    }

    public class RoomMessageRequest
    {
        public Guid ChatRoomId { get; set; }
        public Guid UserId { get; set; }
        public string Message { get; set; }
        public IFormFile? File { get; set; }
    }

    public class PrivateMessageRequest
    {
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public string? Message { get; set; }
        public IFormFile? File { get; set; }
    }

    public class StartPrivateChatRequest
    {
        public Guid SenderUserId { get; set; }
        public Guid ReceiverUserId { get; set; }
    }
}




