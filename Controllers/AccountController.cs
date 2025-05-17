﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using UserAccountAPI.DTOs;
using UserAccountAPI.Models;
using UserAccountAPI.Repositories.Interfaces;
using UserAccountAPI.Services.Interfaces;
using UserAccountAPI.Repositories;
using System.Linq;

namespace UserAccountAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IPatientRepository _patientRepository;

        public AccountController(
            IUserRepository userRepository,
            IAuthService authService,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IEmailService emailService,
            IDoctorRepository doctorRepository = null,
            IPatientRepository patientRepository = null)
        {
            _userRepository = userRepository;
            _authService = authService;
            _userManager = userManager;
            _configuration = configuration;
            _emailService = emailService;
            _doctorRepository = doctorRepository;
            _patientRepository = patientRepository;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(user);

        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserDTO model)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            // Update user in ApplicationUser table
            var updatedUser = await _userRepository.UpdateUserAsync(userId, model);
            if (updatedUser == null)
            {
                return NotFound(new { message = "User not found." });
            }

            // Update doctor name if user is a doctor
            if (_doctorRepository != null && User.IsInRole("Doctor"))
            {
                var doctor = (await _doctorRepository.GetAllDoctors())
                                .FirstOrDefault(d => d.UserId == userId);

                if (doctor != null)
                {
                    doctor.Name = $"{updatedUser.FirstName} {updatedUser.LastName}";
                    await _doctorRepository.UpdateDoctor(doctor);
                }
            }

            // Update patient name if user is a patient
            if (_patientRepository != null && User.IsInRole("Patient"))
            {
                var patient = (await _patientRepository.GetAllPatients())
                                .FirstOrDefault(p => p.UserId == userId);

                if (patient != null)
                {
                    patient.FullName = $"{updatedUser.FirstName} {updatedUser.LastName}";
                    await _patientRepository.UpdatePatient(patient);
                }
            }

            return Ok(updatedUser);
        }

        [HttpDelete("profile")]
        public async Task<IActionResult> DeleteUserProfile()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new { message = "Invalid or missing user ID." });
            }

            // Get the user from UserManager
            var user = await _userManager.FindByIdAsync(userIdString);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            // Delete related Doctor data if user is a Doctor
            if (_doctorRepository != null && User.IsInRole("Doctor"))
            {
                var doctor = (await _doctorRepository.GetAllDoctors())
                                .FirstOrDefault(d => d.UserId == int.Parse(userIdString));
                if (doctor != null)
                {
                    await _doctorRepository.DeleteDoctor(doctor.Id); // Pass doctor.Id
                }
            }

            // Delete related Patient data if user is a Patient
            if (_patientRepository != null && User.IsInRole("Patient"))
            {
                var patient = (await _patientRepository.GetAllPatients())
                                .FirstOrDefault(p => p.UserId == int.Parse(userIdString));
                if (patient != null)
                {
                    await _patientRepository.DeletePatient(patient.Id); // Pass patient.Id
                }
            }

            // Delete the user from Identity
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Failed to delete user.", errors = result.Errors });
            }

            return Ok(new { message = "User account has been deleted successfully." });
        }

        [HttpGet("email-verified")]
        public async Task<IActionResult> IsEmailVerified()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var isVerified = await _authService.IsEmailConfirmedAsync(userId);
            return Ok(new { isVerified });
        }

        [HttpPost("resend-confirmation")]
        [Authorize]
        public async Task<IActionResult> ResendConfirmationEmail()
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized(new { message = "Invalid or missing user ID." });
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            if (user.EmailConfirmed)
            {
                return BadRequest(new { message = "Email already confirmed." });
            }

            await _authService.GenerateAndSendEmailConfirmationCodeAsync(user);

            return Ok(new { message = "Confirmation code sent successfully to your email." });
        }
    }
}