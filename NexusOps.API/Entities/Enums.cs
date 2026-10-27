namespace NexusOps.API.Entities
{
    public enum ServiceStatus
    {
        Up = 1,
        Down = 2,
        Degraded = 3
    }

    public enum IncidentStatus
    {
        Open = 1,
        Investigating = 2,
        Resolved = 3
    }
}