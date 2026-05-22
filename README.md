# MediAid Healthcare Assistance Platform

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-blue?style=for-the-badge)
![MongoDB](https://img.shields.io/badge/Database-MongoDB-47A248?style=for-the-badge&logo=mongodb&logoColor=white)
![Razor Views](https://img.shields.io/badge/Frontend-Razor%20Views-purple?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Development-orange?style=for-the-badge)

**MediAid** is a healthcare assistance platform built with **ASP.NET Core MVC**, **.NET 8**, and **MongoDB**.

The platform connects patients, caregivers, experts, and administrators through a structured workflow for healthcare assistance requests, proposals, mission tracking, notifications, chat, reviews, and safety reporting.

---

## Overview

MediAid is a web platform designed to coordinate healthcare assistance between patients who need support and aidants who can provide help.

Patients can create assistance requests, aidants can send proposals, experts can validate sensitive requests, and administrators can supervise platform activity.

The project focuses on a real end-to-end workflow with authentication, role-based access, MongoDB persistence, mission tracking, notification management, and a professional Razor-based interface.

---

## Main Features

### Authentication and Account Management

- User registration and login
- Cookie-based authentication
- BCrypt password hashing
- Account activation and deactivation logic
- Role-based access control
- Change password
- Change email
- General account profile management

### Patient Features

- Patient dashboard
- Create assistance requests
- View personal requests
- Accept or reject aidant proposals
- Follow mission status
- Access notifications and chat
- Review completed missions
- Manage patient profile information

### Aidant Features

- Aidant dashboard
- View available validated requests
- Send proposals
- Track sent proposals
- Start assigned missions
- Upload mission proof
- Manage availability, skills, and location
- Access planning, notifications, and chat

### Expert Features

- Expert dashboard
- Review sensitive requests
- Validate or reject requests requiring expert supervision

### Admin Features

- Admin dashboard
- User management
- Account suspension
- Audit logs
- Platform supervision
- Safety incident monitoring

---

## User Roles

| Role | Description |
|---|---|
| Patient | Creates assistance requests and follows missions |
| Aidant | Sends proposals and completes assistance missions |
| Expert | Validates sensitive healthcare-related requests |
| Admin | Manages users, logs, and platform activity |

Public registration is limited to:

```text
Patient
Aidant
```

Expert and Admin roles should be assigned through controlled administration logic.

---

## Core Workflow

```text
Patient creates a request
        ↓
Request requires expert validation?
        ↓
Expert validates it if required
        ↓
Aidant views available validated requests
        ↓
Aidant sends a proposal
        ↓
Patient accepts one proposal
        ↓
Mission becomes Assigned
        ↓
Aidant starts the mission
        ↓
Mission becomes InProgress
        ↓
Aidant uploads proof
        ↓
Mission becomes PendingVerification
        ↓
Patient verifies completion
        ↓
Mission becomes Completed
        ↓
Patient can review the aidant
```

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core MVC |
| Framework | .NET 8 |
| Database | MongoDB |
| Frontend | Razor Views, HTML, CSS, Bootstrap |
| Authentication | Cookie Authentication for MVC sessions |
| Password Security | BCrypt |
| Data Access | MongoDB Driver |
| Infrastructure | Docker Compose |
| Development OS | Windows / PowerShell |

---

## Architecture

```text
Browser
   |
   v
ASP.NET Core MVC
   |
   |-- Controllers
   |-- Services
   |-- Filters
   |-- Razor Views
   |
   v
MongoDB Database
```

---

## Project Structure

```text
MediAid/
│
├── README.md
│
└── server/
    ├── Controllers/
    ├── Data/
    ├── DTOs/
    ├── Filters/
    ├── Hubs/
    ├── Models/
    ├── Services/
    ├── Views/
    ├── wwwroot/
    ├── Program.cs
    ├── MediAid.csproj
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── docker-compose.yml
    ├── start-dev.ps1
    ├── stop-dev.ps1
    └── .gitignore
```

---

## Prerequisites

Install:

- .NET 8 SDK
- Docker Desktop
- Git
- MongoDB Compass, optional

Check installations:

```powershell
dotnet --version
docker --version
git --version
```

---

## Installation

```powershell
git clone https://github.com/Userhilal/MediAid-Healthcare-Assistance-Platform.git
cd MediAid-Healthcare-Assistance-Platform/server
dotnet restore
dotnet build
```

---

## Run the Project

### Recommended

From the `server` folder:

```powershell
.\start-dev.ps1
```

Open:

```text
http://localhost:5000
```

Mongo Express:

```text
http://localhost:8081
```

Mongo Express credentials:

```text
admin / admin
```

### Manual Run

```powershell
docker compose up -d
dotnet run --launch-profile http
```

---

## Configuration

Development configuration is stored in:

```text
server/appsettings.Development.json
```

Example:

```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017",
    "DatabaseName": "Mediaid"
  },
  "JwtSettings": {
    "SecretKey": "DevelopmentSecretKeyForMediAidMustBeAtLeast32CharactersLong",
    "Issuer": "MediAid",
    "Audience": "MediAidUsers",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

Production secrets should not be committed.

---

## Main Routes

| Route | Description |
|---|---|
| `/` | Home page |
| `/Account/Login` | Login |
| `/Account/Register` | Register |
| `/Dashboard` | Role-based dashboard |
| `/Profile` | General account profile |
| `/Patient/Profile` | Patient profile |
| `/Aidant/Profile` | Aidant profile |
| `/Request` | Patient request management |
| `/Proposal` | Aidant proposal area |
| `/Mission` | Mission tracking |
| `/Notification` | User notifications |
| `/Chat` | Conversations |
| `/Map` | Map view |
| `/Expert` | Expert validation area |
| `/Admin` | Administration dashboard |

---

## Security and Privacy Improvements

The project includes:

- BCrypt password hashing
- Account deactivation check during login
- Role-based controller protection
- Public registration restricted to Patient and Aidant
- Cookie authentication for MVC sessions, with `JwtSettings` prepared for future API usage
- Notifications linked to correct user IDs
- Expert validation before aidants can propose help
- Safer file upload handling
- Runtime uploads excluded from Git
- Basic location privacy for unassigned users
- Clear separation between account, patient profile, and aidant profile
- Mission workflow that prevents direct completion without proof or verification

---


---

## Authentication Clarification

MediAid currently uses **Cookie Authentication** for ASP.NET Core MVC sessions.

JWT settings are kept in the configuration for future API usage, but the current web application authentication flow is session-based.

## Development Commands

```powershell
cd server
dotnet clean
dotnet restore
dotnet build
dotnet run --launch-profile http
```

Stop local app processes:

```powershell
Get-Process MediAid -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force
```

---

## Screenshots

Recommended screenshots to add later:

```text
docs/screenshots/home.png
docs/screenshots/login.png
docs/screenshots/patient-dashboard.png
docs/screenshots/aidant-dashboard.png
docs/screenshots/request-create.png
docs/screenshots/proposals.png
docs/screenshots/mission-tracking.png
docs/screenshots/notifications.png
docs/screenshots/admin-dashboard.png
docs/screenshots/map.png
```

---

## Current Limitations

The project is still under development. Some features need production hardening:

- Full email verification workflow
- Password reset by email
- Complete refresh token implementation
- Advanced audit logging
- Cloud file storage
- Automated tests
- CI/CD pipeline
- Production deployment configuration
- Full legal privacy compliance workflow

---

## Future Improvements

- Add automated tests
- Add GitHub Actions CI
- Add Dockerfile for the ASP.NET Core app
- Improve real-time chat with SignalR events
- Add email notifications
- Add stricter upload validation
- Add admin analytics dashboard
- Add advanced geolocation matching
- Add request recommendation system for aidants
- Add data export and full account deactivation/anonymization workflow
- Add deployment guide

---

## Author

**Hind Hilal**  
Computer Science and Networks Engineering Student  

GitHub: [@Userhilal](https://github.com/Userhilal)

---

## License

This project was developed for academic and portfolio purposes.


