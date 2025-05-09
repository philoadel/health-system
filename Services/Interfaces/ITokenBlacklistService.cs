namespace UserAccountAPI.Services.Interfaces
{
    public interface ITokenBlacklistService
    {
        Task BlacklistTokenAsync(string token);
        Task<bool> IsTokenBlacklistedAsync(string token);
    }
}
