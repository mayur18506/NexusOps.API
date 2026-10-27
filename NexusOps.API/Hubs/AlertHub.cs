using Microsoft.AspNetCore.SignalR;

namespace NexusOps.API.Hubs
{
    public class AlertHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("Connected", $"Connected to NexusOps Live Telemetry: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }
    }
}