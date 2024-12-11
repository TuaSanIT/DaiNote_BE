using dai.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.dataAccess.IRepositories;

public interface IUserRepository
{
    Task<IEnumerable<UserModel>> GetAllUsersAsync();
    Task<UserModel> GetUserByIdAsync(Guid userId);
    Task<UserModel> CreateUserAsync(UserModel user);
    Task<UserModel> UpdateUserAsync(UserModel user);
    Task<bool> DeleteUserAsync(Guid userId);
    Task<UserModel> GetUserByEmailAsync(string email);
}
