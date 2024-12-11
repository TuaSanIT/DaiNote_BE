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

        public ChatHub(AppDbContext context, UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task SendPrivateMessage()
        {
            var chatPrivateList = await _context.Chats
               .Select(x => new
               {
                   Message = x.Message,
                   ImageChatRoom = x.ImageChatRoom
               }) // Chỉ chọn phần nội dung của tin nhắn
               .ToListAsync();

            // Gửi mỗi tin nhắn riêng tới tất cả client đang kết nối
            foreach (var message in chatPrivateList)
            {
                await Clients.All.SendAsync("ReceivePrivateMessage", message);
            }
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
            // Tạo một Dictionary mới để lưu trữ thông tin người dùng duy nhất dựa trên userId
            Dictionary<Guid, UserInformation> uniqueUsers = new Dictionary<Guid, UserInformation>();

            // Lặp qua danh sách connected users và thêm vào uniqueUsers chỉ nếu chưa có người dùng có cùng userId
            foreach (var userInfo in ConnectedUsers.Values)
            {
                if (!uniqueUsers.ContainsKey(userInfo.Id))
                {
                    uniqueUsers[userInfo.Id] = userInfo;
                }
            }

            // Chuyển danh sách các người dùng duy nhất thành List
            List<UserInformation> uniqueUsersList = uniqueUsers.Values.ToList();

            // Gửi danh sách người dùng duy nhất tới tất cả các client
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

            // Lặp qua danh sách connected users và thêm vào uniqueUsers chỉ nếu chưa có người dùng có cùng userId
            foreach (var userInfo in ConnectedUsers.Values)
            {
                if (!uniqueUsers.ContainsKey(userInfo.Id))
                {
                    uniqueUsers[userInfo.Id] = userInfo;
                }
            }

            // Chuyển danh sách các người dùng duy nhất thành List
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

    }
}
