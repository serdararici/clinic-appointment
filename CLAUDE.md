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
  Validation lives in the entity: price 0 to 99,999,999.99 with at most two decimals,
  duration above zero and at most 24 hours, a known type, a non-blank name.
- A branch's working hours are ranges per weekday; two ranges on the same weekday may not
  overlap (back-to-back and a lunch gap are fine).
- `StaffMember.IsActive` (default true): a deactivated staff member keeps their history but
  cannot be booked and has no examination slots.

## Booking Rules

The service is chosen at **department** level: if the department has exactly one service
it is selected automatically; if it has several, they are listed. The service type then
decides the flow (`POST /api/appointments` takes `patientId`, `serviceId`, `startUtc` and,
for examinations, `doctorId`).

- **Examination**: the patient picks a doctor, then a slot. The doctor is required and
  must offer the service. The appointment occupies exactly one slot: its length is the
  **doctor's slot duration** (not the service's `Duration`). It must lie inside the branch
  working hours and one availability window, start on the doctor's slot grid, and not
  overlap another active appointment of that doctor.
- **Diagnostic**: no staff selection (a `doctorId` in the request is ignored). The patient
  picks a date and time; the appointment lasts the service's fixed `Duration`, starts on
  the branch's service grid, ends by closing time, and is stored **unassigned**
  (`StaffMemberId` is null). No two active appointments may overlap for the same service
  in the same branch (a service belongs to one branch through its department).
- "Active" means not cancelled and not rejected; pending and confirmed appointments hold
  their time. Pricing and confirmation always go through the strategies, for both types.

Slot endpoints (`date` is `YYYY-MM-DD`, read in clinic local time, Europe/Istanbul; they
return **all** slots with `startUtc`, `endUtc`, and `status` = `Available` | `Booked` | `Past`):

- `GET /api/doctors/{doctorId}/slots?date=` - the doctor's availability windows cut into
  slots of the doctor's slot duration, anchored at each window's start (a window that began
  on an earlier day is cut from local midnight). A slot shorter than one step at the end of a
  window is dropped, and so is any slot not entirely inside the branch working hours.
- `GET /api/services/{serviceId}/slots?date=` - Diagnostic services only (400 for an
  Examination service). The branch's working hours for that weekday cut into slots of the
  service's `Duration`, starting at opening time; a slot whose end passes closing is dropped.
- `Booked` = overlaps an active appointment (the doctor's, or for the same service). `Past` =
  the slot has already started (`start < now`); Past wins over Booked. "Now" comes from
  `TimeProvider`, so it can be fixed in tests.
- The grid rules live in one place, `SlotGrid` (Domain), and are used both to list slots and
  to check that a booking's start is aligned, so the list and the booking cannot disagree.
- Booking a start time in the past is rejected (422): same definition as `Past` in the slot
  list, so a slot starting exactly now is still bookable. "Now" comes from `TimeProvider`.
- A deactivated doctor cannot be booked (409).

## Management API

Create, read, update, list and delete, under the existing layering (controller -> Application
service -> repositories/entities). Access is role-based, see "Access rules" below. Request/response bodies
are DTOs; enums are written as names (`"Monday"`, `"Diagnostic"`) and numbers are still accepted.

| Resource | Endpoints |
|---|---|
| Organization | `GET/POST /api/organizations`, `GET/PUT/DELETE /api/organizations/{id}` |
| Branch | `GET/POST /api/organizations/{orgId}/branches`, `GET/PUT/DELETE /api/branches/{id}` |
| Working hours | `GET/POST /api/branches/{id}/working-hours` (`?dayOfWeek=`), `DELETE /api/branches/{id}/working-hours/{whId}` |
| Department | `GET/POST /api/branches/{id}/departments`, `GET/PUT/DELETE /api/departments/{id}` |
| Service | `GET/POST /api/departments/{id}/services`, `GET/PUT/DELETE /api/services/{id}` |
| Branch list | `GET /api/branches` (every branch with `organizationName`) |
| Doctors of a department | `GET /api/departments/{id}/doctors` (active doctors only, with offered services and slot duration) |
| Appointment lists | `GET /api/patients/{id}/appointments`, `GET /api/doctors/{id}/appointments` (patient, staff, branch and service names, time, price, status) |
| Appointment actions | `POST /api/appointments/{id}/confirm`, `/reject`, `/cancel` |
| Bulk availability | `POST /api/doctors/{id}/availability/bulk` (date range + weekdays + local time window; skips overlapping and past dates) |
| Staff | `POST /api/departments/{id}/doctors`, `POST /api/departments/{id}/technicians`, `GET /api/staff` (`?branchId=&departmentId=`), `GET/PUT/DELETE /api/staff/{id}`, `PUT /api/staff/{id}/services`, `PUT /api/staff/{id}/slot-duration`, `POST /api/staff/{id}/deactivate`, `POST /api/staff/{id}/activate` |

Status codes: 201 on create (with a `Location`), 200 on read/update, 204 on delete; **400** invalid input
(`ArgumentException`), **404** unknown id or parent (`KeyNotFoundException`), **409** a rule or dependent
data in the way (`InvalidOperationException`). `ApiExceptionHandler` maps these three exception types
centrally; the older booking/slot controllers still catch their own exceptions.

Delete rules (409 with a message when violated):

- Organization: no branches. Branch: no departments and no clinic managers (its working hours go with it).
- Department: no services and no staff.
- Service: no appointments (any status, it is history) and no staff offering it. Its **type** also cannot
  be changed once appointments exist; other fields stay editable.
- Staff: no upcoming appointments, i.e. no pending/confirmed appointment that has not finished yet.
  The same rule blocks **deactivating** them. Cancelled, rejected and finished appointments do not block.
  Deleting staff leaves past appointments pointing at a removed id (there is no foreign key on
  `Appointment.StaffMemberId`); deactivating keeps the history intact.
- `PUT /api/staff/{id}/services` replaces the offered set; every service must belong to the staff
  member's own department (400 otherwise, nothing changes).

## Authentication and Authorization

- `User.PasswordHash` stores a salted hash made with ASP.NET's `PasswordHasher` (only the hasher, not the Identity
  tables). Email is the login name and has a unique index (case-insensitive).
