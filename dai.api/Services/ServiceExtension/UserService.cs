using dai.core.Models;
using Microsoft.AspNetCore.Identity;

namespace dai.api.Services.ServiceExtension
{
    public class UserService
    {

        private readonly UserManager<UserModel> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public UserService(UserManager<UserModel> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<UserModel> GetCurrentLoggedInUser()
        {
            var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            return user;
        }

    }
}
