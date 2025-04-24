using UserAccountAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace UserAccountAPI.DTOs
{
    public class DoctorDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Specialty { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsAvailableToday { get; set; }
        public int? DepartmentId { get; set; }
        public string UserId { get; set; }
        public ICollection<WorkingHoursDto> WorkingHours { get; set; } = new List<WorkingHoursDto>();
    }

    public class DoctorCreateDto
    {
        public string Name { get; set; }
        public string Specialty { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsAvailableToday { get; set; }
        public int? DepartmentId { get; set; }
        public string UserId { get; set; } 
    }

    public class DoctorUpdateDto
    {
        public string Name { get; set; }
        public string Specialty { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsAvailableToday { get; set; }
        public int? DepartmentId { get; set; }
    }

    public class DoctorRegisterDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [MinLength(8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Specialty { get; set; }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        public int? DepartmentId { get; set; }
    }

    public class WorkingHoursDto
    {
        public DayOfWeek DayOfWeek { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }
}
