# Keycard.NET
Cross-platform hotel management system built in C#/.NET. Includes a staff desktop app, guest web portal, and real-time backend for bookings, housekeeping, and digital keys.

A C#/.NET 8–based hotel management platform that combines:
- **Staff desktop app** (front desk + housekeeping) built with **Avalonia** (Windows/macOS/Linux).
- **Guest web/kiosk** built with **Blazor Server**.
- **ASP.NET Core** backend (REST + **SignalR**), **EF Core** with **PostgreSQL**, **Redis** for cache/backplane, and **Hangfire** for background jobs.
- Fully **containerized** (Docker/Compose) for on-prem (Proxmox VM) or local laptops.

> **Windows required** (per course), but desktop + server also run on macOS/Linux.

---

## Features (Initial Scope)
- Bookings & availability search
- Check-in / Check-out workflows
- Room lifecycle & housekeeping Kanban (Vacant → Occupied → Dirty → Cleaning → Inspected → Vacant)
- Real-time updates (SignalR)
- Guest self-check-in & **digital key** (QR/JWT)
- PDF invoices (QuestPDF)

**Stretch:** demand pricing, multi-property, photo uploads, mock OTA feed.

---

## Tech Stack
- **Language/Runtime:** C# / .NET 8
- **Desktop:** Avalonia (MVVM)
- **Web:** Blazor Server (Razor Components)
- **Server:** ASP.NET Core + EF Core + Identity + SignalR + Hangfire
- **DB:** PostgreSQL (or SQL Server alt)
- **Infra:** Redis, Nginx (TLS), Docker/Compose
- **Testing:** xUnit, FluentAssertions
- **Reports:** QuestPDF

---

## Repository Layout
```
keycard.net/
├─ docs/
│  └─ architecture.md
├─ src/
│  ├─ Backend/                          ← Dhruv (Backend Lead)
│  │  ├─ KeyCard.Api/                   # ASP.NET Core API + SignalR + Auth
│  │  ├─ KeyCard.Application/           # CQRS/validators/domain services
│  │  ├─ KeyCard.Domain/                # Entities, VOs, domain events
│  │  ├─ KeyCard.Infrastructure/        # EF Core, providers, migrations
│  │  └─ KeyCard.Contracts/             # DTOs + Swagger/OpenAPI config
│  │
│  ├─ Desktop/                          ← Nima (Desktop Lead)
│  │  └─ KeyCard.Desktop/               # WPF/WinUI + MVVM + client SDK
│  │
│  └─ Web/                              ← Aastha (Web/Kiosk Lead)
│     └─ keycard-web/                   # Blazor (Server recommended)
│
├─ tests/
│  ├─ Backend.UnitTests/
│  ├─ Backend.IntegrationTests/
│  ├─ Desktop.UITests/
│  └─ Web.E2E/
├─ build/                               # Dockerfiles (api, web)
├─ ops/                                 # compose, nginx, scripts
└─ keycard.net.sln
```

## Quick Start
#### 1) Clone & configure
```git clone <your-repo-url> && cd hotel-suite```  
```cp .env.example .env``` edit if needed

#### 2) Bring up dev stack
cd ops
docker compose -f compose.dev.yml up -d --build

#### 3) Open apps
* API:   http://localhost:8080
* Web:   http://localhost:8081
* Login with seeded admin from .env


#### 4) Desktop App On Windows
How to run KeyCard Desktop

Open PowerShell and set environment variables:
"Mock mode (test data): Use the desktop app with mock data and no backend connection."
$env:DOTNET_ENVIRONMENT='Development'
$env:KeyCard__UseMocks='true'
dotnet run --project src/Desktop/KeyCard.Desktop/KeyCard.Desktop.csproj

"Live mode (real data): Connect the desktop app to the backend API."
Use the "KeyCard Live" launch profile, or run:
$env:DOTNET_ENVIRONMENT='Production'
$env:KeyCard__UseMocks='false'
dotnet run --project src/Desktop/KeyCard.Desktop/KeyCard.Desktop.csproj

