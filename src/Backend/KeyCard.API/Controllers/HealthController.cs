// ============================================================================
// HEALTH CONTROLLER - THE HEARTBEAT OF OUR API
// load balancers, monitoring tools, devops people love this endpoint
// "is your API alive?" - yes, I am! here is proof :)
// ============================================================================

using KeyCard.Infrastructure.Models.AppDbContext; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeyCard.Api.Controllers
{
    /// <summary>
    /// Health check endpoint - monitors if API and database are working
    /// Used by Docker, Kubernetes, Azure, AWS... basically any infrastructure
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [AllowAnonymous]  // no auth needed - monitoring tools must access without login
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public HealthController(ApplicationDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Simple endpoint to check if API is alive and database is reachable
        /// Returns 200 if healthy, 503 if database is down
        /// </summary>
        /// <returns>Health status with timestamp and db connection status</returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            // try to ping database - if this fails, something is very wrong
            var dbHealthy = await CheckDatabaseConnectionAsync();

            var result = new
            {
                service = "KeyCard.NET API",
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                database = dbHealthy ? "Connected" : "Unavailable"
            };

            // 503 = Service Unavailable - tells load balancer to not send traffic here
            return dbHealthy ? Ok(result) : StatusCode(503, result);
        }

        /// <summary>
        /// Quick database connection test
        /// We dont run complex queries, just check if we CAN connect
        /// </summary>
        private async Task<bool> CheckDatabaseConnectionAsync()
        {
            try
            {
                // EF Core built-in method - opens connection and closes it
                return await _context.Database.CanConnectAsync();
            }
            catch
            {
                // any exception means database is unreachable
                return false;
            }
        }
    }
}
