using dai.core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using MailKit.Net.Smtp;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Newtonsoft.Json.Linq;
using Google.Apis.Auth;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using dai.dataAccess.DbContext;
using dai.api.Services.ServiceExtension;
using dai.api.Helper;
using Google.Apis.Util;

namespace dai.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<UserModel> _userManager;
        private readonly RoleManager<UserRoleModel> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _dbContext;
        private readonly TokenService _tokenService;

        private static Dictionary<string, string> _otpStorage = new Dictionary<string, string>();

        public AuthController(UserManager<UserModel> userManager, RoleManager<UserRoleModel> roleManager, IConfiguration configuration, ILogger<AuthController> logger, HttpClient httpClient, AppDbContext dbContext, TokenService tokenService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            _dbContext = dbContext;
            _tokenService = tokenService;
        }

        private Guid? GetUserIdFromHeader()
        {
            if (Request.Headers.TryGetValue("UserId", out var userIdString) && Guid.TryParse(userIdString, out var userId))
            {
                return userId;
            }
            return null;
        }

        [HttpGet("get-token/{userId}")]
        public async Task<IActionResult> GetTokenByUserId(Guid userId)
        {
            try
            {

                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }


                var token = GenerateToken(user);

                return Ok(new { message = "Token generated successfully.", token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while generating token for user.");
                return StatusCode(500, new { message = "An error occurred while generating the token." });
            }
        }


        [HttpGet("validate-ownership/{resourceType}/{resourceId}")]
public async Task<IActionResult> ValidateOwnership(string resourceType, Guid resourceId)
{
    var userId = GetUserIdFromHeader();
    if (userId == null)
    {
        return Unauthorized(new { message = "User not logged in." });
    }


    switch (resourceType.ToLower())
    {
        case "workspace":
            var workspace = await _dbContext.Workspaces.FindAsync(resourceId);
            if (workspace == null)
            {
                return NotFound(new { message = "Workspace not found." });
            }
            if (workspace.UserId != userId)
            {
                   return StatusCode(403, new { message = "You do not have permission to access this Workspace." });
            }
            return Ok(new { message = "Workspace ownership validated." });

        case "board":
            var board = await _dbContext.Boards.FindAsync(resourceId);
            if (board == null)
            {
                return NotFound(new { message = "Board not found." });
            }


            var workspaceOwner = await _dbContext.Workspaces
                .AnyAsync(w => w.Id == board.WorkspaceId && w.UserId == userId);


            var isEditor = await _dbContext.Collaborators
                .AnyAsync(c => c.Board_Id == resourceId && c.User_Id == userId && c.Permission == "Editor");

            if (!workspaceOwner && !isEditor)
            {
                  return StatusCode(403, new { message = "You do not have permission to access this board." });
            }
            return Ok(new { message = "Board ownership or collaboration validated." });

        default:
            return BadRequest(new { message = "Invalid resource type." });
    }
}

        private string SendEmail(string email, string emailCode)
        {
            string smtpEmail = _configuration["Smtp:Email"];
            string smtpPassword = _configuration["Smtp:Password"];
            string smtpHost = _configuration["Smtp:Host"];
            int smtpPort = int.Parse(_configuration["Smtp:Port"]);

            StringBuilder emailMessage = new StringBuilder();
            emailMessage.AppendLine("<html>");
            emailMessage.AppendLine("<body>");
            emailMessage.AppendLine($"<p> Dear {email}, </p>");
            emailMessage.AppendLine("<p>Thank you for registering with Dai. To verify your email address, please use the following verification code:</p>");
            emailMessage.AppendLine($"<h2>Verification Code: {emailCode} </h2>");
            emailMessage.AppendLine("<p>Please enter this code on our website to complete your registration</p>");
            emailMessage.AppendLine("<p> If you did not request this, please ignore this email.</p>");
            emailMessage.AppendLine("<br>");
            emailMessage.AppendLine("<p> Best regards, </p>");
            emailMessage.AppendLine("<p><strong>DAI</strong></p>");
            emailMessage.AppendLine("</body>");
            emailMessage.AppendLine("</html>");

            string message = emailMessage.ToString();
            var _email = new MimeMessage();
            _email.To.Add(MailboxAddress.Parse(email));
            _email.From.Add(MailboxAddress.Parse(smtpEmail));
            _email.Subject = "Email Confirmation OTP from DAI";
            _email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = message };

            using var smtp = new SmtpClient();
            smtp.Connect(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            smtp.Authenticate(smtpEmail, smtpPassword);
            smtp.Send(_email);
            smtp.Disconnect(true);
            return "Thanks for your registration, kindly check your email for confirmation code";
        }

        [HttpPost("register/{email}")]
        public async Task<IActionResult> Register(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null) return BadRequest("User already exists.");

            string otp = GenerateOtp();
            _otpStorage[email] = otp;

            string sendEmail = SendEmail(email, otp);
            return Ok(new { message = sendEmail, step = "Please verify your email with the OTP." });
        }

        [HttpPost("confirm-otp/{email}/{otp}")]
        public IActionResult ConfirmOtp(string email, string otp)
        {
            if (!_otpStorage.TryGetValue(email, out var storedOtp) || storedOtp != otp)
            {
                return BadRequest("Invalid OTP provided.");
            }


            _otpStorage.Remove(email);
            return Ok("OTP verified successfully. Please provide your registration details.");
        }

        [HttpPost("register-complete/{email}")]
        public async Task<IActionResult> RegisterComplete(string email, [FromBody] UserRegistrationModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Password) ||
                string.IsNullOrEmpty(model.FullName) ||
                string.IsNullOrEmpty(model.PhoneNumber) ||
                string.IsNullOrEmpty(model.UserName))
            {
                return BadRequest("Invalid input provided");
            }

            var user = new UserModel()
            {
                Email = email,
                UserName = model.UserName,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                PasswordHash = model.Password,
                AddedOn = DateTime.UtcNow,
                EmailConfirmed = true // Bạn có thể cần xử lý xác thực email nếu cần
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest($"User creation failed: {errors}");
            }

            return Ok("Registration successful. You can now log in.");
        }


        private async Task SaveUserIpAsync(Guid userId, string ipAddress)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null)
            {
                user.LastLoginIp = ipAddress;
                await _userManager.UpdateAsync(user);
            }
        }

        private async Task<bool> IsNewLoginIpAsync(Guid userId, string ipAddress)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            return user?.LastLoginIp != ipAddress;
        }

        private async Task<string> GetIpAddress()
        {

            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (!string.IsNullOrEmpty(remoteIp) && remoteIp != "::1")
                return remoteIp;

            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetStringAsync("https://api.ipify.org?format=json");
                var json = JObject.Parse(response);
                return json["ip"]?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
        private async Task<string> GetLocationFromIp(string ipAddress)
        {
            string accessKey = "7eeb1bdb63d16ea9b0e261d54103d5f9";
            using var httpClient = new HttpClient();

            try
            {
                var response = await httpClient.GetStringAsync($"http://api.ipstack.com/{ipAddress}?access_key={accessKey}");
                var json = JObject.Parse(response);

                if (json["error"] == null)
                {
                    string city = json["city"]?.ToString() ?? "Unknown city";
                    string country = json["country_name"]?.ToString() ?? "Unknown country";
                    return $"{city}, {country}";
                }
                else
                {
                    return json["error"]["info"]?.ToString() ?? "Unknown error occurred";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                return BadRequest("Email and password are required.");

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                return Unauthorized("Invalid email or password.");

            if (!await _userManager.IsEmailConfirmedAsync(user))
                return BadRequest("You need to confirm your email before logging in.");


            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "User";


            string ipAddress = await GetIpAddress();


            bool isNewIp = await IsNewLoginIpAsync(user.Id, ipAddress);

            if (isNewIp)
            {

                var location = await GetLocationFromIp(ipAddress);
                _logger.LogInformation($"Location from IP ({ipAddress}): {location}");


                string loginTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                SendLoginNotificationEmail(user.Email, loginTime, location);
                await SaveUserIpAsync(user.Id, ipAddress);
            }

            user.IsOnline = true;
            await _dbContext.SaveChangesAsync();

            string accessToken = GenerateToken(user);
            string refreshToken = GenerateRefreshToken();
            await SaveRefreshTokenAsync(user.Id.ToString(), refreshToken);


            HttpContext.Session.SetString("JwtToken", accessToken);
            HttpContext.Session.SetString("UserId", user.Id.ToString());

            return Ok(new
            {
                message = "Login successful.",
                accessToken = accessToken,
                refreshToken = refreshToken,
                role = primaryRole
            });
        }

        [HttpPost("refresh-token")]
        public IActionResult RefreshToken()
        {
            var oldToken = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(oldToken))
                return Unauthorized("No token found in session.");

            try
            {
                ValidateJwtToken(oldToken);
                var user = GetUserFromSession();
                string newToken = GenerateToken(user);
                HttpContext.Session.SetString("JwtToken", newToken);

                return Ok(new { token = newToken });
            }
            catch (Exception)
            {
                return Unauthorized("Invalid or expired token.");
            }
        }

        private UserModel GetUserFromSession()
        {
            var userId = HttpContext.Session.GetString("UserId");
            return _userManager.FindByIdAsync(userId).Result; // Lấy thông tin user từ database
        }

        private void SendLoginNotificationEmail(string email, string loginTime, string location)
        {
            string smtpEmail = _configuration["Smtp:Email"];
            string smtpPassword = _configuration["Smtp:Password"];
            string smtpHost = _configuration["Smtp:Host"];
            int smtpPort = int.Parse(_configuration["Smtp:Port"]);

            var emailMessage = new MimeMessage();
            emailMessage.To.Add(MailboxAddress.Parse(email));
            emailMessage.From.Add(MailboxAddress.Parse(smtpEmail));
            emailMessage.Subject = "Login warning !";

            var messageBuilder = new System.Text.StringBuilder();
            messageBuilder.AppendLine("<html><body>");
            messageBuilder.AppendLine($"<p> Dear {email}, </p>");
            messageBuilder.AppendLine($"<h2>You are logged into the DAI system at this time {loginTime}.</h2>");
            messageBuilder.AppendLine($"<h2>From location {location}.</h2>");
            messageBuilder.AppendLine("<p>Thank you for using our service !</p>");
            messageBuilder.AppendLine("<p>If it is not you, please change your password and reply to this email when you need our support !</p>");
            messageBuilder.AppendLine("<br>");
            messageBuilder.AppendLine("<p> Best regards, </p>");
            messageBuilder.AppendLine("<p><strong>DAI</strong></p>");
            messageBuilder.AppendLine("</body></html>");

            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = messageBuilder.ToString() };

            using var smtp = new SmtpClient();
            smtp.Connect(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            smtp.Authenticate(smtpEmail, smtpPassword);
            smtp.Send(emailMessage);
            smtp.Disconnect(true);
        }
        [HttpGet("validate-token")]
        public IActionResult ValidateToken(
    [FromHeader(Name = "Authorization")] string authorization,
    [FromHeader(Name = "UserId")] string userIdHeader)
        {

            if (!string.IsNullOrEmpty(userIdHeader) && Guid.TryParse(userIdHeader, out var userIdGuid))
            {

                if (IsValidUserId(userIdGuid))
                {
                    return Ok(new { isValid = true, userId = userIdGuid });
                }
                else
                {
                    return Unauthorized(new { isValid = false, message = "Invalid userId." });
                }
            }


            if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Bearer "))
            {
                return Unauthorized(new { isValid = false, message = "Token missing or invalid." });
            }


            var token = authorization.Substring("Bearer ".Length).Trim();

            try
            {

                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]))
                };


                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);


                var jwtToken = validatedToken as JwtSecurityToken;
                var tokenUserId = jwtToken?.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

                if (Guid.TryParse(tokenUserId, out var tokenUserIdGuid))
                {

                    return Ok(new { isValid = true, userId = tokenUserIdGuid });
                }
                else
                {

                    return Unauthorized(new { isValid = false, message = "Token does not contain a valid userId." });
                }
            }
            catch (SecurityTokenException)
            {

                return Unauthorized(new { isValid = false, message = "Token is invalid or expired." });
            }
        }

        private bool IsValidUserId(Guid userId)
        {
            var validUserIds = new List<Guid>
    {
        Guid.Parse("some-valid-guid-1"),
        Guid.Parse("some-valid-guid-2")
    };

            return validUserIds.Contains(userId);
        }

        private string GenerateToken(UserModel? user)
        {
            byte[] key = Encoding.ASCII.GetBytes("YourSuperSecretKeyYourSuperSecretKeyYourSuperSecretKey");
            var securityKey = new SymmetricSecurityKey(key);
            var credential = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user!.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user!.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"], audience: null, claims: claims, expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:AccessTokenExpiryMinutes"])), signingCredentials: credential);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)); // Tạo chuỗi 64 byte ngẫu nhiên
        }

        private void ValidateJwtToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = false,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            }, out _);
        }

        private async Task SaveRefreshTokenAsync(string userId, string refreshToken)
        {
            var refreshTokenEntity = new RevokedToken
            {
                UserId = userId,
                Token = refreshToken,
                Expiration = DateTime.UtcNow.AddDays(Convert.ToDouble(_configuration["Jwt:RefreshTokenExpiryDays"])),
                IsActive = true
            };

            _dbContext.RevokedTokens.Add(refreshTokenEntity);
            await _dbContext.SaveChangesAsync();
        }

        private string GenerateOtp()
        {
            return new Random().Next(100000, 999999).ToString();
        }


        [HttpPost("login-google")]
        public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleLoginRequest request)
        {
            if (string.IsNullOrEmpty(request.IdToken))
                return BadRequest("ID Token is required.");

            try
            {

                var payload = await ValidateGoogleTokenAsync(request.IdToken);
                if (payload == null)
                    return BadRequest("Invalid Google token.");

                var email = payload.Email;
                var user = await _userManager.FindByEmailAsync(email);


                if (user == null)
                {
                    user = new UserModel
                    {
                        Email = email,
                        UserName = email.Split('@')[0],
                        FullName = payload.Name,
                        EmailConfirmed = true, // Google đã xác minh email,
                        AddedOn = DateTime.UtcNow,
                        IsOnline = true,
                        PhoneNumber = "0000000000"
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                    {
                        return BadRequest($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }


                var ipAddress = !string.IsNullOrEmpty(request.ClientIp)
                    ? request.ClientIp
                    : HttpContext.Connection.RemoteIpAddress?.ToString();


                if (await IsNewLoginIpAsync(user.Id, ipAddress))
                {

                    var location = await GetLocationFromIp(ipAddress);
                    SendLoginNotificationEmail(user.Email, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), location);


                    await SaveUserIpAsync(user.Id, ipAddress);
                }


                var token = GenerateToken(user);


                HttpContext.Session.SetString("JwtToken", token);
                HttpContext.Session.SetString("UserId", user.Id.ToString());

                user.IsOnline = true;
                await _userManager.AddToRoleAsync(user, "User");
                await _dbContext.SaveChangesAsync();
                return Ok(new { message = "Login successful.", token, role = "User" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during Google login.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private async Task<GoogleJsonWebSignature.Payload> ValidateGoogleTokenAsync(string idToken)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {

                    Clock = GoogleLoginHelper.GetClock(),
                    Audience = new[] { _configuration["Google:ClientId"] } // Google Client ID từ cấu hình
                };

                return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            }
            catch (InvalidJwtException)
            {
                return null; // Token không hợp lệ
            }
        }


        [HttpPost("forgot-password/{email}")]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email is required.");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            string otp = GenerateOtp();
            _otpStorage[email] = otp; 
            try
            {
                await SendOtpEmailAsync(email, otp);
                return Ok(new { message = "OTP has been sent to your email.", step = "Please verify the OTP to reset your password." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email.");
                return StatusCode(500, "Failed to send OTP email. Please try again later.");
            }
        }


        [HttpPost("verify-otp-for-password/{email}/{otp}")]
        public IActionResult VerifyOtpForPassword(string email, string otp)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp))
            {
                return BadRequest("Email and OTP are required.");
            }

            if (!_otpStorage.TryGetValue(email, out var storedOtp) || storedOtp != otp)
            {
                return BadRequest("Invalid OTP provided.");
            }

            _otpStorage.Remove(email);
            return Ok(new { message = "OTP verified successfully. You can now reset your password." });
        }


        [HttpPost("reset-password/{email}")]
        public async Task<IActionResult> ResetPassword(string email, [FromBody] PasswordResetModel model)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email is required.");
            }

            if (model == null || string.IsNullOrEmpty(model.NewPassword) || model.NewPassword.Length < 6)
            {
                return BadRequest("Invalid password or password too short. Password must be at least 6 characters.");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return BadRequest("User not found.");
            }


            try
            {
                var removePasswordResult = await _userManager.RemovePasswordAsync(user);
                if (!removePasswordResult.Succeeded)
                {
                    var errors = string.Join(", ", removePasswordResult.Errors.Select(e => e.Description));
                    return BadRequest($"Failed to remove old password: {errors}");
                }

                var addPasswordResult = await _userManager.AddPasswordAsync(user, model.NewPassword);
                if (!addPasswordResult.Succeeded)
                {
                    var errors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));
                    return BadRequest($"Failed to set new password: {errors}");
                }

                return Ok(new { message = "Password has been successfully reset." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while resetting the password.");
                return StatusCode(500, "An error occurred while resetting the password. Please try again later.");
            }
        }

        private async Task SendOtpEmailAsync(string email, string otp)
        {
            string smtpEmail = _configuration["Smtp:Email"];
            string smtpPassword = _configuration["Smtp:Password"];
            string smtpHost = _configuration["Smtp:Host"];
            int smtpPort = int.Parse(_configuration["Smtp:Port"]);


            StringBuilder emailMessage = new StringBuilder();
            emailMessage.AppendLine("<html>");
            emailMessage.AppendLine("<body>");
            emailMessage.AppendLine($"<p>Dear {email},</p>");
            emailMessage.AppendLine("<p>We received a request to reset your password. Please use the following OTP code to reset your password:</p>");
            emailMessage.AppendLine($"<h2 style='color:blue'>{otp}</h2>");
            emailMessage.AppendLine("<p>This OTP code is valid for 15 minutes. If you did not request this, please ignore this email.</p>");
            emailMessage.AppendLine("<br>");
            emailMessage.AppendLine("<p>Best regards,</p>");
            emailMessage.AppendLine("<p><strong>DAI</strong></p>");
            emailMessage.AppendLine("</body>");
            emailMessage.AppendLine("</html>");

            string message = emailMessage.ToString();
            var emailToSend = new MimeMessage();
            emailToSend.To.Add(MailboxAddress.Parse(email));
            emailToSend.From.Add(MailboxAddress.Parse(smtpEmail));
            emailToSend.Subject = "Password Reset OTP - DAI";
            emailToSend.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = message };


            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(smtpEmail, smtpPassword);
            await smtp.SendAsync(emailToSend);
            await smtp.DisconnectAsync(true);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(Guid userId)
        {

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);


            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }


            HttpContext.Session.Clear(); // Xóa toàn bộ session
            user.IsOnline = false;


            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Logout successful." });
        }


        public class PasswordResetModel
        {
            public string NewPassword { get; set; }
        }

        public class RefreshTokenRequest
        {
            public string UserId { get; set; }
            public string RefreshToken { get; set; }
        }

        public class GoogleLoginRequest
        {
            public string IdToken { get; set; }
            public string ClientIp { get; set; }
        }

        public class UserRegistrationModel
        {
            public string Password { get; set; }
            public string FullName { get; set; }
            public string PhoneNumber { get; set; }
            public string UserName { get; set; }
        }

        public class LoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        public class GoogleClock : IClock
        {
            public DateTime Now => DateTime.Now.AddMinutes(5);

            public DateTime UtcNow => DateTime.UtcNow.AddMinutes(5);
        }

        public class GoogleLoginHelper
        {
            public static IClock GetClock()
            {
                return new GoogleClock();
            }
        }

    }
}