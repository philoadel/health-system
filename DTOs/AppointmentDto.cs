using System;
using System.ComponentModel.DataAnnotations;

namespace UserAccountAPI.DTOs
{
    public class AppointmentDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; }
    }


    public class AppointmentCreateDto
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public string StartTime { get; set; }

        [Required]
        public string EndTime { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }
    }


    public class AppointmentUpdateDto
    {
        public DateTime AppointmentDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Notes { get; set; }
    }


    public class AppointmentStatusUpdateDto
    {
        [Required]
        [EnumDataType(typeof(AppointmentStatus))]
        public string Status { get; set; }
        public string Notes { get; set; }
    }
}
