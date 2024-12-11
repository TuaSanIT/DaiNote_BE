using dai.dataAccess.IRepositories;
using dai.core.Models;
using dai.dataAccess.DbContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.dataAccess.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext context;

    public UserRepository(AppDbContext context)
    {
        this.context = context;
    }


    public async Task<UserModel> CreateUserAsync(UserModel user)
    {
        user.Id = Guid.NewGuid();
        user.AddedOn = DateTime.Now;
        user.UpdatedOn = DateTime.Now;

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        context.Users.Remove(user);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<UserModel>> GetAllUsersAsync()
    {
        return await context.Users.ToListAsync();
    }

    public async Task<UserModel> GetUserByIdAsync(Guid userId)
    {
        return await context.Users.FindAsync(userId);
    }

    public async Task<UserModel> UpdateUserAsync(UserModel user)
    {
        var existingUser = await context.Users.FindAsync(user.Id);

        if (existingUser == null)
        {
            return null;
        }

        existingUser.UserName = user.UserName;
        existingUser.Email = user.Email;
        existingUser.PasswordHash = user.PasswordHash;
        existingUser.UpdatedOn = DateTime.UtcNow;

        context.Users.Update(existingUser);
        await context.SaveChangesAsync();

        return existingUser;
    }

    public async Task<UserModel> GetUserByEmailAsync(string email)
    {
        return await context.Users
                             .FirstOrDefaultAsync(u => u.Email == email);
    }

}
