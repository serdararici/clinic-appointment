# CLAUDE.md

Guidance for Claude Code (and any contributor) working in this repository.

## Project

Hospital/Clinic Appointment System — Option A of the course project brief
(`docs/OdevDosyasi-5aNrOgSgE257vRcmQ2Tkw.docx`). A platform for clinics/hospitals
(single-branch or multi-branch) to manage departments, services, doctors,
availability, and patient appointments, built to demonstrate OOP principles
(encapsulation, inheritance, polymorphism) and classic design patterns.

## Language & Stack

- **Backend**: C#, .NET 10, ASP.NET Core Web API.
- **Frontend**: React (Vite + TypeScript) in `/frontend`.
- **Database**: SQL Server LocalDB via EF Core.
  Connection string (in `appsettings.json`):
  `Server=(localdb)\MSSQLLocalDB;Database=ClinicDb;Trusted_Connection=True;TrustServerCertificate=True`
- All code, identifiers, and comments are written in **English**, even though
  the source requirements document is in Turkish. Add explanatory comments
  for non-obvious logic (why, not what).

## Solution Layout (layered architecture)

```
Clinic.sln
src/
  Clinic.Domain/          Entities, value objects, enums, domain interfaces. No dependencies on other layers.
  Clinic.Application/     Use cases/services, DTOs, strategy & policy interfaces, validation. Depends on Domain.
  Clinic.Infrastructure/  EF Core DbContext, migrations, repository implementations, seeding. Depends on Application + Domain.
  Clinic.Api/             ASP.NET Core Web API host, controllers, DI wiring, appsettings. Depends on Infrastructure.
tests/
  Clinic.Tests/           xUnit tests for Domain/Application logic.
frontend/                 Vite + React + TypeScript SPA client.
```

Dependency direction always points inward: `Api -> Infrastructure -> Application -> Domain`.
Domain must never reference Application, Infrastructure, or Api.

## Database & Startup Behavior

- EF Core migrations are applied automatically on application startup
  (`db.Database.Migrate()`), followed by idempotent seeding of sample data
  (clinic, departments, services, doctors, availability slots, users).
- Do not require a manual `dotnet ef database update` step for local dev —
  running the API should be enough to get a working, seeded database.

## OOP Showcase (required by the assignment)

- **Abstract base class** `User` with concrete subclasses `Patient`, `Doctor`,
  `ClinicManager` — shared identity/contact fields in the base, role-specific
  behavior/data in subclasses (inheritance + polymorphism).
- **Strategy pattern**: `IPricingStrategy` (e.g. standard vs. discounted/
  insurance pricing for a service) and `IConfirmationPolicy` (e.g. auto-confirm
  vs. manager-approval-required appointment confirmation).
- **Repository pattern**: generic `IRepository<T>` plus focused repository
  interfaces (e.g. `IAppointmentRepository`, `IDoctorRepository`) implemented
  in Infrastructure against EF Core.
- **Encapsulation**: entities expose behavior/invariants through methods
  (e.g. `Appointment.Confirm()`, `DoctorAvailability.Overlaps(...)`) rather
  than public setters for state that has invariants.

## Functional Requirements Checklist (from the brief)

- Clinics/hospitals defined with working hours and departments.
- Services defined with duration and price.
- Doctors registered with a specialty and the services they provide.
- Doctor availability slots defined; appointments can only be booked inside
  an availability slot.
- Appointment creation performs a conflict check (no double-booking a doctor
  for an overlapping time range).
- Appointment details (service, duration, price, doctor, date/time, patient)
  are persisted.
- Appointment approval mechanism (pending -> confirmed/rejected).
- At least three user roles: Patient, Doctor/Staff, Clinic/Hospital Manager.

## Testing

- **xUnit** only. Use plain `Assert.*` calls — do **not** add FluentAssertions
  or other assertion libraries.
- Prefer testing Domain and Application logic (conflict checks, strategies,
  policies, entity invariants) over trivial EF/infrastructure plumbing.

## Git Workflow

- Small, focused, meaningful commits — one logical change per commit.
- Commit messages in English, imperative mood (e.g. "Add doctor availability
  conflict check"), no emojis.
- The assignment requires a minimum of 10 commits made on different days;
  keep committing incrementally as features land rather than batching work
  into a few large commits.

## Build

- `dotnet build` must pass from the repository root before committing.
- `dotnet test` should pass once tests exist.
- Frontend: `npm install && npm run build` inside `/frontend` must pass once
  the frontend has real code.
