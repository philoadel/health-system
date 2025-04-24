using System.Collections.Generic;
using System.Threading.Tasks;
using UserAccountAPI.DTOs;
using UserAccountAPI.Models;

namespace UserAccountAPI.Services.Interfaces
{
    public interface ITokenService
    {
        Task<AuthResponseDTO> GenerateTokensAsync(ApplicationUser user, IList<string> roles = null);
        Task<AuthResponseDTO> RefreshTokenAsync(string accessToken, string refreshToken);
    }
}