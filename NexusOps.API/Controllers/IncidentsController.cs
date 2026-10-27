using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusOps.API.Data;
using NexusOps.API.Entities;
using NexusOps.API.Services;

namespace NexusOps.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncidentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly GeminiAnalyzerService _aiService;

        public IncidentsController(AppDbContext context, GeminiAnalyzerService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        // GET: api/incidents (Saare incidents + service name)
        [HttpGet]
        public async Task<IActionResult> GetIncidents()
        {
            var incidents = await _context.Incidents
                .Include(i => i.MonitoredService)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new
                {
                    i.Id,
                    ServiceName = i.MonitoredService != null ? i.MonitoredService.ServiceName : "Unknown",
                    EndpointUrl = i.MonitoredService != null ? i.MonitoredService.EndpointUrl : "",
                    i.ErrorMessage,
                    i.AiRootCause,
                    i.AiSuggestedFix,
                    Status = i.Status.ToString(),
                    i.CreatedAt,
                    i.ResolvedAt
                })
                .ToListAsync();

            return Ok(incidents);
        }

        // POST: api/incidents/{id}/analyze (Kisi bhi incident ka AI Root-Cause nikalne ke liye)
        [HttpPost("{id}/analyze")]
        public async Task<IActionResult> AnalyzeIncidentWithAi(Guid id)
        {
            var incident = await _context.Incidents
                .Include(i => i.MonitoredService)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (incident == null) return NotFound("Incident not found.");

            var (rootCause, suggestedFix) = await _aiService.AnalyzeIncidentAsync(
     incident.MonitoredService?.ServiceName ?? "Service",
     incident.MonitoredService?.EndpointUrl ?? "",
     0, // Exact error message neeche pass ho hi raha hai
     incident.ErrorMessage
 );

            incident.AiRootCause = rootCause;
            incident.AiSuggestedFix = suggestedFix;
            incident.Status = IncidentStatus.Investigating;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                incident.Id,
                incident.ErrorMessage,
                incident.AiRootCause,
                incident.AiSuggestedFix,
                Status = incident.Status.ToString()
            });
        }

        // PUT: api/incidents/{id}/resolve (Incident ko Resolved mark karne ke liye)
        [HttpPut("{id}/resolve")]
        public async Task<IActionResult> ResolveIncident(Guid id)
        {
            var incident = await _context.Incidents.FindAsync(id);
            if (incident == null) return NotFound();

            incident.Status = IncidentStatus.Resolved;
            incident.ResolvedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Incident marked as resolved." });
        }
    }
}