# CLAUDE.md

Guidance for Claude Code (and any contributor) working in this repository.

## Project

Hospital/Clinic Appointment System — Option A of the course project brief
(`docs/OdevDosyasi.docx`). A platform for health organizations (single-branch or
multi-branch) to manage branches, departments, services, staff, doctor
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
- The seeded **sample data** (organizations, branches, departments, services,
  people) is in Turkish on purpose; only code, identifiers and comments are English.

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
  (organizations, branches, departments, services, staff, patients, managers,
  and a rolling 14-day weekday availability schedule for doctors).
- Do not require a manual `dotnet ef database update` step for local dev —
  running the API should be enough to get a working, seeded database.

## OOP Showcase (required by the assignment)

- **Abstract base class** `User` with `Patient`, `ClinicManager` and the abstract
  `StaffMember`, which in turn has `Doctor` and `HealthTechnician`. Shared
  identity/contact fields live in `User`; facility/department link, services,
  availability and slot duration live in `StaffMember`; role-specific data
  (`Doctor.Specialty`, `HealthTechnician.Title`) in the leaves (inheritance +
  polymorphism, e.g. the abstract `RoleName`). The whole tree is stored in one
  `Users` table (EF Core TPH) with a `UserType` discriminator.
- **Strategy pattern**: `IPricingStrategy` (e.g. standard vs. discounted/
  insurance pricing for a service) and `IConfirmationPolicy` (e.g. auto-confirm
  vs. manager-approval-required appointment confirmation).
- **Repository pattern**: generic `IRepository<T>` plus focused repository
  interfaces (e.g. `IAppointmentRepository`, `IDoctorRepository`) implemented
  in Infrastructure against EF Core.
- **Encapsulation**: entities expose behavior/invariants through methods
  (e.g. `Appointment.Confirm()`, `DoctorAvailability.Overlaps(...)`,
  `StaffMember.SetSlotDuration(...)`) rather than public setters for state that
  has invariants. Collections are private lists exposed as read-only views.

## Domain Model

```
HealthOrganization (Id, Name)
  └─ ClinicFacility = a branch (OrganizationId required, Name = branch name, Address)
       ├─ WorkingHour        per branch: day of week + opens/closes (clinic local time)
       └─ Department
            └─ Service       Name, ServiceType, Duration, BasePrice
StaffMember (abstract, : User)   ClinicFacilityId, DepartmentId, SlotDurationMinutes,
  │                              services provided, availability windows
  ├─ Doctor                      + Specialty
  └─ HealthTechnician            + Title (registered only; no scheduling logic)
Appointment   PatientId, StaffMemberId? (null = unassigned), ServiceId, start/end (UTC),
              Price, Status
```

- An organization is created first; a branch cannot exist without one
  (`ClinicFacility.OrganizationId` is required and a foreign key). Each branch
  has its own working hours and departments, so services belong to exactly one branch.
- A `StaffMember` is built from a `Department`; its `ClinicFacilityId` is read from
  that department, so staff can never be linked to a department of another branch.
  A staff member may only provide services of their own department.
- `SlotDurationMinutes` defaults to 30; only 10, 15, 20, 30 and 60 are allowed
  (validated in the domain; `PUT /api/staff/{staffId}/slot-duration`).
- `Service` keeps the names `Service` and `BasePrice`. `ServiceType` is
  `Examination` or `Diagnostic`. The service duration is always defined by the manager.

## Booking Rules (target design)

Status: the data model, migration and Turkish seed for these rules are done. The
booking flow itself (slot-based and diagnostic booking) is **not implemented yet**;
today `POST /api/appointments` still takes a doctor and uses the service's duration.

- The service is chosen at **department** level: if the department has exactly one
  service it is selected automatically; if it has several, they are listed.
- **Examination**: the patient picks a doctor, then a slot whose length is that
  doctor's slot duration, inside the doctor's availability windows.
- **Diagnostic**: no staff selection. The patient picks a date and time; the
  appointment lasts the service's fixed duration, must fall inside the branch's
  working hours, and is stored unassigned (`StaffMemberId` is null).
- No two active (pending/confirmed) appointments may overlap for the same
  **service in the same branch** (diagnostic), nor for the same doctor (examination).

## Date and Time

- All stored and exchanged date-times are **UTC**. `ClinicTimeZone` (Domain) is the single
  place defining the clinic time zone (`Europe/Istanbul`).
- Working hours are wall-clock times in the clinic time zone; a UTC range is converted
  to local time before it is compared (`ClinicFacility.IsOpenDuring`).
- API input date-times must be UTC (`Z`) or carry an offset; a date-time with no zone
  is rejected (400).

## Functional Requirements Checklist (from the brief)

- Organizations and their branches defined with working hours and departments.
- Services defined with type, duration and price.
- Doctors registered with a specialty and the services they provide; health
  technicians registered as staff.
- Doctor availability slots defined; appointments can only be booked inside
  an availability slot.
- Appointment creation performs a conflict check (no double-booking a doctor
  for an overlapping time range).
- Appointment details (service, duration, price, doctor if any, date/time, patient)
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
- Never run `git commit`, `git push` or any command that modifies git history.
  Never add `Co-Authored-By` lines or mention Claude in commit messages. When a
  step is done, provide the exact `git add` paths and a commit message for the
  user to run themselves.
- The assignment requires a minimum of 10 commits made on different days;
  keep committing incrementally as features land rather than batching work
  into a few large commits.

## Build

- `dotnet build` must pass from the repository root before committing.
- `dotnet test` should pass once tests exist.
- Frontend: `npm install && npm run build` inside `/frontend` must pass once
  the frontend has real code.
