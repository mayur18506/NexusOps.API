using Microsoft.EntityFrameworkCore;
using NexusOps.API.Data;
using NexusOps.API.Hubs;
using NexusOps.API.Workers;

var builder = WebApplication.CreateBuilder(args);

// 1. Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. HttpClient & Background Worker
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<NexusOps.API.Services.GeminiAnalyzerService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15); // 100s wait karne ki jagah 15s max rakho
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    // Force IPv4 connection so Google API responds in 1-2 seconds instead of hanging!
    ConnectCallback = async (context, cancellationToken) =>
    {
        var entry = await System.Net.Dns.GetHostEntryAsync(context.DnsEndPoint.Host, System.Net.Sockets.AddressFamily.InterNetwork, cancellationToken);
        var socket = new System.Net.Sockets.Socket(System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
        await socket.ConnectAsync(entry.AddressList, context.DnsEndPoint.Port, cancellationToken);
        return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
    }
});
builder.Services.AddHostedService<HealthCheckWorker>();

// 3. SignalR
builder.Services.AddSignalR();

// 4. CORS (React Frontend ke liye - Port 5173 Vite ka default hota hai)
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactAppPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // SignalR ke liye AllowCredentials zaroori hai!
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS middleware use karo
app.UseCors("ReactAppPolicy");

app.UseAuthorization();

app.MapControllers();

// SignalR Hub Endpoint map karo
app.MapHub<AlertHub>("/hubs/alerts");

app.Run();