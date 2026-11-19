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
  (organizations, branches, departments, services, staff, patients and managers).
  Doctor availability is not seeded directly: right after the seed, and then every 6 hours
  (`DefaultScheduleWorker`, `DefaultSchedule:IntervalHours`), `IDefaultScheduleService` fills the default work
  schedule for every doctor who has `AutoScheduleEnabled` (see "Booking window and automatic schedule").
- Do not require a manual `dotnet ef database update` step for local dev —
  running the API should be enough to get a working, seeded database.

## OOP Showcase (required by the assignment)

- **Abstract base class** `User` with `Patient`, `ClinicManager`, `SystemAdmin` and the abstract
  `StaffMember`, which in turn has `Doctor` and `HealthTechnician`. Shared
  identity/contact fields live in `User`; facility/department link, services,
  availability and slot duration live in `StaffMember`; role-specific data
  (`HealthTechnician.Title`) in the leaves (inheritance +
  polymorphism, e.g. the abstract `RoleName`). The whole tree is stored in one
  `Users` table (EF Core TPH) with a `UserType` discriminator.
- **Strategy pattern**: `IPricingStrategy` (e.g. standard vs. discounted/
  insurance pricing for a service) and `IConfirmationPolicy` (auto-confirm,
  approval-required, and the per-doctor choice, see "Appointment approval").
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
  ├─ Doctor                      (no own specialty: it is the department name)
  └─ HealthTechnician            + Title (registered only; no scheduling logic; never signs in)
Appointment   PatientId, StaffMemberId? (null = unassigned), ServiceId, start/end (UTC),
              Price, Status
