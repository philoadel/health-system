using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserAccountAPI.DTOs;
using UserAccountAPI.Models;

namespace UserAccountAPI.Services.Interfaces
{
    public interface IAuthService
    {
        Task<(IdentityResult Result, ApplicationUser User)> RegisterUserAsync(RegisterDTO model);
        Task<(SignInResult Result, ApplicationUser User, IList<string> Roles)> LoginUserAsync(LoginDTO model);
        Task LogoutAsync();
        Task<bool> ForgotPasswordAsync(string email);
        Task<IdentityResult> ResetPasswordAsync(ResetPasswordDTO model);
        Task<IdentityResult> ChangePasswordAsync(string userId, ChangePasswordDTO model);
        Task<bool> ConfirmEmailAsync(EmailConfirmationDTO model);
        Task<AuthResponseDTO> GenerateTokenAsync(ApplicationUser user);
        Task<AuthResponseDTO> RefreshTokenAsync(RefreshTokenDTO model);
        Task<bool> IsEmailConfirmedAsync(string userId);
        Task<ApplicationUser> GetUserByEmailAsync(string email);
        Task<ApplicationUser> GetUserByIdAsync(string id);
        Task<IList<string>> GetUserRolesAsync(string userId);
        Task<bool> AddUserToRoleAsync(string userId, string role);
        Task<bool> RemoveUserFromRoleAsync(string userId, string role);
    }
}