using MediAid.Data;
using MediAid.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace MediAid.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly MongoDbContext _context;

    public HealthController(MongoDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return Ok(new
        {
            status = "Healthy",
            application = "MediAid",
            framework = ".NET 8",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("database")]
    public async Task<IActionResult> Database()
    {
        try
        {
            await _context.CanConnectAsync();

            var users = await _context.Users.CountDocumentsAsync(FilterDefinition<User>.Empty);
            var requests = await _context.Requests.CountDocumentsAsync(FilterDefinition<Request>.Empty);

            return Ok(new
            {
                status = "Connected",
                database = _context.DatabaseName,
                users,
                requests,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new
            {
                status = "Database unavailable",
                database = "MongoDB",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
