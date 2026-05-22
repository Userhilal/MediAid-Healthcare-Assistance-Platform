# MediAid Healthcare Assistance Platform

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-blue?style=for-the-badge)
![MongoDB](https://img.shields.io/badge/Database-MongoDB-47A248?style=for-the-badge&logo=mongodb&logoColor=white)
![Razor Views](https://img.shields.io/badge/Frontend-Razor%20Views-purple?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Portfolio%20Project-success?style=for-the-badge)

**MediAid** is a healthcare assistance platform built with **ASP.NET Core MVC**, **.NET 8**, and **MongoDB**.

The platform connects patients, aidants, experts, and administrators through a structured workflow for healthcare assistance requests, expert validation, proposals, mission tracking, proof submission, notifications, reviews, and account management.

The project is designed as a real end-to-end academic and portfolio web application with role-based access, MongoDB persistence, backend workflows, Razor views, and a professional GitHub setup.

---

## Overview

MediAid helps coordinate non-emergency healthcare assistance between people who need help and people who can provide support.

Patients can create assistance requests. Aidants can browse validated available requests and send proposals. Patients can accept proposals and follow mission progress. Experts validate sensitive requests before they become visible. Administrators supervise users, logs, and platform activity.

The application is not a static demo. It implements a complete workflow from account creation to mission completion.

---

## Main Features

### Authentication and Account Management

- User registration and login
- Cookie-based authentication for MVC sessions
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
- Access notifications
- Access conversations
- Review completed missions
- Manage patient-specific profile information

### Aidant Features

- Aidant dashboard
- View available validated requests
- Send proposals to patients
- Track sent proposals
- Start assigned missions
- Upload mission proof
- Manage availability, skills, and location
- Access planning
- Access notifications and conversations

### Expert Features

- Expert dashboard
- Review sensitive healthcare-related requests
- Validate or reject requests requiring expert supervision
- Help protect the quality and safety of published requests

### Admin Features

- Admin dashboard
- User management
- Account supervision
- Audit logs
- Platform monitoring
- Safety incident monitoring

---

## User Roles

| Role | Description |
|---|---|
| Patient | Creates assistance requests and follows missions |
| Aidant | Sends proposals and completes assistance missions |
| Expert | Validates sensitive healthcare-related requests |
| Admin | Manages users, logs, and platform supervision |

Public registration is intentionally limited to:

```text
Patient
Aidant
```

Admin and Expert accounts are created through a controlled role promotion script.

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
| Local Infrastructure | Docker Compose |
| Development Environment | Windows / PowerShell |
| CI | GitHub Actions |

---

## Architecture

```text
Browser
   |
   v
ASP.NET Core MVC Application
   |
   |-- Controllers
   |-- Services
   |-- Filters
   |-- Razor Views
   |-- Models / DTOs
   |
   v
MongoDB Database
```

### Main Layers

| Layer | Responsibility |
|---|---|
| Controllers | Handle HTTP requests and user actions |
| Services | Business logic and workflow rules |
| Models | Domain entities stored in MongoDB |
| DTOs | Form and input data |
| Views | Razor pages rendered to the user |
| Filters | Shared layout data such as notifications |
| MongoDbContext | MongoDB collections and database access |

---

## Project Structure

```text
MediAid/
│
├── README.md
├── docs/
│   ├── PROJECT_OVERVIEW.md
│   ├── QUALITY_CHECKLIST.md
│   └── WORKFLOWS.md
│
├── .github/
│   └── workflows/
│       └── dotnet-ci.yml
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
    ├── Scripts/
    │   ├── promote-role.ps1
    │   └── verify-project.ps1
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

Clone the repository:

```powershell
git clone https://github.com/Userhilal/MediAid-Healthcare-Assistance-Platform.git
cd MediAid-Healthcare-Assistance-Platform/server
```

Restore dependencies:

```powershell
dotnet restore
```

Build the project:

```powershell
dotnet build
```

---

## Run the Project

### Recommended Development Run

From the `server` folder:

```powershell
.\start-dev.ps1
```

Open the application:

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

---

### Manual Run

Start MongoDB:

```powershell
docker compose up -d
```

Run the application:

```powershell
dotnet run --launch-profile http
```

Open:

```text
http://localhost:5000
```

---

### Stop the Local App

```powershell
.\stop-dev.ps1
```

Or manually:

```powershell
Get-Process MediAid -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force
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

## Authentication Clarification

MediAid currently uses **Cookie Authentication** for ASP.NET Core MVC sessions.

JWT settings are kept in the configuration for future API usage, but the current web application authentication flow is session-based.

---

## Creating Admin and Expert Accounts

Public registration is limited to `Patient` and `Aidant`.

To create an `Admin` or `Expert` account in development:

1. Start the application.
2. Register a normal account from `/Account/Register`.
3. Promote the account using the role promotion script.

From the repository root:

```powershell
.\server\Scripts\promote-role.ps1 -Email "admin@example.com" -Role Admin
.\server\Scripts\promote-role.ps1 -Email "expert@example.com" -Role Expert
```

This keeps public registration safe while still allowing development and testing of privileged roles.

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
- Cookie authentication for MVC sessions
- JWT settings prepared for future API usage
- Notifications linked to correct user IDs
- Expert validation before aidants can propose help
- Runtime uploads excluded from Git
- Basic location privacy for unassigned users
- Clear separation between account profile, patient profile, and aidant profile
- Mission workflow that prevents direct completion without proof and verification
- Controlled Admin and Expert role promotion script

---

## Quality and Verification

A final verification script is available:

```powershell
.\server\Scripts\verify-project.ps1
```

It checks:

- Required project files
- Encoding issues
- Backup source files that could break the build
- Runtime upload tracking
- README presence
- .NET build status
- Git status

---

## Documentation

Additional documentation is available in the `docs/` folder:

| File | Description |
|---|---|
| `docs/PROJECT_OVERVIEW.md` | Project objective, architecture, and improvements |
| `docs/WORKFLOWS.md` | Functional workflows |
| `docs/QUALITY_CHECKLIST.md` | Quality, security, and production-readiness checklist |

---

## Development Commands

Clean:

```powershell
dotnet clean
```

Restore:

```powershell
dotnet restore
```

Build:

```powershell
dotnet build
```

Run:

```powershell
dotnet run --launch-profile http
```

Check Git status:

```powershell
git status
```

Commit and push:

```powershell
git add .
git commit -m "Update MediAid project"
git push origin main
```

---

## Current Limitations

The project is still under development. Some features require production hardening:

- Full email verification workflow
- Password reset by email
- Complete API authentication if JWT is used later
- Cloud file storage
- Authorized file serving outside `wwwroot`
- Automated tests
- Advanced audit logs
- CI test stage
- Production deployment configuration
- Full account anonymization and export workflow
- Legal privacy compliance review

---

## Future Improvements

- Add automated unit and integration tests
- Add GitHub Actions test stage
- Add production Dockerfile for the ASP.NET Core app
- Improve real-time chat with SignalR events
- Add email notifications
- Add stricter upload validation
- Store sensitive files outside `wwwroot`
- Add admin analytics dashboard
- Add advanced geolocation matching
- Add request recommendation system for aidants
- Add deployment guide for Azure, Render, or Railway

---

## Author

**Hind Hilal**  
Computer Science and Networks Engineering Student  

GitHub: [@Userhilal](https://github.com/Userhilal)

---

## License

This project was developed for academic and portfolio purposes.
