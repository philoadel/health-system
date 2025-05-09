using Microsoft.Extensions.Caching.Distributed;
using System.IdentityModel.Tokens.Jwt;
using UserAccountAPI.Services.Interfaces;

namespace UserAccountAPI.Services
{
    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly IDistributedCache _cache;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        public TokenBlacklistService(IDistributedCache cache)
        {
            _cache = cache;
            _tokenHandler = new JwtSecurityTokenHandler();
        }

        public async Task BlacklistTokenAsync(string token)
        {
            try
            {
                // Parse token to get expiration time
                var jwtToken = _tokenHandler.ReadJwtToken(token);
                var expiry = jwtToken.ValidTo;

                // Calculate how long until the token expires
                var timeUntilExpiry = expiry - DateTime.UtcNow;
                if (timeUntilExpiry.TotalMinutes <= 0)
                {
                    // Token already expired, no need to blacklist
                    return;
                }

                // Store token in cache until it expires
                await _cache.SetStringAsync(
                    $"blacklist_token_{token}",
                    "revoked",
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = timeUntilExpiry
                    });
            }
            catch (Exception)
            {
                // Handle invalid token format
            }
        }

        public async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            var result = await _cache.GetStringAsync($"blacklist_token_{token}");
            return result != null;
        }
    }
}


