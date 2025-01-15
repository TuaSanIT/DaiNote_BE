using dai.core.DTO.User;
using dai.dataAccess.IRepositories;
using dai.core.Models;
using Microsoft.AspNetCore.Mvc;
using dai.core.DTO.Service;
using Microsoft.EntityFrameworkCore;
using dai.api.Helper;
using dai.dataAccess.DbContext;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Azure.Storage.Blobs;
using dai.api.Services.ServicesAPI;
using dai.api.Services.ServiceExtension;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace dai.api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly UserManager<UserModel> _userManager;
    private readonly IConfiguration _configuration;
    private readonly AzureBlobService _storageService;

    public UserController(AppDbContext context, IUserRepository userRepository, UserManager<UserModel> userManager, IConfiguration configuration, AzureBlobService storageService)
    {
        _context = context;
        _userRepository = userRepository;
        _userManager = userManager;
        _configuration = configuration;
        _storageService = storageService;
    }

    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate([FromBody] UserModel userObj)
    {
        if (userObj == null)
            return BadRequest(new { Message = "Invalid user object" });

        // Fetch the user with the provided email from the database
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == userObj.Email);

        if (user == null)
            return NotFound(new { Message = "User Not Found" });

        // Verify the provided password against the stored hashed password
        if (!PasswordHasher.VerifyPassword(userObj.PasswordHash, user.PasswordHash))
            return Unauthorized(new { Message = "Invalid Password" });

        user.Token = CreateJwt(user);
        var newAccessToken = user.Token;
        var newRefreshToken = CreateRefreshToken();
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.Now.AddDays(5);
        await _context.SaveChangesAsync();

        return Ok(new TokenApiDto()
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserModel>>> GetAllUsers()
    {
        var users = await _userRepository.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserModel>> GetUserById(Guid id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        // Return the user including the AvatarImage URL
        return Ok(new UserModel
        {
            UserName = user.UserName,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            TimeZoneId = user.TimeZoneId,
            Email = user.Email,
            AvatarImage = user.AvatarImage, // Make sure this property exists in UserModel
                                            // Include other properties as needed
            AddedOn = user.AddedOn,
            UpdatedOn = user.UpdatedOn,
            // etc...
        });
    }

    [HttpPost]
    public async Task<ActionResult<UserModel>> PostUser(POST_User postUser)
    {
        var user = new UserModel
        {
            UserName = postUser.UserName,
            FullName = postUser.FullName,
            Email = postUser.UserEmail,
            PasswordHash = postUser.UserPassword,
            TimeZoneId = postUser.TimeZoneId,
            AddedOn = DateTime.UtcNow,
            UpdatedOn = DateTime.UtcNow,
        };

        var createdUser = await _userRepository.CreateUserAsync(user);

        return CreatedAtAction(nameof(GetUserById),
            new { id = createdUser.Id },
            createdUser);
    }

    [HttpPut("changePassword/{id}")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordDTO changePasswordDto)
    {
        var existingUser = await _userManager.FindByIdAsync(id.ToString());

        if (existingUser == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var passwordVerificationResult = await _userManager.CheckPasswordAsync(existingUser, changePasswordDto.OldPassword);
        if (!passwordVerificationResult)
        {
            return BadRequest(new { Message = "Incorrect old password" });
        }

        if (string.IsNullOrEmpty(changePasswordDto.NewPassword))
        {
            return BadRequest(new { Message = "New password is required" });
        }

        var result = await _userManager.ChangePasswordAsync(existingUser, changePasswordDto.OldPassword, changePasswordDto.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { Message = "Failed to change password", Errors = result.Errors });
        }

        existingUser.UpdatedOn = DateTime.UtcNow;

        await _userManager.UpdateAsync(existingUser);

        return Ok(new { Message = "Password changed successfully" });
    }

    [HttpPut("editProfile/{id}")]
    public async Task<ActionResult> EditProfile(Guid id, [FromForm] PUT_User userUpdated)
    {
        var existingUser = await _userManager.FindByIdAsync(id.ToString());

        if (existingUser == null)
            return NotFound(new { Message = "User not found" });

        if (!string.IsNullOrEmpty(userUpdated.UserName))
            existingUser.UserName = userUpdated.UserName;

        if (!string.IsNullOrEmpty(userUpdated.UserContact))
            existingUser.PhoneNumber = userUpdated.UserContact;

        if (!string.IsNullOrEmpty(userUpdated.TimeZoneId))
            existingUser.TimeZoneId = userUpdated.TimeZoneId;

        string newAvatarUrl = null;
        if (userUpdated.AvatarImage != null)
        {
            try
            {
                // Xóa ảnh cũ nếu tồn tại
                if (!string.IsNullOrEmpty(existingUser.AvatarImage))
                {
                    var deleteResult = await _storageService.DeleteFileAsync(existingUser.AvatarImage);
                    if (!deleteResult)
                        return BadRequest(new { Message = "Failed to delete old avatar image" });
                }

                // Upload ảnh mới
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(userUpdated.AvatarImage.FileName)}";
                var folderName = "avatars";
                var containerName = _configuration["AzureBlobStorage:ContainerName"];

                var imageUrl = await _storageService.UploadImageAsync(userUpdated.AvatarImage.OpenReadStream(), containerName, folderName, fileName);

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    existingUser.AvatarImage = imageUrl;
                    newAvatarUrl = imageUrl; // lưu URL ảnh mới để trả về phía client
                }
                else
                    return BadRequest(new { Message = "Failed to upload new avatar image" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "An error occurred while uploading avatar image", Error = ex.Message });
            }
        }

        existingUser.UpdatedOn = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(existingUser);
        if (!updateResult.Succeeded)
            return BadRequest(new { Message = "Failed to update profile", Errors = updateResult.Errors });

        // Trả về URL avatar mới trong response
        return Ok(new { Message = "Profile updated successfully", NewAvatarUrl = newAvatarUrl });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DELETEUser(Guid id)
    {
        var result = await _userRepository.DeleteUserAsync(id);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("register")]

    public async Task<IActionResult> RegisterUser([FromBody] UserModel userObj)
    {
        if (userObj == null)
            return BadRequest();

        if (await CheckEmailExistAsync(userObj.Email))
            return BadRequest(new { Message = "Email Already Exist!" });


        userObj.PasswordHash = PasswordHasher.HashPassword(userObj.PasswordHash);
        userObj.Token = "";
        await _context.Users.AddAsync(userObj);
        await _context.SaveChangesAsync();
        return Ok(new
        {
            Message = "User Registered!"
        });
    }

    private Task<bool> CheckEmailExistAsync(string email)
            => _context.Users.AnyAsync(x => x.Email == email);

    private bool UserExists(Guid id)
    {
        return _context.Users.Any(e => e.Id == id);
    }

    private string CreateJwt(UserModel user)
    {
        var jwtTokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("veryverysupersecretkeythatneedstobelongenough");

        var identity = new ClaimsIdentity(new Claim[]
        {
        new Claim(ClaimTypes.Name, user.FullName),
        });

        var credential = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = identity,
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = credential
        };

        var token = jwtTokenHandler.CreateToken(tokenDescriptor);
        return jwtTokenHandler.WriteToken(token);
    }

    private string CreateRefreshToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var refreshToken = Convert.ToBase64String(tokenBytes);

        var tokenInUser = _context.Users
            .Any(a => a.RefreshToken == refreshToken);
        if (tokenInUser)
        {
            return CreateRefreshToken();
        }
        return refreshToken;
    }
    private ClaimsPrincipal GetPrincipalFromExpireToken(string token)
    {
        var key = Encoding.ASCII.GetBytes("veryverysupersecretkeythatneedstobelongenough");
        var tokenValidationParameters = new TokenValidationParameters


        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateLifetime = false,
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken securityToken;
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
        var jwtSecurityToken = securityToken as JwtSecurityToken;

        if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("This is in valid Token");
        {
            return principal;
        }
    }
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(TokenApiDto tokenApiDto)
    {
        if (tokenApiDto == null)
            return BadRequest("Invalid Client Request");
        string accessToken = tokenApiDto.AccessToken;
        string refreshToken = tokenApiDto.RefreshToken;
        var principal = GetPrincipalFromExpireToken(accessToken);
        var name = principal.Identity.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.FullName == name);
        if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
            return BadRequest("Invalid Request");
        var newAccessToken = CreateJwt(user);
        var newRefreshToken = CreateRefreshToken();
        user.RefreshToken = newRefreshToken;
        await _context.SaveChangesAsync();
        return Ok(new TokenApiDto()
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
        });
    }


}
