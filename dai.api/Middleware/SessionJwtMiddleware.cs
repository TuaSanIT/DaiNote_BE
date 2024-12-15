using dai.api.Services.ServiceExtension;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace dai.api.Middleware
{
    public class SessionJwtMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionJwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var token = context.Session.GetString("JwtToken");

            if (!string.IsNullOrEmpty(token))
            {
                try
                {

                    var claimsPrincipal = ValidateJwtToken(token);
                    context.User = claimsPrincipal; // Gán thông tin người dùng vào HttpContext
                }
                catch (SecurityTokenException)
                {
                    context.Session.Clear(); // Xóa session nếu token không hợp lệ
                }
            }

            await _next(context);
        }

        private ClaimsPrincipal ValidateJwtToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("YourSuperSecretKeyYourSuperSecretKeyYourSuperSecretKey");

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "FreeTrained",
                ValidateAudience = false,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            SecurityToken validatedToken;
            var principal = tokenHandler.ValidateToken(token, parameters, out validatedToken);
            return principal;
        }
    }


}
