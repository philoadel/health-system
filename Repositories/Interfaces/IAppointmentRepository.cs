using UserAccountAPI.Models;

namespace UserAccountAPI.Repositories.Interfaces
{
    public interface IAppointmentRepository
    {
        Task<IEnumerable<Appointment>> GetAllAsync();
        Task<Appointment> GetByIdAsync(int id);
        Task<IEnumerable<Appointment>> GetByPatientAsync(int patientId);
        Task<IEnumerable<Appointment>> GetByDoctorAsync(int doctorId);
        Task<Appointment> AddAsync(Appointment appointment);
        Task<Appointment> UpdateAsync(Appointment appointment);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<Appointment>> FilterAsync(DateTime? date = null, int? doctorId = null,
            int? patientId = null, AppointmentStatus? status = null);
        Task<bool> IsDoctorAvailableAsync(int doctorId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeAppointmentId = null);
    }
}
