using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusOps.API.Data;
using NexusOps.API.DTOs;
using NexusOps.API.Entities;

namespace NexusOps.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonitoredServicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MonitoredServicesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/monitoredservices (Saari services + unke last 10 health logs)
        [HttpGet]
        public async Task<IActionResult> GetAllServices()
        {
            var services = await _context.MonitoredServices
                .AsNoTracking()
                .Select(s => new
                {
                    s.Id,
                    s.ServiceName,
                    s.EndpointUrl,
                    Status = s.CurrentStatus.ToString(),
                    s.LastCheckedAt,
                    RecentLogs = s.HealthLogs
                        .OrderByDescending(h => h.CheckedAt)
                        .Take(10)
                        .Select(h => new { h.StatusCode, h.ResponseTimeMs, h.IsHealthy, h.CheckedAt })
                })
                .ToListAsync();

            return Ok(services);
        }

        // POST: api/monitoredservices (Nayi URL monitor karne ke liye add karo)
        [HttpPost]
        public async Task<IActionResult> AddService([FromBody] CreateServiceDto dto)
        {
            var newService = new MonitoredService
            {
                ServiceName = dto.ServiceName,
                EndpointUrl = dto.EndpointUrl,
                CheckIntervalSeconds = dto.CheckIntervalSeconds
            };

            _context.MonitoredServices.Add(newService);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAllServices), new { id = newService.Id }, newService);
        }

        // DELETE: api/monitoredservices/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteService(Guid id)
        {
            var service = await _context.MonitoredServices.FindAsync(id);
            if (service == null) return NotFound();

            _context.MonitoredServices.Remove(service);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}