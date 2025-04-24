using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using UserAccountAPI.DTOs;
using UserAccountAPI.Models;
using UserAccountAPI.Services.Interfaces;
using UserAccountAPI.Common;

namespace UserAccountAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            ITokenService tokenService,
            IEmailService emailService,
            IConfiguration configuration,
            IMapper mapper)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task<(IdentityResult Result, ApplicationUser User)> RegisterUserAsync(RegisterDTO model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                NationalId = model.NationalId,
                DateOfBirth = model.DateOfBirth
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Check if role exists first, create it if it doesn't
                if (!await _roleManager.RoleExistsAsync(UserRoles.Patient))
                {
                    await _roleManager.CreateAsync(new ApplicationRole { Name = UserRoles.Patient });
                }

                // Now assign the role
                await _userManager.AddToRoleAsync(user, UserRoles.Patient);

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
            }

            return (result, user);
        }

        public async Task<(SignInResult Result, ApplicationUser User, IList<string> Roles)> LoginUserAsync(LoginDTO model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return (SignInResult.Failed, null, null);
            }

            if (!await _userManager.IsEmailConfirmedAsync(user) &&
                _configuration.GetValue<bool>("RequireConfirmedEmail"))
            {
                return (SignInResult.Failed, user, null);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                user.LastLogin = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Get user roles
                var roles = await _userManager.GetRolesAsync(user);

                return (result, user, roles);
            }

            return (result, user, null);
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                return true;
            }
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = HttpUtility.UrlEncode(token);
            var clientUrl = _configuration["ClientUrl"];
            var resetLink = $"{clientUrl}/reset-password?email={HttpUtility.UrlEncode(email)}&token={encodedToken}";

            await _emailService.SendPasswordResetEmailAsync(email, resetLink, user.Id, token);

            return true;
        }

        public async Task<IdentityResult> ResetPasswordAsync(ResetPasswordDTO model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Password reset failed" });
            }

            return await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        }

        public async Task<IdentityResult> ChangePasswordAsync(string userId, ChangePasswordDTO model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });
            }

            return await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        }

        public async Task<bool> ConfirmEmailAsync(EmailConfirmationDTO model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.ConfirmEmailAsync(user, model.Token);
            return result.Succeeded;
        }

        public async Task<AuthResponseDTO> GenerateTokenAsync(ApplicationUser user)
        {
            // Get user roles to include in token
            var roles = await _userManager.GetRolesAsync(user);
            return await _tokenService.GenerateTokensAsync(user, roles);
        }

        public async Task<AuthResponseDTO> RefreshTokenAsync(RefreshTokenDTO model)
        {
            return await _tokenService.RefreshTokenAsync(model.AccessToken, model.RefreshToken);
        }

        public async Task<bool> IsEmailConfirmedAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            return await _userManager.IsEmailConfirmedAsync(user);
        }

        public async Task<ApplicationUser> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<ApplicationUser> GetUserByIdAsync(string id)
        {
            return await _userManager.FindByIdAsync(id);
        }

        public async Task<IList<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new List<string>();
            }

            return await _userManager.GetRolesAsync(user);
        }

        public async Task<bool> AddUserToRoleAsync(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Check if role exists
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new ApplicationRole { Name = role });
            }

            var result = await _userManager.AddToRoleAsync(user, role);
            return result.Succeeded;
        }

        public async Task<bool> RemoveUserFromRoleAsync(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.RemoveFromRoleAsync(user, role);
            return result.Succeeded;
        }
    }
}