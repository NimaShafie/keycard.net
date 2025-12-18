# KeyCard.NET

<p align="center">

  <!-- .NET Version -->
  <img alt=".NET 8" src="https://img.shields.io/badge/.NET-8.0-blueviolet?style=for-the-badge&logo=dotnet" />

  <!-- CI Status -->
  <a href="https://github.com/NimaShafie/keycard.net/actions/workflows/ci.yml">
    <img alt="CI" src="https://img.shields.io/github/actions/workflow/status/NimaShafie/keycard.net/ci.yml?style=for-the-badge&logo=github&label=CI" />
  </a>

  <!-- License -->
  <img alt="License" src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" />

  <!-- Project Type -->
  <img alt="Avalonia" src="https://img.shields.io/badge/Desktop-Avalonia-0064A5?style=for-the-badge&logo=avalonia" />
  <img alt="Blazor Server" src="https://img.shields.io/badge/Web-Blazor_Server-512BD4?style=for-the-badge&logo=blazor" />
  <img alt="SQLite" src="https://img.shields.io/badge/Database-SQLite-044F88?style=for-the-badge&logo=sqlite" />

</p>

Cross-platform hotel management system built in C#/.NET.\
Includes a staff desktop app, guest web portal, and real-time backend
for bookings, housekeeping, and digital keys.

A .NET 8--based hotel platform with:

-   **Staff desktop app** (front desk + housekeeping) built with
    Avalonia (Windows/macOS/Linux).
-   **Guest web/kiosk** built with Blazor Server.
-   **Backend API** using ASP.NET Core, EF Core, and
    Identity, with SignalR for live updates.

The desktop and backend also run on Windows/macOS/Linux.

------------------------------------------------------------------------

## Current Status / What's Changed

Compared to the original project plan:

-   The development database now uses SQLite (file-based) instead of
    LocalDB.
-   The desktop app supports Mock and Live modes via
    configuration (no separate exe).
-   The repo is organized as a .NET monorepo with three solution
    files.
-   Docker, Nginx, and compose files are no longer part of the standard
    setup.
-   GitHub workflows are minimal/optional and not required to run or
    develop the app locally.

------------------------------------------------------------------------

## Prerequisites

-   .NET 8 SDK
-   Windows (primary target)
-   (Optional) macOS/Linux for backend & desktop development

The backend uses a SQLite database file under:

    src/Backend/KeyCard.Api/App_Data/keycard_dev.db

If the schema becomes out of sync, delete the DB file and let EF
recreate it, or reapply migrations.

------------------------------------------------------------------------

## Database Migrations (Backend)

Apply migrations:

    dotnet ef database update --project src/Backend/KeyCard.Infrastructure --startup-project src/Backend/KeyCard.Api

Reset DB:

1.  Stop backend\
2.  Delete: `src/Backend/KeyCard.Api/App_Data/keycard_dev.db`\
3.  Re-run migrations or run API once to recreate

------------------------------------------------------------------------

## How to Run

### Visual Studio

Open the root solution or area-specific solution:

-   `KeyCard.Backend.sln`
-   `KeyCard.Desktop.sln`
-   `KeyCard.Web.sln`

Select the desired run profile:

-   Backend API
-   Web Portal
-   Desktop
-   Backend + Web
-   Backend + Desktop
-   Full stack

Ports are shown in the startup logs.

------------------------------------------------------------------------

### Visual Studio Code

Use built-in `.vscode/launch.json`:

1.  Open repo in VS Code\
2.  Go to **Run & Debug**\
3.  Choose configuration (Backend, Web, Desktop, or combinations)

------------------------------------------------------------------------

### Command Line

    dotnet run --project src/Backend/KeyCard.Api/KeyCard.Api.csproj
    dotnet run --project src/Web/KeyCard.Web/KeyCard.Web.csproj
    dotnet run --project src/Desktop/KeyCard.Desktop/KeyCard.Desktop.csproj

Backend should run first when using Web or Desktop.

------------------------------------------------------------------------

## Desktop App Modes (Mock vs Live)

The desktop app supports modes via config:

### Mock Mode

    KeyCard:Mode = "Mock"

### Live Mode

    KeyCard:Mode = "Live"
    KeyCard:ApiBaseUrl = "https://localhost:7224"

Example:

    $env:KeyCard__Mode = "Live"
    $env:KeyCard__ApiBaseUrl = "https://localhost:7224"
    dotnet run --project src/Desktop/KeyCard.Desktop

------------------------------------------------------------------------

## Features (Current Scope)

-   Guest bookings & availability search
-   Check-in / Check-out workflows
-   Room lifecycle tracking (Vacant → Occupied → Dirty → Cleaning →
    Inspected → Vacant)
-   Real-time updates (SignalR)
-   Guest self-check-in + digital key (QR)
-   PDF invoices

------------------------------------------------------------------------

## Tech Stack

-   **Language:** C# / .NET 8
-   **Desktop:** Avalonia (MVVM)
-   **Web:** Blazor Server
-   **Backend:** ASP.NET Core, EF Core, Identity, SignalR
-   **Database:** SQLite
-   **Testing:** xUnit, FluentAssertions

------------------------------------------------------------------------

## Repository Layout

    keycard.net/
    ├─ docs/
    │  └─ architecture.md
    ├─ src/
    │  ├─ Backend/
    │  │  ├─ KeyCard.Api/
    │  │  ├─ KeyCard.BusinessLogic/
    │  │  ├─ KeyCard.Core/
    │  │  ├─ KeyCard.Domain/
    │  │  └─ KeyCard.Infrastructure/
    │  ├─ Desktop/
    │  │  └─ KeyCard.Desktop/
    │  └─ Web/
    │     └─ KeyCard.Web/
    ├─ tests/
    ├─ run/
    └─ keycard.net.sln

------------------------------------------------------------------------

## Troubleshooting

### "Error loading bookings"

Likely SQLite schema mismatch.\
Fix:

1.  Delete `keycard_dev.db`\
2.  Run migrations or restart backend

### Port conflicts

Update ports in `launchSettings.json`.

### Desktop Mock Mode not switching

Verify `KeyCard:Mode` environment variable or appsettings entry.

------------------------------------------------------------------------