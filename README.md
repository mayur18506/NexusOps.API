# ⚙️ NexusOps Backend API
> Enterprise-grade SRE & DevOps Monitoring RESTful API built with ASP.NET Core.

![ASP.NET Core](https://img.shields.io/badge/.NET-8.0-purple) ![Entity Framework Core](https://img.shields.io/badge/EF%20Core-SQL%20Server-blue) ![SignalR](https://img.shields.io/badge/SignalR-WebSockets-orange) ![Google Gemini AI](https://img.shields.io/badge/Gemini-AI-indigo)

## 🌟 Overview
The backend engine for **NexusOps Command Center**. It handles concurrent background health checks, real-time telemetry streaming over WebSockets, incident lifecycle management, and automated AI root-cause analysis.

## 🛠️ Tech Stack & Architecture
* **Framework:** ASP.NET Core Web API (C#)
* **Database & ORM:** SQL Server, Entity Framework Core (Code-First)
* **Real-Time Communication:** ASP.NET Core SignalR Hubs
* **Background Processing:** Hosted Services (`IHostedService` / `BackgroundService`) for 30s telemetry loops
* **AI Integration:** Google Gemini API (`gemini-3.8-flash`) with SRE fallback diagnostics

## 🚀 Key Features
* **Concurrent Health Pings:** Background worker pings registered microservices every 30 seconds, measuring response times via `Stopwatch` and capturing HTTP status codes.
* **SignalR WebSocket Broadcasting:** Instantly pushes real-time health telemetry updates to all connected UI clients.
* **AI-Driven SRE Diagnostics:** Automatically analyzes HTTP 5xx/4xx outages using Google Gemini AI to generate root-cause analysis and actionable fix steps.
* **Self-Healing Auto-Resolution:** Automatically transitions open incidents to `"Resolved"` when an unhealthy endpoint recovers.
