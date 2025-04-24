using System.Collections.Generic;
using System.Threading.Tasks;
using UserAccountAPI.DTOs;
using UserAccountAPI.Models;

namespace UserAccountAPI.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<UserDTO> GetUserByIdAsync(string userId);
        Task<UserDTO> UpdateUserAsync(string userId, UpdateUserDTO model);
        Task<bool> DeleteUserAsync(string userId);
        Task<List<UserDTO>> GetAllUsersAsync(int page = 1, int pageSize = 10);
    }
}
