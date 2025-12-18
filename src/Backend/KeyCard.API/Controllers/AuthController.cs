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
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMediator _mediator;
        private readonly IConfiguration _config;

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
        /// Staff/Admin login - accepts username or email
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand request)
        {
            // Try to find user by username first, then by email
            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(request.Username);
            }
            
            if (user != null && await _userManager.CheckPasswordAsync(user, request.Password))
            {
                var roles = await _userManager.GetRolesAsync(user);
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                    new Claim("FullName", user.FullName ?? string.Empty),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                authClaims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
                var token = new JwtSecurityToken(
                    issuer: _config["Jwt:Issuer"],
                    audience: _config["Jwt:Audience"],
                    expires: DateTime.Now.AddMinutes(Convert.ToDouble(_config["Jwt:ExpireMinutes"] ?? "60")),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
                );

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo
                });
            }

            return Unauthorized();
        }

        /// <summary>
        /// Guest signup (no admin privileges required)
        /// </summary>
        [HttpPost("guest/signup")]
        [ProducesResponseType(typeof(AuthResultViewModel), 200)]
        public async Task<IActionResult> GuestSignup(
            [FromBody] GuestSignupCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Staff self-registration (no authentication required)
        /// Creates Employee accounts for staff console access
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
                var command = new AdminCreateUserCommand(
                    Username: dto.Username,
                    FirstName: dto.FirstName,
                    LastName: dto.LastName,
                    Email: dto.Email,
                    Password: dto.Password,
                    Role: "Employee",
                    Address: null,
                    Country: null
                );

                var result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred during registration." });
            }
        }

        /// <summary>
        /// Admin: Create any user (requires Admin role)
        /// </summary>
        [HttpPost("admin/CreateUser")]
        [Authorize(Roles = "Admin")]
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
    /// </summary>
    public record StaffRegisterDto(
        string Username,
        string FirstName,
        string LastName,
        string Email,
        string Password
    );

    /// <summary>
    /// Login command
    /// </summary>
    public record LoginCommand(
        string Username,
        string Password
    );
}
