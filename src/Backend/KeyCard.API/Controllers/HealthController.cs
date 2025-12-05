using KeyCard.Infrastructure.Models.AppDbContext; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeyCard.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public HealthController(ApplicationDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Simple endpoint to check if the API is running and can connect to the database.
        /// </summary>
        /// <returns>Message whether System is up or not.</returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var dbHealthy = await CheckDatabaseConnectionAsync();

            var result = new
            {
                service = "KeyCard.NET API",
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                database = dbHealthy ? "Connected" : "Unavailable"
            };

            return dbHealthy ? Ok(result) : StatusCode(503, result);
        }

        private async Task<bool> CheckDatabaseConnectionAsync()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }
    }
}
