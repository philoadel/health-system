using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserAccountAPI.Models;
using UserAccountAPI.Repositories;
using AutoMapper;
using UserAccountAPI.DTOs;
using System.Linq;
using System.Security.Claims;

namespace MedicalAPI_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoctorsController : ControllerBase
    {
        private readonly IDoctorRepository _doctorRepository;
        private readonly IMapper _mapper;

        public DoctorsController(IDoctorRepository doctorRepository, IMapper mapper)
        {
            _doctorRepository = doctorRepository;
            _mapper = mapper;
        }

        // GET: api/Doctors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DoctorDto>>> GetDoctors()
        {
            var doctors = await _doctorRepository.GetAllDoctors();
            return _mapper.Map<List<DoctorDto>>(doctors);
        }

        // GET: api/Doctors/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DoctorDto>> GetDoctor(int id)
        {
            var doctor = await _doctorRepository.GetDoctorById(id);

            if (doctor == null)
            {
                return NotFound();
            }

            return _mapper.Map<DoctorDto>(doctor);
        }

        // GET: api/Doctors/department/5
        [HttpGet("department/{departmentId}")]
        public async Task<ActionResult<IEnumerable<DoctorDto>>> GetDoctorsByDepartment(int departmentId)
        {
            var doctors = await _doctorRepository.GetDoctorsByDepartment(departmentId);
            return _mapper.Map<List<DoctorDto>>(doctors);
        }

        // GET: api/Doctors/specialty/Cardiology
        [HttpGet("specialty/{specialty}")]
        public async Task<ActionResult<IEnumerable<DoctorDto>>> GetDoctorsBySpecialty(string specialty)
        {
            var doctors = await _doctorRepository.GetDoctorsBySpecialty(specialty);
            return _mapper.Map<List<DoctorDto>>(doctors);
        }

        // GET: api/Doctors/available
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<DoctorDto>>> GetAvailableDoctors()
        {
            var doctors = await _doctorRepository.GetAvailableDoctorsToday();
            return _mapper.Map<List<DoctorDto>>(doctors);
        }

        // PUT: api/Doctors/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<ActionResult<DoctorDto>> PutDoctor(int id, DoctorUpdateDto doctorDto)
        {
            // Get the existing doctor first
            var existingDoctor = await _doctorRepository.GetDoctorById(id);

            if (existingDoctor == null)
            {
                return NotFound();
            }

            // Check if the user is a doctor updating their own profile
            if (User.IsInRole("Doctor") && !User.IsInRole("Admin"))
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (existingDoctor.UserId != userId)
                {
                    return Forbid();
                }
            }

            // Update the properties from the DTO
            existingDoctor.Name = doctorDto.Name;
            existingDoctor.Specialty = doctorDto.Specialty;
            existingDoctor.PhoneNumber = doctorDto.PhoneNumber;
            existingDoctor.IsAvailableToday = doctorDto.IsAvailableToday;
            existingDoctor.DepartmentId = doctorDto.DepartmentId;

            var updatedDoctor = await _doctorRepository.UpdateDoctor(existingDoctor);

            return _mapper.Map<DoctorDto>(updatedDoctor);
        }

        // POST: api/Doctors
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DoctorDto>> PostDoctor(DoctorCreateDto doctorDto)
        {
            // Check if a doctor with this UserId already exists
            if (!string.IsNullOrEmpty(doctorDto.UserId))
            {
                var existingDoctor = await _doctorRepository.GetDoctorByUserId(doctorDto.UserId);
                if (existingDoctor != null)
                {
                    return Conflict($"A doctor with User ID '{doctorDto.UserId}' already exists.");
                }
            }

            // Map the DTO to a Doctor entity
            var doctor = new Doctor
            {
                Name = doctorDto.Name,
                Specialty = doctorDto.Specialty,
                PhoneNumber = doctorDto.PhoneNumber,
                IsAvailableToday = doctorDto.IsAvailableToday,
                DepartmentId = doctorDto.DepartmentId,
                UserId = doctorDto.UserId
            };

            var newDoctor = await _doctorRepository.AddDoctor(doctor);

            return CreatedAtAction(
                nameof(GetDoctor),
                new { id = newDoctor.Id },
                _mapper.Map<DoctorDto>(newDoctor)
            );
        }

        // DELETE: api/Doctors/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            var result = await _doctorRepository.DeleteDoctor(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        // PUT: api/Doctors/5/department/2
        [HttpPut("{id}/department/{departmentId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DoctorDto>> AssignDepartment(int id, int departmentId)
        {
            // No need for a DTO here since we're just passing a simple departmentId
            var doctor = await _doctorRepository.AssignDepartment(id, departmentId);

            if (doctor == null)
            {
                return NotFound();
            }

            return _mapper.Map<DoctorDto>(doctor);
        }
        // PUT: api/Doctors/5/working-hours
        [HttpPut("{id}/working-hours")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<ActionResult<DoctorDto>> UpdateWorkingHours(int id, List<WorkingHoursDto> workingHoursDto)
        {
            // Check if the user is a doctor updating their own working hours
            if (User.IsInRole("Doctor") && !User.IsInRole("Admin"))
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var existingDoctor = await _doctorRepository.GetDoctorById(id);

                if (existingDoctor == null)
                {
                    return NotFound();
                }

                if (existingDoctor.UserId != userId)
                {
                    return Forbid();
                }
            }

            // Convert DTOs to entities
            var workingHoursList = workingHoursDto.Select(wh => new WorkingHours
            {
                DoctorId = id,
                DayOfWeek = wh.DayOfWeek,
                StartTime = TimeSpan.Parse(wh.StartTime),
                EndTime = TimeSpan.Parse(wh.EndTime)
            }).ToList();

            var doctor = await _doctorRepository.UpdateWorkingHours(id, workingHoursList);

            if (doctor == null)
            {
                return NotFound();
            }

            return _mapper.Map<DoctorDto>(doctor);
        }

        // GET: api/Doctors/filter?specialty=Cardiology&available=true
        [HttpGet("filter")]
        public async Task<ActionResult<IEnumerable<DoctorDto>>> FilterDoctors([FromQuery] string specialty, [FromQuery] bool? available)
        {
            var doctors = await _doctorRepository.FilterDoctors(specialty, available);
            return _mapper.Map<List<DoctorDto>>(doctors);
        }

        // GET: api/Doctors/profile
        [HttpGet("profile")]
        [Authorize(Roles = "Doctor")]
        public async Task<ActionResult<DoctorDto>> GetDoctorProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var doctor = await _doctorRepository.GetDoctorByUserId(userId);

            if (doctor == null)
            {
                return NotFound();
            }

            return _mapper.Map<DoctorDto>(doctor);
        }
    }
}