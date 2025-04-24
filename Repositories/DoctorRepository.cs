// DoctorRepository.cs
using UserAccountAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserAccountAPI.Data;

namespace UserAccountAPI.Repositories
{
    public class DoctorRepository : IDoctorRepository
    {
        private readonly ApplicationDbContext _context;

        public DoctorRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Doctor>> GetAllDoctors()
        {
            return await _context.Doctors.ToListAsync();
        }

        public async Task<Doctor> GetDoctorById(int id)
        {
            return await _context.Doctors.FindAsync(id);
        }

        public async Task<Doctor> GetDoctorByUserId(string userId)
        {
            return await _context.Doctors
                .FirstOrDefaultAsync(d => d.UserId == userId);
        }

        public async Task<Doctor> AddDoctor(Doctor doctor)
        {
            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();
            return doctor;
        }

        public async Task<Doctor> UpdateDoctor(Doctor doctor)
        {
            _context.Entry(doctor).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return doctor;
        }

        public async Task<bool> DeleteDoctor(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return false;

            _context.Doctors.Remove(doctor);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Doctor>> GetDoctorsByDepartment(int departmentId)
        {
            return await _context.Doctors
                .Where(d => d.DepartmentId == departmentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Doctor>> GetDoctorsBySpecialty(string specialty)
        {
            return await _context.Doctors
                .Where(d => d.Specialty.ToLower() == specialty.ToLower())
                .ToListAsync();
        }

        public async Task<IEnumerable<Doctor>> GetAvailableDoctorsToday()
        {
            return await _context.Doctors
                .Where(d => d.IsAvailableToday)
                .ToListAsync();
        }

        public async Task<Doctor> UpdateWorkingHours(int doctorId, List<WorkingHours> workingHours)
        {
            var doctor = await _context.Doctors
                .Include(d => d.WorkingHours)
                .FirstOrDefaultAsync(d => d.Id == doctorId);

            if (doctor == null) return null;

            if (doctor.WorkingHours == null)
            {
                doctor.WorkingHours = new List<WorkingHours>(workingHours);
            }
            else
            {
                foreach (var newHours in workingHours)
                {
                    var existingHours = doctor.WorkingHours
                        .FirstOrDefault(wh => wh.DayOfWeek == newHours.DayOfWeek);

                    if (existingHours != null)
                    {
                        existingHours.StartTime = newHours.StartTime;
                        existingHours.EndTime = newHours.EndTime;
                    }
                    else
                    {
                        doctor.WorkingHours.Add(newHours);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return doctor;
        }
        public async Task<Doctor> AssignDepartment(int doctorId, int departmentId)
        {
            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null) return null;

            doctor.DepartmentId = departmentId;
            await _context.SaveChangesAsync();
            return doctor;
        }

        public async Task<IEnumerable<Doctor>> FilterDoctors(string specialty, bool? available)
        {
            var query = _context.Doctors.AsQueryable();

            if (!string.IsNullOrEmpty(specialty))
                query = query.Where(d => d.Specialty.Contains(specialty));

            if (available.HasValue)
                query = query.Where(d => d.IsAvailableToday == available);

            return await query.ToListAsync();
        }
    }
}