```

- An organization is created first; a branch cannot exist without one
  (`ClinicFacility.OrganizationId` is required and a foreign key). Each branch
  has its own working hours and departments, so services belong to exactly one branch.
- A `StaffMember` is built from a `Department`; its `ClinicFacilityId` is read from
  that department, so staff can never be linked to a department of another branch.
  A staff member may only provide services of their own department, and of their own kind: a **doctor** offers
  **Examination** services, a **health technician** is linked to **Diagnostic** services (`StaffMember.OfferedServiceType`;
  400 otherwise). The technician link is a definition only: booking, slots, availability and appointments do not use it
  (a diagnostic appointment still has no staff member).
- `SlotDurationMinutes` defaults to 30; only 10, 15, 20, 30 and 60 are allowed
  (validated in the domain; `PUT /api/staff/{staffId}/slot-duration`).
- `Service` keeps the names `Service` and `BasePrice`. `ServiceType` is
  `Examination` or `Diagnostic`. The service duration is always defined by the manager.
  Validation lives in the entity: price 0 to 99,999,999.99 with at most two decimals,
  duration above zero and at most 24 hours, a known type, a non-blank name.
- A **new branch** (`POST /api/organizations/{id}/branches`) is created with default working hours Monday-Friday
  08:00-17:00 (`ClinicFacility.AddDefaultWorkingHours`), so doctors added to it are bookable; they can be changed with
  the working-hours endpoints. (The seeded branches define their own hours.)
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
- **Appointment approval** (per doctor): `Doctor.AutoConfirmAppointments` (default true). `IConfirmationPolicy.DetermineInitialStatus(Doctor?)`
  is implemented by `AutoConfirmationPolicy` (Confirmed), `ApprovalRequiredConfirmationPolicy` (Pending) and
  `DoctorPreferenceConfirmationPolicy`, which is the registered one: a booking without a doctor (Diagnostic) or with a
  doctor who auto-confirms gets Confirmed, otherwise Pending. Switching the whole system back to auto-confirm is a
  one-line change in `DependencyInjection` (register `AutoConfirmationPolicy` as `IConfirmationPolicy`). The assigned
  doctor confirms or rejects a **Pending** appointment with `POST /api/appointments/{id}/confirm` / `/reject` (managers
  of the organization and admins may too; another doctor and patients get 403); anything not Pending is **409**.
  A rejected appointment frees the slot and the patient sees "Reddedildi". `PUT /api/doctors/{id}/auto-confirm`
  (`{ "enabled": bool }`, doctor only, own id) changes the setting; turning it on does not touch appointments that are
  already Pending. `GET doctors/{id}/profile` includes `autoConfirmAppointments`. The seed has exactly one doctor with it
  off: **Dr. Kemal Aslan** (`kemal.aslan@sakaryasehir.example`, Kardiyoloji, Merkez), for demonstrating the flow.
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

### Booking window and automatic schedule

- **Booking window**: patients can only book from today up to today + 14 days (Europe/Istanbul calendar days,
  both ends included). The number lives in one place, `BookingRules.WindowDays` (Domain); the frontend mirrors it in
  `BOOKING_WINDOW_DAYS`. The slot endpoints return **no slots** outside the window (a date in the past included),
  and booking outside it is **422** with the Turkish message "Randevu yalnızca bugünden itibaren 14 gün içindeki
  tarihler için alınabilir." (`OutsideBookingWindowException`).
- **Automatic schedule**: `Doctor.AutoScheduleEnabled` (default true). The default schedule is weekdays only,
  09:00-12:00 and 13:00-16:00 clinic time (12:00-13:00 is the lunch break), `DefaultSchedule.Windows`. A window is cut
  to whole slots of the doctor's slot duration (all allowed durations divide the 3-hour windows).
- `IDefaultScheduleService.EnsureDefaultAvailabilityAsync(doctorId, utcNow)` adds the default windows for every weekday
  from max(today, the day after the doctor's last existing availability day) up to today + 14 days. It only extends
  **forward**: days the doctor removed before their last availability day stay closed (a day off), but if the doctor
  deletes the *last* days while the switch is on, they come back. It does nothing for a doctor with the switch off (or
  deactivated) and is idempotent. `EnsureForAllAsync` does the same for all auto-schedule doctors; it runs at startup and
  every 6 hours. The future "create doctor" flow should call `EnsureDefaultAvailabilityAsync` so a new doctor has
  availability immediately.
- `GET` / `PUT /api/doctors/{id}/auto-schedule` (`{ "enabled": true }`, doctor only, own id): reads and sets the switch;
  switching it on fills the window right away.

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
- `POST /api/departments/{id}/doctors` takes a **temporary password** (`password`, at least 8 characters; the doctor logs
  in with the email and this password). 400 for an invalid email or short password, 409 if the email is used (also when
  `PUT staff/{id}` changes an email to one that is taken). The request may carry `serviceIds` (the examination services
  the doctor offers): when left out the doctor offers **all Examination services of the department**; when given, every id
  must be an examination service of the department and **at least one** is required (400 otherwise). The doctor and the
  services are saved in one operation, and the default work schedule is filled in at once (`EnsureDefaultAvailabilityAsync`),
  so the new doctor is bookable immediately. `GET /api/departments/{id}/doctors?serviceId=` lists only the doctors who
  offer that service (the booking flow's doctor step).
- **Health technicians** are staff records only: the manager creates them with name, email, phone and department (no
  password; the title is optional and defaults to "Sağlık Teknisyeni"), lists them with the doctors (`GET /api/staff`, the
  `role` field tells `Doctor` from `HealthTechnician`), edits, deactivates and activates them with the same scope rules as
  doctors. `User.CanSignIn` is **false** for technicians, so the login refuses them like a wrong password. The create
  request may carry `serviceIds` (diagnostic services of the department; an empty list is fine); when left out the
  technician is linked to **all Diagnostic services of the department**. A technician's services are in the staff DTO's
  `serviceIds` like a doctor's.
- `PUT /api/staff/{id}/services` replaces the offered set (a doctor's current services are the `serviceIds` of the staff
  record) and works for doctors and technicians under the same manager scope rules; a doctor can only offer
  **Examination** services and a technician only **Diagnostic** ones, and every service must belong to the staff
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

### Stale tokens

A token is signed and valid for 8 hours, but its user can be deleted (for example a reset database) or deactivated in
that time. After the signature, issuer, audience and expiry are validated, `OnTokenValidated` runs
`TokenUserValidation` / `IUserStatusChecker`: the user in the `sub` claim must still exist and `User.CanSignIn` must be
true (false only for deactivated staff and clinic managers; patients have no flag). Otherwise the answer is **401** with the
usual "The token is invalid or has expired." text, so the frontend ends the session. This is **one primary-key lookup of
the user per authenticated request**. The role still comes from the token, as before.

### Access rules (JWT bearer, role-based)

Every token is validated for signature, issuer, audience and expiry (`AddJwtAuthentication`). Every controller action
is annotated explicitly with `[AllowAnonymous]` or `[Authorize(Roles = ...)]` (constants in `AppRoles`), and a
fallback policy makes any forgotten endpoint require a login. `AuthorizationGuardTests` scans all actions by
reflection and fails if one has neither attribute (or `[Authorize]` without roles), so **a new endpoint must be
annotated and, if it matters, added to the role-mapping table in that test**.

| Who | May call |
|---|---|
| Anyone (anonymous) | `auth/register`, `auth/login`; reads of organizations, branches (and their working hours), departments, services, `departments/{id}/doctors`; slot lists (`doctors/{id}/slots`, `services/{id}/slots`) |
| Every logged-in role | **my account** (the user is always the one in the token, no id parameter): `GET /api/account` (id, full name, email, phone, role, and the insurance number for patients), `PUT /api/account` (full name and phone, for patients also the insurance number; the **email cannot be changed**), `PUT /api/account/password` (`currentPassword`, `newPassword`; a wrong current password is **400** "The current password is incorrect.", not 401; the new password follows the registration rules and is stored hashed) |
| Patient | `POST appointments`, `GET services/{id}/price-preview` (the amount this patient would pay, same pricing strategy as booking, creates nothing), `GET patients/{id}/appointments`, `POST appointments/{id}/cancel` |
| Doctor | `GET doctors/{id}/profile` (own name and department), `GET/PUT doctors/{id}/auto-schedule`, `PUT doctors/{id}/auto-confirm`, `doctors/{id}/availability` (list, add, bulk, delete), `GET doctors/{id}/appointments`, `PUT staff/{id}/slot-duration`, confirm / reject / cancel (own appointments) |
| ClinicManager | update of their organization; create / update / delete of branches, working hours, departments, services and staff (including activate / deactivate, offered services, `GET staff`), `PUT staff/{id}/slot-duration`, confirm / reject / cancel appointments |
| SystemAdmin | everything a ClinicManager can do, across all organizations, plus **create and delete organizations** and **`POST branches/{id}/managers`** (creating ClinicManager accounts with a temporary password), and the manager list / edit / deactivate / activate endpoints `GET /api/managers`, `PUT managers/{id}`, `POST managers/{id}/deactivate` and `/activate` (a deactivated manager cannot log in: the same 401 as a wrong password) |

Errors use the plain-text style of the rest of the API: **401** when there is no valid token (missing, bad
signature, wrong issuer/audience, expired), **403** when the role is not allowed *or* the resource is not the
caller's.

### Ownership and scope (checked from the ids in the token)

The role decides which endpoint may be called; `AccessGuard` (Application) then checks that the resource is the caller's
and throws `ForbiddenException` (403) if not. `ICurrentUser` (read from the JWT in the Api layer) supplies the ids.

- **Patient**: books only for themselves (the patient id comes from the token; a `patientId` in the body is ignored),
  reads only their own appointment list, cancels only their own appointments.
- **Doctor**: only their own availability (list, add, bulk, delete), slot duration and appointment list; confirms,
  rejects or cancels only appointments assigned to them (never an unassigned diagnostic one).
- **ClinicManager**: only resources of the **organization in their token**: that organization, its branches, working
  hours, departments, services and staff, and the appointments of its branches (confirm, reject, cancel). A staff list
  without a filter shows only their organization's staff. Creating or deleting organizations and creating manager
  accounts is SystemAdmin only.
- **Working hours of a branch** (`POST` / `DELETE branches/{id}/working-hours`): a **ClinicManager may change them for
  their own branch only** (the `branch_id` in the token; another branch of the same organization is 403, and a manager token
  without a branch cannot change any), a SystemAdmin for every branch. Reading them is public. Availability outside the
  branch hours is not offered to patients, so shrinking the hours hides those slots.
- **SystemAdmin** is not tied to an organization and passes every scope check. Its token has no branch or organization.
- Checks run after the resource is loaded, so an unknown id is 404 and someone else's id is 403.

### Demo accounts

The sample data (`SeedCatalog`) has three branches with departments such as İç Hastalıkları (Dahiliye),
Gastroenteroloji, Kardiyoloji, Ortopedi ve Travmatoloji, Göz Hastalıkları, Çocuk Sağlığı ve Hastalıkları, Nöroloji,
Radyoloji and Laboratuvar. Every examination department has at least two doctors with different slot durations, and
**every seeded doctor** has a demo login (`firstname.lastname@sakaryasehir.example` or `@marmarapoliklinik.example`).
In Dahiliye, Kardiyoloji, Çocuk Sağlığı ve Hastalıkları and Göz Hastalıkları (where the branch has them) there are two
examination services, "Genel Muayene" and a cheaper "Kontrol Muayenesi" (about 60 percent of the price): the first doctor
offers both, the second only "Genel Muayene". Ortopedi ve Travmatoloji keeps a single service (auto-select demo). The
technicians are linked to varied diagnostic services (for example in Merkez Radyoloji one does Röntgen and Ultrasonografi,
the other MR; the laboratories have Kan Tahlili, İdrar Tahlili and Tam Kan Sayımı).
The table below lists only a short selection. The seed runs only on an empty database (reset `ClinicDb` to apply changes).

All demo accounts use the password **`Demo123!`** and are given it at startup only if they have no password yet
(`DbInitializer.DemoEmails`). Technicians have no login at all (the seed has two or three per branch, shown on the manager's "Personel" page). The system administrator account is created at
startup if it does not exist.

| Role | Email | Works at |
|---|---|---|
| Patient | `ahmet.celik@example.com` (insured), `fatma.yildirim@example.com` | - |
| Doctor | `hakan.yildiz@sakaryasehir.example`, `elif.sahin@sakaryasehir.example` | Sakarya Şehir Hastanesi - Merkez |
| Doctor | `zeynep.celik@sakaryasehir.example`, `burak.ozturk@sakaryasehir.example` | Sakarya Şehir Hastanesi - Adapazarı Şubesi |
| Doctor | `can.aksoy@marmarapoliklinik.example` | Marmara Poliklinik - Serdivan Kliniği |
| Clinic manager | `ayse.yilmaz@sakaryasehir.example` | Merkez |
| Clinic manager | `mehmet.kara@sakaryasehir.example` | Adapazarı Şubesi |
| Clinic manager | `ali.erdogan@marmarapoliklinik.example` | Serdivan Kliniği |
| System administrator | `admin@clinic.example` | all organizations |

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
- Doctors registered under a department (their specialty is the department name) and the services they provide; health
  technicians registered as staff.
- Doctor availability slots defined; appointments can only be booked inside
  an availability slot.
- Appointment creation performs a conflict check (no double-booking a doctor
  for an overlapping time range).
- Appointment details (service, duration, price, doctor if any, date/time, patient)
  are persisted.
- Appointment approval mechanism (pending -> confirmed/rejected).
- At least three user roles: Patient, Doctor/Staff, Clinic/Hospital Manager.

## Frontend design rules

Applies to everything in `/frontend`. These rules take precedence over the general guidance of the
frontend-design skill: use that skill for craft (spacing, states, accessibility), not to change the direction.
Code, identifiers and comments are English; **all user-facing text is Turkish**.

- **No generic AI aesthetics**: no purple or indigo gradients, no glow or floating effects, no glassmorphism,
  no emoji, no stock illustrations, no placeholder or invented data (show real API data or an honest empty state).
- **Avoid the template look**: no colored circle or tile behind every icon; no row of three identical cards with
  icon, title and blurb; no oversized hero icons; **one accent color only** (teal); hierarchy through size, weight
  and spacing; real data wherever something is shown.
- **Palette**: primary deep slate `#0f172a` (slate-900) and teal `#0f766e` (teal-700), neutral background `#f8fafc`
  (slate-50), crisp borders (`border-slate-200`). Status colors are used consistently everywhere: **pending amber,
  confirmed green, rejected or cancelled red or slate**.
