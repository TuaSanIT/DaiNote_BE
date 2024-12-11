using dai.core.DTO.Chat;
using dai.dataAccess.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace dai.api.Services.ServiceExtension
{
    public class OnlineUserService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly Dictionary<string, UserInformation> _connectedUsers = new Dictionary<string, UserInformation>();

        public OnlineUserService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public void AddUser(string connectionId, UserInformation userInfo)
        {
            if (userInfo != null)
            {
                _connectedUsers.TryAdd(connectionId, userInfo);
            }
        }

        public async Task<List<UserInformation>> GetOnlineUsersAsync()
        {
            return _connectedUsers.Values.ToList();
        }

        public async Task<int> GetTotalUsersFromDatabaseAsync()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await dbContext.Users.CountAsync();
            }
        }
    }
}
