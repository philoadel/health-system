using Microsoft.AspNetCore.Authorization;
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
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
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
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Update user in ApplicationUser table
            var updatedUser = await _userRepository.UpdateUserAsync(userId, model);
            if (updatedUser == null)
            {
                return NotFound(new { message = "User not found." });
            }

            // Check if user is a doctor and update corresponding record
            if (_doctorRepository != null && User.IsInRole("Doctor"))
            {
                var doctors = await _doctorRepository.GetAllDoctors();
                var doctor = doctors.FirstOrDefault(d => d.UserId == userId);

                if (doctor != null)
                {
                    // Update relevant doctor fields that should be synchronized with user profile
                    doctor.Name = $"{model.FirstName} {model.LastName}";
                    await _doctorRepository.UpdateDoctor(doctor);
                }
            }

            // Check if user is a patient and update corresponding record
            if (_patientRepository != null && User.IsInRole("Patient"))
            {
                var patients = await _patientRepository.GetAllPatients();
                var patient = patients.FirstOrDefault(p => p.UserId == userId);

                if (patient != null)
                {
                    // Update relevant patient fields that should be synchronized with user profile
                    patient.FullName = $"{model.FirstName} {model.LastName}";
                    await _patientRepository.UpdatePatient(patient);
                }
            }

            return Ok(updatedUser);
        }

        [HttpDelete("profile")]
        public async Task<IActionResult> DeleteUserProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _userRepository.DeleteUserAsync(userId);
            if (!result)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(new { message = "User account has been deactivated successfully." });
        }

        [HttpGet("email-verified")]
        public async Task<IActionResult> IsEmailVerified()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var isVerified = await _authService.IsEmailConfirmedAsync(userId);
            return Ok(new { isVerified });
        }

        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmationEmail()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
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

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var encodedToken = HttpUtility.UrlEncode(token);
            var clientUrl = _configuration["ClientUrl"];
            var confirmationLink = $"{clientUrl}/confirm-email?userId={user.Id}&token={encodedToken}";

            await _emailService.SendEmailConfirmationAsync(
                user.Email,
                confirmationLink,
                user.Id,
                token
            );

            return Ok(new { message = "Confirmation email sent successfully." });
        }
    }
}