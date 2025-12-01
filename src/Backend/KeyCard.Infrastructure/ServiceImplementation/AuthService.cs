// ============================================================================
// AUTH SERVICE - USER MANAGEMENT AND REGISTRATION
// handles creating new users for both guests and staff
// works with ASP.NET Identity under the hood
// ============================================================================

using KeyCard.BusinessLogic.Commands.Auth;
using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Auth;
using KeyCard.Infrastructure.Models.User;

using Microsoft.AspNetCore.Identity;

namespace KeyCard.Infrastructure.ServiceImplementation
{
    /// <summary>
    /// Authentication service - creates and manages user accounts
    /// Uses ASP.NET Identity for password hashing, validation, etc.
    /// We never store plain text passwords - Identity handles that!
    /// </summary>
    public class AuthService : IAuthService
    {
        // userManager = ASP.NET Identity helper for user operations
        private readonly UserManager<ApplicationUser> _userManager;
        // roleManager = helper for managing roles (Admin, Guest, etc.)
        private readonly RoleManager<ApplicationUserRole> _roleManager;

        public AuthService(UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationUserRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        /// <summary>
        /// Admin creates any type of user with any role
        /// Only admins should call this - they can create other admins, staff, etc.
        /// </summary>
        public async Task<AuthResultViewModel> AdminCreateUserAsync(AdminCreateUserCommand command, CancellationToken cancellationToken)
        {
            // check if username already taken
            var existingByUsername = await _userManager.FindByNameAsync(command.Username);
            if (existingByUsername != null)
                throw new InvalidOperationException("A user with this username already exists.");

            // check if email already registered
            var existingByEmail = await _userManager.FindByEmailAsync(command.Email);
            if (existingByEmail != null)
                throw new InvalidOperationException("A user with this email already exists.");

            // create role if it doesnt exist yet
            // this is handy if admin creates user with new role type
            if (!await _roleManager.RoleExistsAsync(command.Role))
                await _roleManager.CreateAsync(new ApplicationUserRole(command.Role));

            // create the user object
            var user = new ApplicationUser
            {
                UserName = command.Username,
                Email = command.Email,
                FirstName = command.FirstName,
                LastName = command.LastName,
                FullName = (command.LastName + " " + command.FirstName).Trim(),  // "Doe John"
                Address = command.Address,
                Country = command.Country,
                EmailConfirmed = true  // skip email verification for admin-created accounts
            };

            // Identity creates user and hashes password securely
            var result = await _userManager.CreateAsync(user, command.Password);
            if (!result.Succeeded)
                // password too weak? invalid email format? return all errors
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

            // assign the role to the user
            await _userManager.AddToRoleAsync(user, command.Role);

            return new AuthResultViewModel(
                user.Id,
                user.FullName,
                user.Email!,
                command.Role
            );
        }

        /// <summary>
        /// Guest self-registration - anyone can create guest account
        /// Used by web booking portal - guest signs up before making reservation
        /// </summary>
        public async Task<AuthResultViewModel> GuestSignupAsync(GuestSignupCommand command, CancellationToken cancellationToken)
        {
            // email must be unique - its also used as username for guests
            var existingUser = await _userManager.FindByEmailAsync(command.Email);
            if (existingUser != null)
                throw new InvalidOperationException("A user with this email already exists.");

            // for guests, email = username (simpler to remember)
            var user = new ApplicationUser
            {
                UserName = command.Email,  // guests login with email
                Email = command.Email,
                FirstName = command.FirstName,
                LastName = command.LastName,
                FullName = (command.LastName + " " + command.FirstName).Trim(),
                EmailConfirmed = true  // TODO: in production, maybe require email verification
            };

            // create user with hashed password
            var result = await _userManager.CreateAsync(user, command.Password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

            // make sure Guest role exists
            if (!await _roleManager.RoleExistsAsync("Guest"))
                await _roleManager.CreateAsync(new ApplicationUserRole("Guest"));

            // all self-registered users get Guest role only
            // they cant give themselves Admin role through signup!
            await _userManager.AddToRoleAsync(user, "Guest");

            return new AuthResultViewModel(
                user.Id,
                user.FullName,
                user.Email!,
                "Guest"
            );
        }
    }
}
