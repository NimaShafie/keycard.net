# Keycard.NET
Cross-platform hotel management system built in C#/.NET. Includes a staff desktop app, guest web portal, and real-time backend for bookings, housekeeping, and digital keys.

A C#/.NET 8–based hotel management platform that combines:
- **Staff desktop app** (front desk + housekeeping) built with **Avalonia** (Windows/macOS/Linux).
- **Guest web/kiosk** built with **Blazor Server**.
- **ASP.NET Core** backend (REST + **SignalR**), **EF Core** with **PostgreSQL**, **Redis** for cache/backplane, and **Hangfire** for background jobs.
- Fully **containerized** (Docker/Compose) for on-prem (Proxmox VM) or local laptops.

> **Windows required** (per course), but desktop + server also run on macOS/Linux.

---

## Prereqs
- **.NET 8 SDK**
- **Windows** (course target). macOS/Linux also work for API/Web/Desktop.
- **No SQL Server install required** – dev uses **LocalDB** via `dotnet user-secrets`.

If you haven’t already, set the LocalDB connection string once (Windows PowerShell):

```powershell
cd <repo-root>
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=KeyCardDb;Trusted_Connection=True;MultipleActiveResultSets=True;Encrypt=False" --project src/Backend/KeyCard.Api
```
Run EF migrations (first time only):
```powershell
dotnet ef database update --project src/Backend/KeyCard.Infrastructure --startup-project src/Backend/KeyCard.Api
```

---

## How to Run

### 🧩 Using Visual Studio
Visual Studio automatically detects root-level launch configurations from `launch.vs.json`.

Available **Startup Items** (green play dropdown):
1. **KeyCard Backend** – Runs only the backend API (http://localhost:8080)
2. **KeyCard Desktop** – Runs only the Avalonia desktop client
3. **KeyCard Web** – Runs only the Blazor Server web portal (http://localhost:8081)
4. **KeyCard Backend + Desktop** – Runs both API and Desktop
5. **KeyCard Backend + Web** – Runs both API and Web
6. **KeyCard Backend + Desktop + Web** – Runs all three together

> These use PowerShell scripts in `/run` (e.g., `run/api.ps1`, `run/api-desktop.ps1`, etc.).  
> No project `appsettings` or `launchSettings.json` are modified.

If scripts don’t launch the first time, enable PowerShell script execution:
```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

---

### 🧩 Using Visual Studio Code
VS Code uses `.vscode/launch.json` and `.vscode/tasks.json`. The repo already includes predefined debug configurations:

**Available launch options:**
- **KeyCard Backend** → http://localhost:8080 (Swagger at `/swagger`)
- **KeyCard Desktop** → Avalonia client connected to http://localhost:8080
- **KeyCard Web** → http://localhost:8081
- **KeyCard Backend + Desktop**
- **KeyCard Backend + Web**
- **KeyCard Backend + Desktop + Web**

> The first time, allow Windows Firewall access for ports 8080/8081.

**To start:**
1. Open the repo in VS Code.  
2. Go to **Run and Debug** (`Ctrl+Shift+D`).  
3. Pick the configuration (e.g., *KeyCard Backend + Desktop*) and click **▶ Run**.

---

### 🧩 Using any IDE or Command Line
You can run the same scripts manually from the `/run` directory:

```powershell
# 1. Run only the backend API
pwsh run/api.ps1

# 2. Run only the desktop client
pwsh run/desktop.ps1

# 3. Run only the web app
pwsh run/web.ps1

# 4. Run backend + desktop
pwsh run/api-desktop.ps1

# 5. Run backend + web
pwsh run/api-web.ps1

# 6. Run backend + desktop + web
pwsh run/api-desktop-web.ps1
```

All scripts automatically configure URLs and environment variables.

---

## Features (Initial Scope)
- Bookings & availability search
- Check-in / Check-out workflows
- Room lifecycle & housekeeping Kanban (Vacant → Occupied → Dirty → Cleaning → Inspected → Vacant)
- Real-time updates (SignalR)
- Guest self-check-in & **digital key** (QR/JWT)
- PDF invoices (QuestPDF)

**Stretch Goals:** demand pricing, multi-property, photo uploads, mock OTA feed.

---

## Tech Stack
- **Language/Runtime:** C# / .NET 8
- **Desktop:** Avalonia (MVVM)
- **Web:** Blazor Server (Razor Components)
- **Server:** ASP.NET Core + EF Core + Identity + SignalR + Hangfire
- **DB:** LocalDB (dev) or PostgreSQL (prod)
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
│  │  └─ KeyCard.Desktop/               # Avalonia + MVVM client
│  │
│  └─ Web/                              ← Aastha (Web/Kiosk Lead)
│     └─ KeyCard.Web/                   # Blazor Server
│
├─ tests/
│  ├─ Backend.UnitTests/
│  ├─ Backend.IntegrationTests/
│  ├─ Desktop.UITests/
│  └─ Web.E2E/
├─ build/                               # Dockerfiles (api, web)
├─ ops/                                 # compose, nginx, scripts
├─ run/                                 # PowerShell launch scripts
└─ keycard.net.sln
```

---

## Optional: Docker Dev Stack
```powershell
cd ops
docker compose -f compose.dev.yml up -d --build
```
- API → http://localhost:8080  
- Web → http://localhost:8081  
- Desktop connects automatically to the same base URL.

---

## Desktop App Modes
**Mock mode (test data):**
```powershell
$env:DOTNET_ENVIRONMENT='Development'
$env:KeyCard__UseMocks='true'
dotnet run --project src/Desktop/KeyCard.Desktop/KeyCard.Desktop.csproj
```

**Live mode (real backend):**
```powershell
$env:DOTNET_ENVIRONMENT='Production'
$env:KeyCard__UseMocks='false'
dotnet run --project src/Desktop/KeyCard.Desktop/KeyCard.Desktop.csproj
```
