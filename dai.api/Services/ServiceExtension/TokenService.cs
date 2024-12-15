using dai.core.Models;
using dai.dataAccess.DbContext;
using Google;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace dai.api.Services.ServiceExtension
{
    public class TokenService
    {
        private readonly AppDbContext _context;

        public TokenService(AppDbContext context)
        {
            _context = context;
        }


        public async Task InvalidateTokenAsync(string token, string userId, DateTime tokenExpiration)
        {
            var revokedToken = new RevokedToken
            {
                Token = token,
                UserId = userId,
                RevokedAt = DateTime.UtcNow,
                Expiration = tokenExpiration,
                IsActive = false // Mark the token as revoked
            };

            _context.RevokedTokens.Add(revokedToken);
            await _context.SaveChangesAsync();
        }


        public async Task<bool> IsTokenRevokedAsync(string token)
        {
            return await _context.RevokedTokens
                .AnyAsync(rt => rt.Token == token && !rt.IsActive && rt.Expiration > DateTime.UtcNow);
        }


        public DateTime? GetTokenExpiration(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                return jwtToken.ValidTo; // Return expiration time
            }
            catch (Exception)
            {
                return null; // If token is invalid
            }
        }
    }

}
