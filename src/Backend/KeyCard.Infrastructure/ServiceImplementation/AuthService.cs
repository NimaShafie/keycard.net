//using KeyCard.BusinessLogic.Commands.Auth;
//using KeyCard.BusinessLogic.ServiceInterfaces;
//using KeyCard.BusinessLogic.ViewModels.Auth;
//using KeyCard.Infrastructure.Models.Users;

//using Microsoft.AspNetCore.Identity;

//namespace KeyCard.Infrastructure.ServiceImplementation
//{
//    public class AuthService : IAuthService
//    {
//        private readonly UserManager<ApplicationUser> _userManager;
//        private readonly RoleManager<ApplicationRole> _roleManager;
//        public AuthService(UserManager<ApplicationUser> userManager,
//            RoleManager<ApplicationRole> roleManager) {
//            _userManager = userManager;
//            _roleManager = roleManager;
//        }

//        public async Task<AuthResultViewModel> GuestSignupAsync(GuestSignupCommand command, CancellationToken cancellationToken)
//        {
//            var existingUser = await _userManager.FindByEmailAsync(command.Email);
//            if (existingUser != null)
//                throw new InvalidOperationException("A user with this email already exists.");

//            var user = new ApplicationUser
//            {
//                UserName = command.Email,
//                Email = command.Email,
//                FullName = command.FullName,
//                EmailConfirmed = true // can require email verification later
//            };

//            var result = await _userManager.CreateAsync(user, command.Password);
//            if (!result.Succeeded)
//                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

//            // Ensure Guest role exists
//            if (!await _roleManager.RoleExistsAsync("Guest"))
//                await _roleManager.CreateAsync(new ApplicationRole("Guest"));

//            await _userManager.AddToRoleAsync(user, "Guest");

//            return new AuthResultViewModel(
//                user.Id,
//                user.FullName,
//                user.Email!,
//                "Guest"
//            );
//        }
//    }
//}
