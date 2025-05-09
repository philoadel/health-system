using UserAccountAPI.Data;
using UserAccountAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UserAccountAPI.Models;

namespace UserAccountAPI.Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AppointmentRepository> _logger;

        public AppointmentRepository(ApplicationDbContext context, ILogger<AppointmentRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Appointment>> GetAllAsync()
        {
            return await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .ToListAsync();
        }

        public async Task<Appointment> GetByIdAsync(int id)
        {
            return await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<Appointment>> GetByPatientAsync(int patientId)
        {
            return await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Where(a => a.PatientId == patientId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Appointment>> GetByDoctorAsync(int doctorId)
        {
            return await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == doctorId)
                .ToListAsync();
        }

        public async Task<Appointment> AddAsync(Appointment appointment)
        {
            await _context.Appointments.AddAsync(appointment);
            await _context.SaveChangesAsync();
            return appointment;
        }

        public async Task<Appointment> UpdateAsync(Appointment appointment)
        {
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();
            return appointment;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return false;

            _context.Appointments.Remove(appointment);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<Appointment>> FilterAsync(DateTime? date = null, int? doctorId = null,
            int? patientId = null, AppointmentStatus? status = null)
        {
            var query = _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .AsQueryable();

            if (date.HasValue)
            {
                query = query.Where(a => a.AppointmentDate.Date == date.Value.Date);
            }

            if (doctorId.HasValue)
            {
                query = query.Where(a => a.DoctorId == doctorId.Value);
            }

            if (patientId.HasValue)
            {
                query = query.Where(a => a.PatientId == patientId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<bool> IsDoctorAvailableAsync(int doctorId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeAppointmentId = null)
        {
            try
            {
                _logger.LogInformation($"Checking availability for Doctor ID {doctorId} on {date.ToShortDateString()} from {startTime} to {endTime}");

                var doctor = await _context.Doctors
                    .Include(d => d.WorkingHours)
                    .FirstOrDefaultAsync(d => d.Id == doctorId);

                if (doctor == null)
                {
                    _logger.LogWarning($"Doctor with ID {doctorId} not found");
                    return false;
                }

                if (endTime <= startTime)
                {
                    _logger.LogInformation($"Invalid appointment times: End time ({endTime}) must be after start time ({startTime})");
                    return false;
                }

                var dayOfWeek = date.DayOfWeek;
                bool isWithinWorkingHours = false;

                // Check if doctor has working hours for the specified day
                var workingHours = doctor.WorkingHours.FirstOrDefault(wh => (int)wh.DayOfWeek == (int)dayOfWeek);

                if (workingHours != null)
                {
                    _logger.LogInformation($"Doctor has working hours on {dayOfWeek}: {workingHours.StartTime} - {workingHours.EndTime}");

                    // Check if the requested time is within the doctor's working hours
                    isWithinWorkingHours = startTime >= workingHours.StartTime && endTime <= workingHours.EndTime;

                    if (!isWithinWorkingHours)
                    {
                        _logger.LogInformation($"Requested time {startTime}-{endTime} is outside working hours {workingHours.StartTime}-{workingHours.EndTime}");
                        return false;
                    }
                }
                else
                {
                    // If no working hours found for this day, doctor is not available
                    _logger.LogInformation($"Doctor is not available on {dayOfWeek} because no working hours are set.");
                    return false;
                }

                // Check for overlapping appointments
                var query = _context.Appointments
                    .Where(a => a.DoctorId == doctorId &&
                                a.AppointmentDate.Date == date.Date &&
                                a.Status != AppointmentStatus.Cancelled);

                if (excludeAppointmentId.HasValue)
                {
                    query = query.Where(a => a.Id != excludeAppointmentId.Value);
                }

                var overlappingAppointments = await query
                    .Where(a => (startTime < a.EndTime && endTime > a.StartTime))
                    .ToListAsync();

                if (overlappingAppointments.Any())
                {
                    _logger.LogInformation($"Found {overlappingAppointments.Count} overlapping appointments");
                    foreach (var appointment in overlappingAppointments)
                    {
                        _logger.LogInformation($"Overlapping appointment ID: {appointment.Id}, Time: {appointment.StartTime} - {appointment.EndTime}");
                    }
                    return false;
                }

                _logger.LogInformation($"Doctor {doctorId} is available on {date.ToShortDateString()} from {startTime} to {endTime}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking doctor availability: {ex.Message}");
                return false;
            }
        }
    }
}