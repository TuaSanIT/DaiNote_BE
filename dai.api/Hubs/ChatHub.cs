using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using dai.core.Models.Entities;
using dai.dataAccess.IRepositories;
using Microsoft.AspNetCore.SignalR;

namespace dai.api.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatRepository _repository;
        private readonly BlobServiceClient _blobServiceClient;

        public ChatHub(IChatRepository repository, BlobServiceClient blobServiceClient)
        {
            _repository = repository;
            _blobServiceClient = blobServiceClient;
        }

        public async Task SendRoomMessage(Guid chatRoomId, Guid userId, string message, string? attachmentUrl = null)
        {
            var chat = new Chat
            {
                ChatId = Guid.NewGuid(),
                ChatRoomDataId = chatRoomId,
                UserId = userId,
                Message = message,
                MessageType = string.IsNullOrEmpty(attachmentUrl) ? "text" : "file",
                NotificationDateTime = DateTime.UtcNow,
                ImageChatRoom = attachmentUrl
            };

            await _repository.CreateChatAsync(chat);

            await Clients.Group(chatRoomId.ToString())
                .SendAsync("ReceiveRoomMessage", chat);
        }

        public async Task SendPrivateMessage(Guid senderId, Guid receiverId, string message, string? attachmentUrl, string messageType, string senderName, string senderAvatar)
        {
            if (messageType == "file" && string.IsNullOrEmpty(attachmentUrl))
            {
                throw new ArgumentException("Attachment URL is required for file messages.");
            }

            // Create group name based on sender and receiver IDs to ensure consistent group naming
            string groupName = senderId.CompareTo(receiverId) < 0
                ? $"{senderId}-{receiverId}"
                : $"{receiverId}-{senderId}";

            Console.WriteLine($"Sending private message to group: {groupName}");

            // Notify via SignalR, including sender info
            await Clients.Group(groupName).SendAsync("ReceivePrivateMessage", new
            {
                SenderUserId = senderId,
                ReceiverUserId = receiverId,
                Message = message,
                ImageChat = attachmentUrl,
                SenderName = senderName,
                SenderAvatar = senderAvatar,
                NotificationDateTime = DateTime.UtcNow,
                MessageType = messageType
            });
        }


        public async Task<string> UploadFile(IFormFile file, string containerName, string folderName)
        {
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = blobContainerClient.GetBlobClient($"{folderName}/{file.FileName}");
            await using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

            return blobClient.Uri.ToString();
        }

        public async Task<List<Chat>> GetRoomMessages(Guid chatRoomId)
        {
            return await _repository.GetChatsByRoomIdAsync(chatRoomId);
        }

        public async Task<List<ChatPrivate>> GetPrivateMessages(Guid senderId, Guid receiverId)
        {
            return await _repository.GetChatPrivateMessagesAsync(senderId, receiverId);
        }
        public async Task JoinPrivateRoom(Guid senderUserId, Guid receiverUserId)
        {
            // Sắp xếp ID của người gửi và người nhận để tạo nhóm cố định
            var groupName = senderUserId.CompareTo(receiverUserId) < 0
                ? $"{senderUserId}-{receiverUserId}"  // sender first
                : $"{receiverUserId}-{senderUserId}"; // receiver first

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("UserJoined", senderUserId);

            // Log cho việc gia nhập nhóm
            Console.WriteLine($"User {senderUserId} and {receiverUserId} joined the group {groupName}");
        }

        public async Task JoinRoom(Guid chatRoomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chatRoomId.ToString());
        }

        public async Task LeaveRoom(Guid chatRoomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatRoomId.ToString());
        }

        public async Task<List<string>> GetEmojis()
        {
            return await Task.FromResult(new List<string>
        {
            "😀", "😂", "😍", "👍", "🔥", "🎉", "💔", "🥳", "😢", "😎"
        });
        }
    }

}
