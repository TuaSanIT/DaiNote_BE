using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using dai.core.Models;

namespace dai.dataAccess.DbContext
{
    public class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<UserRoleModel>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserModel>>();

            await SeedRolesAsync(roleManager);
            await SeedUsersAsync(userManager);
        }

        private static async Task SeedRolesAsync(RoleManager<UserRoleModel> roleManager)
        {
            string[] roleNames = { "Admin", "User" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new UserRoleModel { Name = roleName };
                    var result = await roleManager.CreateAsync(role);

                    if (!result.Succeeded)
                    {
                        Console.WriteLine($"Error creating role {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }

        private static async Task SeedUsersAsync(UserManager<UserModel> userManager)
        {
            await CreateUserIfNotExistsAsync(userManager, "admin", "Admin@123", "Admin");
        }

        private static async Task CreateUserIfNotExistsAsync(UserManager<UserModel> userManager, string username, string password, string role)
        {
            var existingUser = await userManager.FindByNameAsync(username);
            if (existingUser == null)
            {
                var user = new UserModel
                {
                    FullName = username,
                    UserName = username,
                    Email = $"{username}@admin.com",
                    RefreshToken = Guid.NewGuid().ToString(),
                    Token = Guid.NewGuid().ToString(),
                    EmailConfirmed = true,
                    IsOnline = false,
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
                else
                {
                    Console.WriteLine($"Error creating user {username}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}
