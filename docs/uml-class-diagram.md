# UML Sınıf Diyagramları

Bu belgedeki diyagramlar [Mermaid](https://mermaid.js.org/) ile yazılmıştır; GitHub, VS Code (Markdown önizleme) ve
<https://mermaid.live> üzerinde görüntülenir. Sınıf, üye ve dosya adları koddaki adlarla aynıdır; kolay okunması için
yalnızca önemli üyeler gösterilmiştir.

## 1. Alan modeli (Clinic.Domain)

Tüm varlıklar `Entity` soyut sınıfından `Id` alır. Kullanıcılar `User` soyut sınıfı altında bir kalıtım ağacı oluşturur:
`Patient`, `ClinicManager`, `SystemAdmin` ve soyut `StaffMember` (onun alt sınıfları `Doctor` ve `HealthTechnician`).
Kurum yapısı `HealthOrganization` -> `ClinicFacility` (şube) -> `Department` -> `Service` zinciridir; `Appointment`,
hasta, hizmet ve (muayenede) personel kaydına kimlikle bağlanır, tetkik randevusunda `StaffMemberId` boştur.

```mermaid
classDiagram
    direction TB

    class Entity {
        <<abstract>>
        +Guid Id
    }

    class User {
        <<abstract>>
        +string FullName
        +string Email
        +string PhoneNumber
        +string PasswordHash
        +DateTime CreatedAtUtc
        +bool CanSignIn
        +string RoleName
        +SetPasswordHash(string passwordHash)
        +Rename(string fullName)
        +ChangePhoneNumber(string phoneNumber)
    }

    class Patient {
        +DateOnly DateOfBirth
        +string InsuranceNumber
        +bool HasInsurance
        +ChangeInsuranceNumber(string insuranceNumber)
    }

    class ClinicManager {
        +Guid ClinicFacilityId
        +bool IsActive
        +Activate()
        +Deactivate()
    }

    class SystemAdmin

    class StaffMember {
        <<abstract>>
        +Guid ClinicFacilityId
        +Guid DepartmentId
        +int SlotDurationMinutes
        +bool IsActive
        +ServiceType OfferedServiceType
        +SetSlotDuration(int minutes)
        +Activate()
        +Deactivate()
        +AddService(Service service)
        +SetServices(services)
        +OffersService(Guid serviceId) bool
        +AddAvailability(DateTime startUtc, DateTime endUtc) DoctorAvailability
        +RemoveAvailability(Guid slotId) DoctorAvailability
        +IsAvailableDuring(DateTime start, DateTime end) bool
    }

    class Doctor {
        +bool AutoScheduleEnabled
        +bool AutoConfirmAppointments
        +SetAutoSchedule(bool enabled)
        +SetAutoConfirm(bool enabled)
    }

    class HealthTechnician {
        +string Title
        +ChangeTitle(string title)
    }

    class HealthOrganization {
        +string Name
        +Rename(string name)
    }

    class ClinicFacility {
        +Guid OrganizationId
        +string Name
        +string Address
        +UpdateDetails(string name, string address)
        +AddDepartment(string name) Department
        +AddDefaultWorkingHours()
        +AddWorkingHour(DayOfWeek dayOfWeek, TimeOnly opensAt, TimeOnly closesAt) WorkingHour
        +RemoveWorkingHour(Guid workingHourId) WorkingHour
        +IsOpenDuring(DateTime startUtc, DateTime endUtc) bool
    }

    class WorkingHour {
        +Guid ClinicFacilityId
        +DayOfWeek DayOfWeek
        +TimeOnly OpensAt
        +TimeOnly ClosesAt
        +Contains(DayOfWeek day, TimeOnly time) bool
        +Overlaps(DayOfWeek day, TimeOnly opensAt, TimeOnly closesAt) bool
    }

    class Department {
        +Guid ClinicFacilityId
        +string Name
        +Rename(string name)
        +AddService(string name, ServiceType serviceType, TimeSpan duration, decimal price) Service
    }

    class Service {
        +Guid DepartmentId
        +string Name
        +ServiceType ServiceType
        +TimeSpan Duration
        +decimal BasePrice
        +Update(string name, ServiceType serviceType, TimeSpan duration, decimal basePrice)
    }

    class DoctorAvailability {
        +Guid StaffMemberId
        +DateTime StartUtc
        +DateTime EndUtc
        +Overlaps(DateTime start, DateTime end) bool
        +Contains(DateTime start, DateTime end) bool
    }

    class Appointment {
        +Guid PatientId
        +Guid StaffMemberId
        +Guid ServiceId
        +DateTime ScheduledStartUtc
        +DateTime ScheduledEndUtc
        +decimal Price
        +AppointmentStatus Status
        +bool IsActive
        +OverlapsWith(DateTime start, DateTime end) bool
        +Confirm()
        +Reject()
        +Cancel()
    }

    class ServiceType {
        <<enumeration>>
        Examination
        Diagnostic
    }

    class AppointmentStatus {
        <<enumeration>>
        Pending
        Confirmed
        Rejected
        Cancelled
    }

    Entity <|-- User
    User <|-- Patient
    User <|-- ClinicManager
    User <|-- SystemAdmin
    User <|-- StaffMember
    StaffMember <|-- Doctor
    StaffMember <|-- HealthTechnician

    Entity <|-- HealthOrganization
    Entity <|-- ClinicFacility
    Entity <|-- WorkingHour
    Entity <|-- Department
    Entity <|-- Service
    Entity <|-- DoctorAvailability
    Entity <|-- Appointment

    HealthOrganization "1" --> "*" ClinicFacility : OrganizationId
    ClinicFacility "1" *-- "*" WorkingHour : WorkingHours
    ClinicFacility "1" *-- "*" Department : Departments
    Department "1" *-- "*" Service : Services
    StaffMember "*" --> "1" Department : DepartmentId
    StaffMember "*" --> "*" Service : ServicesProvided
    StaffMember "1" *-- "*" DoctorAvailability : AvailabilitySlots
    ClinicManager "*" --> "1" ClinicFacility : ClinicFacilityId
    Appointment "*" --> "1" Patient : PatientId
    Appointment "*" --> "0..1" StaffMember : StaffMemberId
    Appointment "*" --> "1" Service : ServiceId
    Service --> ServiceType
    Appointment --> AppointmentStatus
```

Notlar: `User.RoleName` soyut bir özelliktir ve her alt sınıf kendi değerini döndürür (çok biçimlilik). `StaffMember.OfferedServiceType`
da soyuttur: `Doctor` için `Examination`, `HealthTechnician` için `Diagnostic`. `Appointment.StaffMemberId` tetkik
randevusunda boştur (`Guid?`). Tüm kullanıcı sınıfları veritabanında tek `Users` tablosunda, `UserType` ayırt edicisiyle saklanır.

## 2. Strateji deseni (fiyat ve onay kuralı)

`AppointmentService`, fiyatı `IPricingStrategy` ile, ilk randevu durumunu `IConfirmationPolicy` ile belirler; somut sınıfları
bilmez, yalnızca arayüzlere bağlıdır. Hangi uygulamanın kullanılacağı `Clinic.Infrastructure/DependencyInjection.cs` içinde
tek satırla seçilir (şu an `InsuranceDiscountPricingStrategy` ve `DoctorPreferenceConfirmationPolicy`); servis kodu değişmez.

```mermaid
classDiagram
    direction LR

    class IPricingStrategy {
        <<interface>>
        +CalculatePrice(Service service, Patient patient) decimal
    }

    class StandardPricingStrategy {
        +CalculatePrice(Service service, Patient patient) decimal
    }

    class InsuranceDiscountPricingStrategy {
        +CalculatePrice(Service service, Patient patient) decimal
    }

    class IConfirmationPolicy {
        <<interface>>
        +DetermineInitialStatus(Doctor doctor) AppointmentStatus
    }

    class AutoConfirmationPolicy {
        +DetermineInitialStatus(Doctor doctor) AppointmentStatus
    }

    class ApprovalRequiredConfirmationPolicy {
        +DetermineInitialStatus(Doctor doctor) AppointmentStatus
    }

    class DoctorPreferenceConfirmationPolicy {
        +DetermineInitialStatus(Doctor doctor) AppointmentStatus
    }

    class IAppointmentService {
        <<interface>>
        +CreateAppointmentAsync(request) AppointmentDto
        +PreviewPriceAsync(Guid serviceId) PricePreviewDto
        +ConfirmAppointmentAsync(Guid appointmentId)
        +RejectAppointmentAsync(Guid appointmentId)
        +CancelAppointmentAsync(Guid appointmentId)
    }

    class AppointmentService {
        -IPricingStrategy pricingStrategy
        -IConfirmationPolicy confirmationPolicy
        -IAppointmentRepository appointmentRepository
        -IUnitOfWork unitOfWork
        -AccessGuard guard
    }

    IPricingStrategy <|.. StandardPricingStrategy
    IPricingStrategy <|.. InsuranceDiscountPricingStrategy
    IConfirmationPolicy <|.. AutoConfirmationPolicy
    IConfirmationPolicy <|.. ApprovalRequiredConfirmationPolicy
    IConfirmationPolicy <|.. DoctorPreferenceConfirmationPolicy
    DoctorPreferenceConfirmationPolicy --> AutoConfirmationPolicy : doktor otomatik onaylıyorsa
    DoctorPreferenceConfirmationPolicy --> ApprovalRequiredConfirmationPolicy : doktor onay istiyorsa
    IAppointmentService <|.. AppointmentService
    AppointmentService --> IPricingStrategy
    AppointmentService --> IConfirmationPolicy
```

`InsuranceDiscountPricingStrategy`, sigorta numarası olan hastaya fiyatın %20 indirimlisini uygular; `StandardPricingStrategy`
her zaman `BasePrice` döndürür. `DoctorPreferenceConfirmationPolicy`, doktorsuz (tetkik) veya otomatik onaylayan doktorlu
randevuyu `Confirmed`, onay isteyen doktorlu randevuyu `Pending` başlatır; kararı iki basit politikaya yönlendirir.

## 3. Katmanlar ve bağımlılıklar

Oklar "bağımlıdır / başvurur" yönünü gösterir (proje referansları `.csproj` dosyalarındaki `ProjectReference` kayıtlarıdır).
`Clinic.Domain` hiçbir katmana bağımlı değildir; `Clinic.Api` hem `Clinic.Infrastructure` hem `Clinic.Application` hem de
`Clinic.Domain` projesine başvurur.

```mermaid
flowchart LR
    Frontend["frontend<br/>React + TypeScript"]
    Api["Clinic.Api<br/>Controller'lar, DI, JWT, ApiExceptionHandler"]
    Infrastructure["Clinic.Infrastructure<br/>ClinicDbContext, Repository'ler, Seed, JwtTokenGenerator"]
    Application["Clinic.Application<br/>AppointmentService, SlotService, StaffService, AccessGuard"]
    Domain["Clinic.Domain<br/>Varlıklar, Strategy arayüzleri, SlotGrid, BookingRules"]
    Tests["Clinic.Tests<br/>xUnit"]

    Frontend -. "HTTP / JSON" .-> Api
    Api --> Infrastructure
    Api --> Application
    Api --> Domain
    Infrastructure --> Application
    Infrastructure --> Domain
    Application --> Domain
    Tests --> Api
    Tests --> Infrastructure
    Tests --> Application
    Tests --> Domain
```

Bağımlılık yönü içe doğrudur: iş kuralları `Clinic.Domain` ve `Clinic.Application` içinde kalır, EF Core ve JWT gibi
ayrıntılar `Clinic.Infrastructure` içindedir. Application katmanı repository'leri yalnızca `Clinic.Domain.Repositories`
altındaki arayüzler (`IAppointmentRepository`, `IUnitOfWork` vb.) üzerinden kullanır; uygulamalar Infrastructure'dadır.

## Doğrulama notu

Bu ortamda Mermaid komut satırı aracı (`mmdc`) kurulu olmadığından diyagramlar otomatik olarak çizdirilmedi; söz dizimi
elle gözden geçirildi (`classDiagram` ve `flowchart` yapıları, çok kardinalite etiketleri, tırnaklı düğüm etiketleri).
Görüntülenmiyorsa <https://mermaid.live> üzerinde açıp deneyebilirsiniz.
