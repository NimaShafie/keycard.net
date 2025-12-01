// ============================================================================
// AUTH CONTROLLER - THE GATEKEEPER OF OUR HOTEL SYSTEM
// handles all authentication stuff: login, signup, user creation
// if you cant get past this, you cant do nothing in the system!
// ============================================================================

using KeyCard.BusinessLogic.Commands.Auth;
using KeyCard.BusinessLogic.ViewModels.Auth;
using KeyCard.Infrastructure.Models.User;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KeyCard.Api.Controllers
{
    /// <summary>
    /// Authentication controller - login, signup, user management
    /// This is public endpoint mostly, no auth needed to login (obviously lol)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // userManager is ASP.NET Identity thing - handles passwords, users, all that jazz
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMediator _mediator;  // our CQRS friend for sending commands
        private readonly IConfiguration _config;  // app settings, JWT secrets etc

        public AuthController(
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            IMediator mediator)
        {
            _userManager = userManager;
            _config = config;
            _mediator = mediator;
        }

        /// <summary>
        /// Staff/Admin login - this is the main entry point for hotel staff
        /// Desktop app and admin panel use this to get their JWT tokens
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand request)
        {
            // first, find the user by username
            var user = await _userManager.FindByNameAsync(request.Username);
            
            // check if user exists AND password is correct
            if (user != null && await _userManager.CheckPasswordAsync(user, request.Password))
            {
                // get all roles assigned to this user (Admin, Employee, HouseKeeping, etc)
                var roles = await _userManager.GetRolesAsync(user);
                
                // build the claims - these go inside the JWT token
                // whoever has this token, we know who they are and what they can do
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),  // user ID
                    new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),  // username
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),    // email
                    new Claim("FullName", user.FullName ?? string.Empty),       // display name
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())  // unique token ID
                };

                // add all roles as claims - this is how we check "can this user access admin stuff?"
                authClaims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

                // create the JWT token with all our claims baked in
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
                var token = new JwtSecurityToken(
                    issuer: _config["Jwt:Issuer"],
                    audience: _config["Jwt:Audience"],
                    expires: DateTime.Now.AddMinutes(Convert.ToDouble(_config["Jwt:ExpireMinutes"] ?? "60")),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
                );

                // send back the token and when it expires
                // client stores this and sends it with every request
                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo
                });
            }

            // wrong username or password - no hints about which one is wrong (security!)
            return Unauthorized();
        }

        /// <summary>
        /// Guest signup - for hotel guests booking through web app
        /// No auth needed - anyone can create guest account to make reservation
        /// </summary>
        [HttpPost("guest/signup")]
        [ProducesResponseType(typeof(AuthResultViewModel), 200)]
        public async Task<IActionResult> GuestSignup(
            [FromBody] GuestSignupCommand command,
            CancellationToken cancellationToken)
        {
            // MediatR sends this to GuestSignupCommandHandler
            // which creates user with "Guest" role
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Staff self-registration - new employees can register themselves
        /// Creates Employee accounts for desktop app access
        /// In real production, you might want admin approval for this!
        /// </summary>
        [HttpPost("staff/register")]
        [ProducesResponseType(typeof(AuthResultViewModel), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> StaffRegister(
            [FromBody] StaffRegisterDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                // reuse the admin create user command but force role to Employee
                // staff cant give themselves Admin role through this endpoint haha
                var command = new AdminCreateUserCommand(
                    Username: dto.Username,
                    FirstName: dto.FirstName,
                    LastName: dto.LastName,
                    Email: dto.Email,
                    Password: dto.Password,
                    Role: "Employee",  // hardcoded! no way to escalate privileges here
                    Address: null,
                    Country: null
                );

                var result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                // probably duplicate username or email
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                // something went really wrong - dont expose details to client
                return StatusCode(500, new { message = "An error occurred during registration." });
            }
        }

        /// <summary>
        /// Admin-only: Create any type of user with any role
        /// Only admins can create other admins or assign special roles
        /// </summary>
        [HttpPost("admin/CreateUser")]
        [Authorize(Roles = "Admin")]  // must be admin to use this!
        [ProducesResponseType(typeof(AuthResultViewModel), 200)]
        public async Task<IActionResult> CreateUser(
            [FromBody] AdminCreateUserCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
    }

    /// <summary>
    /// DTO for staff self-registration
    /// just the basics - name, email, password
    /// </summary>
    public record StaffRegisterDto(
        string Username,
        string FirstName,
        string LastName,
        string Email,
        string Password
    );

    /// <summary>
    /// Login command - username and password, thats all we need
    /// </summary>
    public record LoginCommand(
        string Username,
        string Password
    );
}
