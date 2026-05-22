# MediAid Project Overview

MediAid is a healthcare assistance platform built with ASP.NET Core MVC, .NET 8 and MongoDB.

The project connects four main user roles:

- Patient
- Aidant
- Expert
- Admin

The platform manages the full assistance workflow from request creation to mission completion.

## Business Problem

Patients may need non-emergency assistance, support, follow-up, or human help in different situations. MediAid provides a structured web platform to organize this support through verified workflows, proposals, mission tracking and notifications.

## Main Workflow

1. A patient creates an assistance request.
2. If needed, the request is validated by an expert.
3. Aidants can view available validated requests.
4. Aidants send proposals.
5. The patient accepts one proposal.
6. The mission becomes assigned.
7. The aidant starts the mission.
8. The mission becomes in progress.
9. The aidant uploads proof or the patient validates completion.
10. The mission becomes completed.
11. The patient can review the aidant.

## Technical Architecture

The application uses ASP.NET Core MVC with a layered architecture:

- Controllers for HTTP requests
- Services for business logic
- Models for domain entities
- DTOs for form data
- Razor Views for UI
- MongoDbContext for database access
- Filters for shared layout data

## Key Improvements Added

- Role-based route protection
- Safer authentication workflow
- Account deactivation check during login
- Public registration restricted to Patient and Aidant
- Expert validation enforced before aidant proposals
- Safer file upload logic
- Notifications linked to the correct user accounts
- Mission workflow protected against direct completion
- Basic location privacy on the map
- Professional GitHub README
- GitHub Actions CI workflow

## Status

This project is designed for academic and portfolio purposes. It is functional as a development application and can be extended with production-level features such as email verification, cloud storage, automated tests and deployment.
