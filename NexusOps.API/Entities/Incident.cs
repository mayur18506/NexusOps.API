using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusOps.API.Entities
{
    public class Incident
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ServiceId { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public MonitoredService? MonitoredService { get; set; }

        [Required, MaxLength(500)]
        public string ErrorMessage { get; set; } = string.Empty;

        public string? StackTrace { get; set; }

        // Gemini AI Columns
        public string? AiRootCause { get; set; }
        public string? AiSuggestedFix { get; set; }

        public IncidentStatus Status { get; set; } = IncidentStatus.Open;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}