using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusOps.API.Entities
{
    public class HealthLog
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public Guid ServiceId { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public MonitoredService? MonitoredService { get; set; }

        public int StatusCode { get; set; } // e.g., 200, 500, 404
        public int ResponseTimeMs { get; set; } // e.g., 145 ms
        public bool IsHealthy { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }
}