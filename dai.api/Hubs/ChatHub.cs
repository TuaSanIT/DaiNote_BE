using dai.api.Services.ServiceExtension;
using dai.core.DTO.Chat;
using dai.core.Models.ChatModels;
using dai.core.Models.Entities;
using dai.core.Models.Notifications;
using dai.dataAccess.DbContext;
using Google;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace dai.api.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;
        private readonly UserService _userService;
        private readonly AzureBlobService _storageService;

        public ChatHub(AppDbContext context, UserService userService, AzureBlobService storageService)
        {
            _context = context;
            _userService = userService;
            _storageService = storageService;
        }
        public async Task SendMessage(Message msg)
        {
            await Clients.All.SendAsync("ReceiveMessage2", $"{msg.User} send message {msg.Content}");
        }
        public async Task JoinRoom(UserConnection userConnection)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userConnection.Room);

            await Clients.Group(userConnection.Room).SendAsync("ReceiveMessage", "bot", $"{userConnection.User} has joined {userConnection.Room}");
        }
        public async Task CallLoadChatData()
        {
            await Clients.All.SendAsync("LoadChatData");
        }
        public async Task GetChatRoomSignalR(ChatRoom createRoom)
        {
            await Clients.All.SendAsync("GetChatRoomSignalR", createRoom);
        }
        public async Task SendUpdatedNotifications(List<Notification> notifications)
        {
            await Clients.All.SendAsync("ReceiveNotificationRealtime", notifications);
        }
        public async Task SendUpdatedChatPrivate(List<ChatPrivate> chatPrivates)
        {
            await Clients.All.SendAsync("ReceiveChatPrivateRealtime", chatPrivates);
        }
        public async Task NotifyTyping(bool isTyping, string receiverConnectionId)
        {
            var emailUserCurrent = Context.User.Identity.Name;
            var userCurrent = await _context.userModels.FirstOrDefaultAsync(user => user.Email == emailUserCurrent);
            await Clients.User(receiverConnectionId).SendAsync("ReceiveTypingNotification", userCurrent, isTyping);
        }

        public async Task SendNotificationToAll(string message)
        {
            await Clients.Others.SendAsync("ReceivedNotification", message);
        }

        public static readonly Dictionary<string, UserInformation> ConnectedUsers = new Dictionary<string, UserInformation>();
        public static readonly Dictionary<string, UserInfo> UserIds = new Dictionary<string, UserInfo>();
        public override async Task OnConnectedAsync()
        {
            var currentUser = await _userService.GetCurrentLoggedInUser();
            if (currentUser != null)
            {
                var userId = currentUser.Id;
                var username = currentUser.UserName;
                UserIds[Context.ConnectionId] = new UserInfo { UserId = userId, Username = username };

                UserInformation userInfo = await GetUserInfoFromContext();
                if (!ConnectedUsers.ContainsKey(Context.ConnectionId))
                {
                    ConnectedUsers[Context.ConnectionId] = userInfo;
                    await Clients.Caller.SendAsync("ReceivedNotificationWelcome", $"xin chào {userInfo.FullName}");
                    await Clients.All.SendAsync("LoadChatData");
                }
                await Clients.Others.SendAsync("ReceivedNotificationUserOnline", $"{userInfo.FullName}");
                await UpdateConnectedUsersList();
                await UpdateConnectedUsersOnlineList();
                await UpdateConnectedUsersOfflineList(ConnectedUsers.Values.ToList());
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string connectionId = Context.ConnectionId;
            if (ConnectedUsers.ContainsKey(connectionId))
            {
                ConnectedUsers.Remove(connectionId);
                await UpdateConnectedUsersList();
                await UpdateConnectedUsersOnlineList();
                await UpdateConnectedUsersOfflineList(ConnectedUsers.Values.ToList());
            }
        }

        List<UserInformation> userList = new List<UserInformation>();
        List<UserInformation> userOnlineList = new List<UserInformation>();

        private async Task UpdateConnectedUsersList()
        {

            Dictionary<Guid, UserInformation> uniqueUsers = new Dictionary<Guid, UserInformation>();


            foreach (var userInfo in ConnectedUsers.Values)
            {
                if (!uniqueUsers.ContainsKey(userInfo.Id))
                {
                    uniqueUsers[userInfo.Id] = userInfo;
                }
            }


            List<UserInformation> uniqueUsersList = uniqueUsers.Values.ToList();


            await Clients.All.SendAsync("UpdateUsersList", uniqueUsersList);
        }

        private async Task UpdateConnectedUsersOfflineList(List<UserInformation> userOnlineList)
        {
            var userOfflineList = await _context.userModels
                                    .Select(user => new UserInformation
                                    {
                                        Id = user.Id,
                                        Email = user.Email,
                                        Avatar = user.AvatarImage,
                                        FullName = user.FullName
                                    })
                                    .ToListAsync();

            var offlineUsers = userOfflineList.Where(user => !userOnlineList.Any(u => u.Email == user.Email)).ToList();

            await Clients.All.SendAsync("UpdateUsersOfflineList", offlineUsers);
        }
        public async Task GetUserId()
        {
            Guid userId = UserIds.ContainsKey(Context.ConnectionId) ? UserIds[Context.ConnectionId].UserId : Guid.Empty;
            string username = UserIds.ContainsKey(Context.ConnectionId) ? UserIds[Context.ConnectionId].Username : "";
            await Clients.Caller.SendAsync("ReceiveUserId", userId, username);
        }


        private async Task UpdateConnectedUsersOnlineList()
        {
            Dictionary<Guid, UserInformation> uniqueUsers = new Dictionary<Guid, UserInformation>();


            foreach (var userInfo in ConnectedUsers.Values)
            {
                if (!uniqueUsers.ContainsKey(userInfo.Id))
                {
                    uniqueUsers[userInfo.Id] = userInfo;
                }
            }


            List<UserInformation> uniqueUsersList = uniqueUsers.Values.ToList();

            await Clients.All.SendAsync("UpdateUsersOnlineList", uniqueUsersList);
        }


        private async Task<UserInformation> GetUserInfoFromContext()
        {
            var email = Context.User.Identity.Name;
            var user = await _context.userModels.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                return new UserInformation
                {
                    Id = user.Id,
                    Email = user.Email,
                    Avatar = user.AvatarImage,
                    FullName = user.FullName
                };
            }
            else
            {
                return new UserInformation
                {
                    Email = "guest@example.com",
                    Avatar = null
                };
            }
        }

        public async Task SendGroupMessage(IFormFile file, string message, Guid chatRoomId)
        {
            string fileUrl = null;

            if (file != null)
            {
                fileUrl = await UploadFileToAzureBlob(file, "chat-files");
            }

            var chat = new Chat
            {
                ChatRoomDataId = chatRoomId,
                Message = message ?? "File sent",
                MessageType = file != null ? "file" : "text",
                ImageChatRoom = fileUrl,
                NotificationDateTime = DateTime.UtcNow
            };

            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();

            await Clients.Group(chatRoomId.ToString()).SendAsync("ReceiveGroupMessage", chat);
        }

        public async Task SendPrivateMessage(IFormFile file, string message, Guid receiverUserId)
        {
            string fileUrl = null;

            if (file != null)
            {
                fileUrl = await UploadFileToAzureBlob(file, "private-chat");
            }

            var senderUser = await _context.userModels
    .FirstOrDefaultAsync(u => u.Email == Context.User.Identity.Name);

            if (senderUser == null)
            {
                throw new InvalidOperationException("Unable to find the authenticated user.");
            }

            var chatPrivate = new ChatPrivate
            {
                SenderUserId = senderUser.Id, 
                ReceiverUserId = receiverUserId, 
                Message = message ?? "File sent",
                ImageChat = fileUrl,
                NotificationDateTime = DateTime.UtcNow
            };

            _context.ChatPrivate.Add(chatPrivate);
            await _context.SaveChangesAsync();

            await Clients.User(receiverUserId.ToString()).SendAsync("ReceivePrivateMessage", chatPrivate);
        }
        private async Task<string> UploadFileToAzureBlob(IFormFile file, string folder)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            using var fileStream = file.OpenReadStream();
            return await _storageService.UploadFileAsync(fileStream, "dainotecontainer", folder, fileName, file.ContentType);
        }

    }
}
