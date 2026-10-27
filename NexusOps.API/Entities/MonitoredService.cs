using System.ComponentModel.DataAnnotations;

namespace NexusOps.API.Entities
{
    public class MonitoredService
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string ServiceName { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string EndpointUrl { get; set; } = string.Empty;

        public int CheckIntervalSeconds { get; set; } = 60; // Default 1 min

        public bool IsActive { get; set; } = true;

        public ServiceStatus CurrentStatus { get; set; } = ServiceStatus.Up;

        public DateTime? LastCheckedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<HealthLog> HealthLogs { get; set; } = new List<HealthLog>();
        public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
    }
}