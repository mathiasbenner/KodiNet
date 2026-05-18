# KodiNet

Web application for centralized management of Raspberry Pi running LibreELEC/Kodi.

Monitor player status, control playback, transfer video files from a private storage server to Pi devices, and schedule automated tasks.

The application was originally designed for video file management (*/storage/videos* folder on the Raspberry Pi).
As a result, other file formats have not been tested.

Pour le français, voir [README.fr.md](README.fr.md)

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Architecture](#architecture)
3. [Quick Install (Docker)](#quick-install-docker)
4. [Configuration](#configuration)
5. [Enabling JSON-RPC on Kodi](#enabling-json-rpc-on-kodi)
6. [Private Storage Server](#private-storage-server)
7. [Microsoft Authentication](#microsoft-authentication)
8. [Application Roles](#application-roles)
9. [Features](#features)
10. [Local Development](#local-development)
11. [Database Migrations](#database-migrations)
12. [Security](#security)
13. [License](#license)

---

## Prerequisites

| Component | Minimum version |
|---|---|
| Raspberry Pi | 4B and upper |
| LibreELEC | 10.0 |
| Kodi | 19 (Matrix) |
| MySQL | 8.0 |
| Docker + Docker Compose | 24.0 |
| Microsoft account | Azure AD / Entra ID (organizational) |

The system requirements simply reflect the environment for which the application was developed; it may well be possible to use an earlier version of the Raspberry Pi, etc.

---

## Architecture

```
┌─────────────────────────────────────────┐
│  Internet                               │
│          │  HTTPS (OIDC Microsoft)      │
│    ┌─────▼──────────┐                   │
│    │  KodiNet Web   │ Blazor Server     │
│    │  (container)   │ .NET 8            │
│    └──┬──────────┬──┘                   │
│  Internal│network │                     │
│    ┌───▼───┐  ┌───▼──────────┐          │
│    │ MySQL │  │   Private    │          │
│    │  8.4  │  │   Storage    │          │
│    └───────┘  │  (FastAPI)   │          │
│               └──────────────┘          │
│                                         │
│  Local Pi network                       │
│    ┌────────┐ ┌────────┐ ┌────────┐     │
│    │  Pi 1  │ │  Pi 2  │ │  Pi N  │     │
│    │  Kodi  │ │  Kodi  │ │  Kodi  │     │
│    └────────┘ └────────┘ └────────┘     │
└─────────────────────────────────────────┘
```

**Application layers:**

```
KodiNet.Domain          Entities, enumerations, pure interfaces
KodiNet.Application     DTOs, service interfaces, business logic
KodiNet.Infrastructure  EF Core/MySQL, Kodi/SFTP/HTTP clients, encryption
KodiNet.Web             Blazor Server, MudBlazor components, pages
```

---

## Quick Install (Docker)

### 1. Clone the repository

```bash
git clone https://github.com/your-account/kodinet.git
cd kodinet
```

### 2. Create the environment file

```bash
cp .env.example .env
```

Edit `.env` with your values:

```env
# Azure AD
AZUREAD_TENANTID=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
AZUREAD_CLIENTID=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
AZUREAD_CLIENTSECRET=your_azure_secret

# Database
MYSQL_ROOT_PASSWORD=strong_root_password
MYSQL_DATABASE=kodinet
MYSQL_USER=kodinet_user
MYSQL_PASSWORD=strong_db_password

# Private storage server
STORAGE_API_KEY=long_random_storage_api_key
STORAGE_MAX_SIZE_MB=4096
```

### 3. Start the stack

```bash
docker compose up -d --build
```

The application is available at `http://localhost:8080`.

Database migrations are applied automatically on startup.

### 4. First access

Sign in with a Microsoft account from your organization. The first user to connect automatically receives the **Owner** role.

---

## Configuration

### `appsettings.json` (structure — values come from `.env`)

```json
{
  "AzureAd": {
    "Instance":     "https://login.microsoftonline.com/",
    "TenantId":     "",
    "ClientId":     "",
    "ClientSecret": "",
    "CallbackPath": "/signin-oidc"
  },
  "Storage": {
    "BaseUrl":  "http://storage:8090",
    "ApiKey":   "",
    "RootPath": "/data"
  },
  "ConnectionStrings": {
    "DefaultConnection": ""
  }
}
```

All sensitive values are injected via Docker environment variables (format `Section__Key`).

### SMTP Configuration (from the UI)

Mail configuration is managed from **Settings → SMTP Email**, accessible only to the Owner role. The SMTP password is encrypted in the database via AES (DataProtection API).

---

## Enabling JSON-RPC on Kodi

On each Raspberry Pi, from the Kodi interface:

1. **Settings** → **Services** → **Control**
2. Enable **Allow remote control via HTTP**
3. Optional: set a username and password
4. Default port is `8080`

---

## Private Storage Server

The storage server is included in the Docker stack. It exposes a REST API on port `8090` (accessible only from the web container, not from the outside).

### Exposed Endpoints

| Method | URL | Description |
|---|---|---|
| `GET` | `/api/files?path=/folder` | List folder contents |
| `GET` | `/api/files/download?path=/file` | Download a file |
| `POST` | `/api/files/upload?path=/folder` | Upload a file (multipart) |
| `POST` | `/api/files/folder?path=/folder` | Create a folder |
| `DELETE` | `/api/files?path=/element` | Delete a file or folder |
| `PATCH` | `/api/files/rename` | Rename |
| `PATCH` | `/api/files/move` | Move |

**Authentication**: `X-Api-Key` header with the value of `STORAGE_API_KEY`.

### Size Limit

Configurable via `STORAGE_MAX_SIZE_MB` in `.env`. Default: 4096 MB (4 GB).

### Data Volume

Files are stored in the Docker volume `storage_data` (persistent). To mount an existing host directory:

```yaml
# In docker-compose.yml
volumes:
  - /path/on/host/videos:/data
```

---

## Microsoft Authentication

### Create an App Registration in Azure Entra ID

1. Azure Portal → **Azure Active Directory** → **App registrations** → **New registration**
2. **Name**: KodiNet
3. **Supported account types**: Accounts in this organizational directory only
4. **Redirect URI**: `https://your-domain.com/signin-oidc` (Web)
5. After creation, note **Application (client) ID** → `AZUREAD_CLIENTID`
6. Directory → **Directory (tenant) ID** → `AZUREAD_TENANTID`
7. **Certificates & secrets** → New client secret → note the value → `AZUREAD_CLIENTSECRET`

### Required Permissions

No Graph permissions required — KodiNet only uses basic OIDC authentication (OpenID + profile + email).

---

## Application Roles

Roles are managed from **Settings → Users**. They are stored in the internal database, independently of Azure AD.

| Role | Attribute | Rights |
|---|---|---|
| **Owner** | Unique — non-revocable — not assignable from the UI | All Admin rights + SMTP config + theme management + import/export + ownership transfer |
| **Admin** | Managed by Owner | Pi management, establishments, users, scheduler, notifications |
| **Operator** | Managed by Admin | Kodi player control, file transfers |
| **Viewer** | Managed by Admin | Read-only — Pi status monitoring |

### First Owner

The first user to sign in to the application automatically receives the Owner role. This mechanism only triggers once (when the `user_roles` table is empty).

### Ownership Transfer

From **Settings → Users**, the "Transfer Owner Role" button allows the Owner to delegate their role to another user. The current Owner is immediately signed out after the transfer.

---

## Features

### Dashboard

- Pi grid with real-time status (Playing / Idle / Offline / Incompatible)
- Filter by establishment, status, text search
- Sort by name, status, establishment (ascending/descending)
- Bulk Pi import from CSV file
- Global stats in the header bar (total / playing / offline)

**CSV import format for Pi:**
```
name,ip_address,location,model,kodi_user,kodi_password,kodi_port,ssh_user,ssh_password,ssh_port,video_folder
Hall Pi A,192.168.1.10,North Building,Pi 4B,kodi,password,8080,pi,password,22,/storage/videos
```

### Pi Detail Panel

**Info & Status tab**
- Kodi status, version, CPU temperature, free memory, disk space
- Volume control
- Configuration details

**Player & Files tab**
- Play/pause, previous/next track, stop
- Repeat mode (Off / One / All)
- File browsing via SFTP
- File upload from browser to Pi
- File renaming
- Direct link to Kodi web interface

### Private Storage

- Browse the storage server file tree
- Upload video files from the browser
- Create folders, rename, move, delete
- Send selected files to one or more Pi (background transfer)
- Transfer queue with progress tracking
- Storage operations log (import, deletion, rename, move)

### Settings

**Users (Admin)** — Role management and cron notification activation per user

**Establishments (Admin)** — CRUD for establishments + CSV import/export

**Scheduler (Admin)**
- `KodiRestarter` job: restarts idle players (Idle/Stopped) and plays the video folder on repeat
- `KodiRebooter` job: reboots the LibreELEC system on all Pi
- Cron expression configuration per job
- Enable/disable per job
- Execution history with per-Pi detail

**Themes (Owner)** — Create, edit, delete custom themes

**SMTP Email (Owner)** — Mail server configuration (AES encryption for password)

### Themes

4 built-in themes: **Ruby** (bordeaux), **Ocean** (petrol blue), **Amber** (warm wood). Each theme has light and dark modes. The mode (System / Light / Dark) is remembered per device in `localStorage`.

### Multilingual

The interface is available in **French** and **English**. Language is selected from the user menu (avatar icon in the top right). The choice is persisted per user account and synchronized across all connected devices.

---

## Local Development

### Prerequisites

- .NET 8 SDK
- MySQL 8.0 (local or Docker)
- Python 3.12+ (for the storage server)

### Getting Started

```bash
# Clone
git clone https://github.com/mathiasbenner/kodinet.git
cd kodinet

# Restore packages
dotnet restore KodiNet.sln

# Configure local secrets (never commit these)
cd KodiNet.Web
dotnet user-secrets set "AzureAd:TenantId"     "your-tenant-id"
dotnet user-secrets set "AzureAd:ClientId"     "your-client-id"
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;..."
dotnet user-secrets set "Storage:ApiKey" "your-key"

# Start the storage server (optional)
cd ../KodiNet.API
pip install -r requirements.txt
python main.py

# Start the application
cd ../KodiNet.Web
dotnet run
```

### `docker-compose.override.yml` (dev only, gitignored)

```yaml
services:
  db:
    ports:
      - "3306:3306"   # direct access from the IDE
  web:
    environment:
      ASPNETCORE_ENVIRONMENT: Development
```

---

## Database Migrations

Migrations are applied automatically on application startup.

To create a new migration after modifying an entity:

```bash
dotnet ef migrations add MigrationName \
  --project KodiNet.Infrastructure \
  --startup-project KodiNet.Web
```

---

## Security

- **Authentication**: Microsoft OIDC — only accounts from your Azure AD organization can sign in
- **Authorization**: application roles stored in database, loaded via `IClaimsTransformation`
- **Kodi/SSH credentials**: encrypted in database via AES (DataProtection API)
- **SMTP password**: encrypted in database via AES
- **Storage API Key**: injected via environment variable, never exposed client-side
- **Docker network**: MySQL and storage server on `internal: true` network — inaccessible from outside
- **No secrets in the repository**: `.env` and `appsettings.*.json` are gitignored

---

## License

MIT — see [LICENSE](LICENSE).

Main dependencies and their licenses:

| Package | License |
|---|---|
| MudBlazor | MIT |
| Microsoft.Identity.Web | MIT |
| Entity Framework Core | MIT |
| Pomelo.EntityFrameworkCore.MySql | MIT |
| SSH.NET | MIT |
| Cronos | MIT |
