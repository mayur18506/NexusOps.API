using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NexusOps.API.Data;
using NexusOps.API.DTOs;
using NexusOps.API.Entities;
using NexusOps.API.Hubs;

namespace NexusOps.API.Workers
{
    public class HealthCheckWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<AlertHub> _hubContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HealthCheckWorker> _logger;

        public HealthCheckWorker(
            IServiceScopeFactory scopeFactory,
            IHubContext<AlertHub> hubContext,
            IHttpClientFactory httpClientFactory,
            ILogger<HealthCheckWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NexusOps Background Health Checker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckAllServicesAsync(stoppingToken);

                // Har 30 seconds mein cycle repeat hogi
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task CheckAllServicesAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10); // 10 sec max timeout
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NexusOps-Monitor/1.0");

            var activeServices = await dbContext.MonitoredServices
                .Where(s => s.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var service in activeServices)
            {
                var stopwatch = Stopwatch.StartNew();
                int statusCode = 0;
                bool isHealthy = false;
                string? errorMessage = null;

                try
                {
                    var response = await httpClient.GetAsync(service.EndpointUrl, cancellationToken);
                    stopwatch.Stop();

                    statusCode = (int)response.StatusCode;
                    isHealthy = response.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    statusCode = 503; // Service Unavailable
                    isHealthy = false;
                    errorMessage = ex.Message;
                }

                int responseTimeMs = (int)stopwatch.ElapsedMilliseconds;

                // 1. Determine Status (Agar 1500ms se zyada laga to Degraded)
                var newStatus = !isHealthy
                    ? ServiceStatus.Down
                    : (responseTimeMs > 1500 ? ServiceStatus.Degraded : ServiceStatus.Up);

                // 2. Agar service abhi-abhi Down hui hai, to Incident create karo
                if (newStatus == ServiceStatus.Down && service.CurrentStatus != ServiceStatus.Down)
                {
                    var aiService = scope.ServiceProvider.GetRequiredService<NexusOps.API.Services.GeminiAnalyzerService>();
                    var (rootCause, fix) = await aiService.AnalyzeIncidentAsync(
                        service.ServiceName,
                        service.EndpointUrl,
                        statusCode,
                        errorMessage ?? $"Endpoint returned HTTP {statusCode}"
                    );

                    dbContext.Incidents.Add(new Incident
                    {
                        ServiceId = service.Id,
                        ErrorMessage = errorMessage ?? $"Endpoint returned HTTP {statusCode}",
                        AiRootCause = rootCause,
                        AiSuggestedFix = fix,
                        Status = IncidentStatus.Open
                    });
                }
                // PHASE 3: Self-Healing Auto-Resolve (Agar service Up/Degraded hai, to open incidents ko Resolved kar do)
                else if (newStatus != ServiceStatus.Down)
                {
                    var openIncidents = await dbContext.Incidents
                        .Where(i => i.ServiceId == service.Id && i.Status != IncidentStatus.Resolved)
                        .ToListAsync(cancellationToken);

                    foreach (var incident in openIncidents)
                    {
                        incident.Status = IncidentStatus.Resolved;
                        incident.ResolvedAt = DateTime.UtcNow;
                        _logger.LogInformation("Auto-resolved incident {IncidentId} as {ServiceName} is healthy ({Status}).", incident.Id, service.ServiceName, newStatus);
                    }
                }

                // 3. Update Service & Save HealthLog
                service.CurrentStatus = newStatus;
                service.LastCheckedAt = DateTime.UtcNow;

                dbContext.HealthLogs.Add(new HealthLog
                {
                    ServiceId = service.Id,
                    StatusCode = statusCode,
                    ResponseTimeMs = responseTimeMs,
                    IsHealthy = isHealthy,
                    CheckedAt = DateTime.UtcNow
                });

                await dbContext.SaveChangesAsync(cancellationToken);

                // 4. SignalR ke through Live Broadcast karo!
                var notification = new HealthUpdateNotification(
                    service.Id,
                    service.ServiceName,
                    newStatus.ToString(),
                    statusCode,
                    responseTimeMs,
                    service.LastCheckedAt.Value
                );

                await _hubContext.Clients.All.SendAsync("ReceiveHealthUpdate", notification, cancellationToken);
                _logger.LogInformation("Checked {Service}: {Status} ({Time}ms)", service.ServiceName, newStatus, responseTimeMs);
            }
        }
    }
}