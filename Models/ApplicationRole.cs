using Microsoft.AspNetCore.Identity;

namespace UserAccountAPI.Models
{
    public class ApplicationRole : IdentityRole
    {
        public string? Description { get; set; }
    }
}