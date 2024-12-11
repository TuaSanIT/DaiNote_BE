using dai.core.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace dai.api.Middleware
{
    public static class HttpContextExtensions
    {
        public static async Task<UserModel> GetUserAsync(this HttpContext httpContext, UserManager<UserModel> userManager)
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                return await userManager.FindByIdAsync(userId);
            }
            return null;
        }
    }
}
