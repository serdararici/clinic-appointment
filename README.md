# Clinic Appointment System

Hospital / clinic appointment platform: organizations with branches, departments, services, doctors, availability and
patient appointments. ASP.NET Core Web API (.NET 10) with SQL Server LocalDB, and a React (Vite + TypeScript) frontend.
See `CLAUDE.md` for the architecture, domain model, booking rules and access rules.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (installed with Visual Studio, or SQL Server Express with the LocalDB option)
- [Node.js](https://nodejs.org/) (only for the frontend)

## Run the API

```
dotnet run --project src/Clinic.Api --launch-profile http
```

The API listens on `http://localhost:5268`. On startup it creates the `ClinicDb` LocalDB database, applies the
migrations and seeds Turkish demo data, so no manual database step is needed.

Doctors work an automatic default schedule (weekdays 09:00-12:00 and 13:00-16:00, Turkey time), which the API keeps
filled for the next 14 days: once at startup and then every 6 hours (`DefaultSchedule:IntervalHours`). Patients can only
book from today up to 14 days ahead. A doctor can switch the automatic schedule off on the availability page.

In Development, an interactive API reference is available at **http://localhost:5268/scalar**. Log in with
`POST /api/auth/login`, copy the `token` from the response and paste it into the Bearer field to try protected endpoints.

The frontend (when you need it) runs with `npm install` and `npm run dev` inside `frontend/` on
`http://localhost:5173`, which is the origin the API allows through CORS (`Cors:AllowedOrigins`).

## Run the tests

```
dotnet build
dotnet test
```

## Demo accounts

All demo accounts use the password **`Demo123!`**.

| Role | Email |
|---|---|
| System administrator | `admin@clinic.example` |
| Clinic manager | `ayse.yilmaz@sakaryasehir.example` (Merkez), `mehmet.kara@sakaryasehir.example` (Adapazarı Şubesi), `ali.erdogan@marmarapoliklinik.example` (Serdivan Kliniği) |
| Doctor | `hakan.yildiz@sakaryasehir.example`, `elif.sahin@sakaryasehir.example`, `zeynep.celik@sakaryasehir.example`, `burak.ozturk@sakaryasehir.example`, `can.aksoy@marmarapoliklinik.example` |
| Patient | `ahmet.celik@example.com`, `fatma.yildirim@example.com` |

### Demo of the appointment approval flow

Every doctor decides whether new appointments are confirmed at once (the default) or wait for their approval. In the
seed data exactly one doctor approves appointments himself: **`kemal.aslan@sakaryasehir.example`** (Dr. Kemal Aslan,
Kardiyoloji, Merkez). To see the flow: log in as a patient and book a Kardiyoloji examination with Dr. Kemal Aslan (it
shows "Onay bekliyor"), then log in as him and open **Randevular > Bekleyen** to "Onayla" or "Reddet" it; the patient
then sees "Onaylandı" or "Reddedildi". Doctors change the setting on their Randevular page ("Randevu onayı").
The seed only runs on an empty database, so an existing `ClinicDb` must be dropped to apply this change.

## Production note

The committed `appsettings.json` contains a **development-only** JWT signing key. In any real deployment override it
with a long random secret through the environment variable **`Jwt__Key`** (at least 32 characters; the app refuses to
start otherwise), and change or remove the demo accounts.
