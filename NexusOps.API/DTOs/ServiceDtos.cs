using System.ComponentModel.DataAnnotations;

namespace NexusOps.API.DTOs
{
    public record CreateServiceDto(
        [Required] string ServiceName,
        [Required, Url] string EndpointUrl,
        int CheckIntervalSeconds = 60
    );

    public record HealthUpdateNotification(
        Guid ServiceId,
        string ServiceName,
        string Status,
        int StatusCode,
        int ResponseTimeMs,
        DateTime CheckedAt
    );
}