- **Soft surfaces**: cards are `rounded-xl` with a slate-200 border and a light shadow (`shadow-sm`, nothing heavier);
  a very light slate or teal tint is allowed on card headers and selected areas. The shared `Card`, `Table`,
  `ChoiceCard`, `FilterBar` and `Modal` all use this surface, the same on every role's pages.
- **Gradients**: a subtle teal-to-slate gradient is allowed **only in brand areas** (the login/register panel, a page
  header band), never elsewhere and never purple.
- **Icons**: only through **lucide-react** (an icon set, not a component kit) and the shared `Icon` component (18px by
  default, 16 to 20px next to labels, stroke 1.75, `aria-hidden`). An icon always sits next to text that says the same
  thing; it is never the only signal. The logo mark (`LogoMark`, the favicon's cross) stays our own SVG. Empty states
  may use a simple geometric SVG drawn by us.
- **Typography**: one clean font (Inter or the system font), high contrast, hierarchy through font weight and size
  rather than bright colors, tabular numbers (`tabular-nums`) for times and prices.
- **Layout**: staff and admin screens (doctor, manager, admin) stay dense and efficient like Linear or Stripe: clean
  tables, structured detail panels. Patient pages may be warmer and richer; the booking flow is calmer and roomier: a
  step-by-step flow with large touch targets (at least 44px).
- **Slot states**: available is neutral gray, booked is red with the text "Dolu" and struck through, selected is
  green with a check mark. Never rely on color alone.
- **States**: every list has loading, empty and error states in Turkish. Tables turn into card lists on phone width.
- **Formats**: tr-TR dates and 24-hour times in Europe/Istanbul, prices as tr-TR currency with the ₺ symbol.
- **Light theme only. Motion**: short transitions on hover, focus, menus and toasts only; no looping or decorative
  animation (a loading spinner and a loading skeleton are allowed).
- **Libraries**: Tailwind CSS, React Router and lucide-react (icons only). No component kits or other UI libraries
  (no shadcn, MUI and the like); the shared building blocks are our own small components in `frontend/src/components`.

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
