using System.Security.Claims;

namespace dai.api.Helper
{
    public static class UserHelper
    {
        public static Guid GetUserId(ClaimsPrincipal user)
        {
            Claim? userIdClaim = user.FindFirst("Id");
            if (userIdClaim == null)
            {
                throw new UnauthorizedAccessException("User is not authorized");
            }

            if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                throw new UnauthorizedAccessException("Invalid user ID");
            }

            return userId;
        }

        public static string GetRole(ClaimsPrincipal user)
        {
            string? userRole = user.FindFirstValue(ClaimTypes.Role);
            if (userRole == null)
            {
                throw new UnauthorizedAccessException("User is not authorized");
            }

            return userRole;
        }

        public static Guid GetUserIdForService(ClaimsPrincipal user)
        {
            Claim? userIdClaim = user.FindFirst("Id");
            if (userIdClaim == null)
            {
                return Guid.Empty;  
            }

            if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Guid.Empty;  
            }

            return userId;
        }

        public static string GetRoleForService(ClaimsPrincipal user)
        {
            string? userRole = user.FindFirstValue(ClaimTypes.Role);
            if (userRole == null)
            {
                return "guest";
            }

            return userRole;
        }
    }

}
