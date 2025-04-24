using UserAccountAPI.DTOs;
using UserAccountAPI.Models;

namespace UserAccountAPI.Services
{
    public interface IAppointmentService
    {
        Task<IEnumerable<AppointmentDto>> GetAllAppointmentsAsync();
        Task<AppointmentDto> GetAppointmentByIdAsync(int id);
        Task<IEnumerable<AppointmentDto>> GetAppointmentsByPatientAsync(int patientId);
        Task<IEnumerable<AppointmentDto>> GetAppointmentsByDoctorAsync(int doctorId);
        Task<AppointmentDto> CreateAppointmentAsync(AppointmentCreateDto appointmentDto);
        Task<AppointmentDto> UpdateAppointmentAsync(int id, AppointmentUpdateDto appointmentDto);
        Task<bool> DeleteAppointmentAsync(int id);
        Task<AppointmentDto> UpdateStatusAsync(int appointmentId, AppointmentStatusUpdateDto statusDto);
        Task<bool> CheckDoctorAvailabilityAsync(int doctorId, DateTime date, string startTime, string endTime);
        Task<IEnumerable<AppointmentDto>> FilterAppointmentsAsync(DateTime? date, int? doctorId, int? patientId, string status);

        // Additional methods for authorization checks
        Task<bool> IsAppointmentForUserAsync(int appointmentId, string userId);
        Task<bool> IsAppointmentForDoctorAsync(int appointmentId, string userId);
        Task<bool> IsPatientUserAsync(int patientId, string userId);
        Task<bool> IsDoctorUserAsync(int doctorId, string userId);
        Task<int?> GetPatientIdFromUserIdAsync(string userId);
        Task<int?> GetDoctorIdFromUserIdAsync(string userId);
    }
}