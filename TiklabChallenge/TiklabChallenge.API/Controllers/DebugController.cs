using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TiklabChallenge.UseCases.Services;

namespace TiklabChallenge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly AppSeeder _seeder;
        public DebugController(AppSeeder seeder)
        {
            _seeder = seeder;
        }
        [HttpPost("reset")]
        public async Task<IActionResult> ResetDatabase(CancellationToken ct)
        {
            try
            {
                await _seeder.ResetAsync(ct);
                return Ok("Database reset successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error resetting database: {ex.Message}");
            }
        }
        [HttpPost("seed")]
        public async Task<IActionResult> SeedDatabase(CancellationToken ct)
        {
            try
            {
                await _seeder.SeedAllAsync(ct);
                return Ok("Database seeded successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error seeding database: {ex.Message}");
            }
        }

    }
}
