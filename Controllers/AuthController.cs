using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UserAccountAPI.DTOs;
using UserAccountAPI.Services.Interfaces;

namespace UserAccountAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (result, user) = await _authService.RegisterUserAsync(model);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(ModelState);
            }

            // Generate tokens immediately after registration
            var authResponse = await _authService.GenerateTokenAsync(user);

            // Return only the user object
            return Ok(authResponse.User);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (result, user, roles) = await _authService.LoginUserAsync(model);

            if (result.IsLockedOut)
            {
                return BadRequest(new { message = "Your account is locked. Please try again later or contact support." });
            }

            if (!result.Succeeded)
            {
                if (user != null && !await _authService.IsEmailConfirmedAsync(user.Id))
                {
                    return BadRequest(new { message = "Please confirm your email before logging in." });
                }
                return BadRequest(new { message = "Invalid login attempt." });
            }

            // Generate auth response with tokens
            var authResponse = await _authService.GenerateTokenAsync(user);

            // Return only the user object with all token information included
            return Ok(authResponse.User);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return Ok(new { message = "Logged out successfully." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _authService.ForgotPasswordAsync(model.Email);

            return Ok(new { message = "If your email is registered, you will receive a password reset link." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.ResetPasswordAsync(model);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(ModelState);
            }

            return Ok(new { message = "Password has been reset successfully." });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _authService.ChangePasswordAsync(userId, model);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(ModelState);
            }

            return Ok(new { message = "Password has been changed successfully." });
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] EmailConfirmationDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.ConfirmEmailAsync(model);

            if (!result)
            {
                return BadRequest(new { message = "Failed to confirm email." });
            }

            return Ok(new { message = "Email confirmed successfully. You can now log in." });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var authResponse = await _authService.RefreshTokenAsync(model);

            if (authResponse == null)
            {
                return BadRequest(new { message = "Invalid token." });
            }

            // Return only the user object
            return Ok(authResponse.User);
        }

        [HttpGet("user-info")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            // Get user roles
            var roles = await _authService.GetUserRolesAsync(userId);

            // Instead of just mapping the user, generate a full auth response which will include tokens
            var authResponse = await _authService.GenerateTokenAsync(user);

            // The user object in authResponse should now have all the required fields including tokens
            return Ok(authResponse.User);
        }

        [HttpGet("check-role")]
        [Authorize]
        public async Task<IActionResult> CheckUserRole(string role)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var roles = await _authService.GetUserRolesAsync(userId);
            var hasRole = roles.Contains(role);

            return Ok(new { hasRole });
        }
    }
}