// Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;
using System;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public bool IsActive { get; set; } = true;
    public string NationalId { get; set; }
    public DateTime? DateOfBirth { get; set; }
}