- `POST /api/auth/register` creates a **Patient** (`fullName`, `email`, `password`, `phoneNumber`, `dateOfBirth`,
  optional `insuranceNumber`): 400 if the password is under 8 characters or the input is invalid, 409 if the
  email is already used. `POST /api/auth/login` (`email`, `password`) returns a JWT valid for 8 hours:
  401 with the same message for a wrong password and an unknown email.
- The token carries `sub` (user id), `email`, `name`, `role` (`Patient`, `Doctor`, `HealthTechnician`,
  `ClinicManager`) and, for staff and clinic managers only, `branch_id` and `organization_id`.
- `Jwt:Key` (at least 32 characters), `Jwt:Issuer` and `Jwt:Audience` come from configuration. The committed
  `appsettings.json` holds a **development-only** key; outside local development override it with the
  environment variable `Jwt__Key`. The app refuses to start with a missing or too-short key.
- A **deactivated** staff member cannot log in (the same 401 as a wrong password).

### Access rules (JWT bearer, role-based)

Every token is validated for signature, issuer, audience and expiry (`AddJwtAuthentication`). Every controller action
is annotated explicitly with `[AllowAnonymous]` or `[Authorize(Roles = ...)]` (constants in `AppRoles`), and a
fallback policy makes any forgotten endpoint require a login. `AuthorizationGuardTests` scans all actions by
reflection and fails if one has neither attribute (or `[Authorize]` without roles), so **a new endpoint must be
annotated and, if it matters, added to the role-mapping table in that test**.

| Who | May call |
|---|---|
| Anyone (anonymous) | `auth/register`, `auth/login`; reads of organizations, branches (and their working hours), departments, services, `departments/{id}/doctors`; slot lists (`doctors/{id}/slots`, `services/{id}/slots`) |
| Patient | `POST appointments`, `GET patients/{id}/appointments`, `POST appointments/{id}/cancel` |
| Doctor | `doctors/{id}/availability` (list, add, bulk, delete), `GET doctors/{id}/appointments`, `PUT staff/{id}/slot-duration`, confirm / reject |
| ClinicManager | all create / update / delete of organizations, branches, working hours, departments, services and staff (including activate / deactivate, offered services, `GET staff`), `PUT staff/{id}/slot-duration`, confirm / reject |

Errors use the plain-text style of the rest of the API: **401** when there is no valid token (missing, bad
signature, wrong issuer/audience, expired), **403** when the role is not allowed. Whose data it is (a patient only
sees their own list, a doctor only their own availability, a manager only their own branch) is **not enforced yet**:
that comes in step 5.3, so for now any user of the right role can pass any id.

### Demo accounts

All demo accounts use the password **`Demo123!`** and are given it at startup only if they have no password yet
(`DbInitializer.DemoEmails`). Technicians have no demo login.

| Role | Email | Works at |
|---|---|---|
| Patient | `ahmet.celik@example.com` (insured), `fatma.yildirim@example.com` | - |
| Doctor | `hakan.yildiz@sakaryasehir.example`, `elif.sahin@sakaryasehir.example` | Sakarya Şehir Hastanesi - Merkez |
| Doctor | `zeynep.celik@sakaryasehir.example`, `burak.ozturk@sakaryasehir.example` | Sakarya Şehir Hastanesi - Adapazarı Şubesi |
| Doctor | `can.aksoy@marmarapoliklinik.example` | Marmara Poliklinik - Serdivan Kliniği |
| Clinic manager | `ayse.yilmaz@sakaryasehir.example` | Merkez |
| Clinic manager | `mehmet.kara@sakaryasehir.example` | Adapazarı Şubesi |
| Clinic manager | `ali.erdogan@marmarapoliklinik.example` | Serdivan Kliniği |

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

## Verification and Reporting

- Keep verification lean: after each step run `dotnet build` and `dotnet test` **once**.
- Do not run the API against throwaway databases, and do not repeat full test runs,
  unless the user asks.
- Keep reports short.

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
