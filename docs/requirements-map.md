# Gereksinim Eşlemesi

Bu belge, ödev dosyasındaki (`docs/OdevDosyasi.docx`, **Seçenek A: Hastane/Klinik Randevu Sistemi**) her gereksinimin
projede nerede karşılandığını ve hangi testlerin kapsadığını gösterir. Sınıf, dosya, uç nokta ve test adları koddaki
adlarla aynıdır. Test yolları `tests/Clinic.Tests/` altındadır. Karşılanmayan veya kısmen karşılanan maddeler
belgenin sonunda açıkça belirtilmiştir.

## 1. Proje konsepti

| Gereksinim | Karşılandığı yer | Testler |
|---|---|---|
| Nesneye dayalı programlama kapsamında Hastane/Klinik Randevu Sistemi (sınıf yapısı, kalıtım, çok biçimlilik, kapsülleme) | `src/Clinic.Domain` (varlıklar), `src/Clinic.Application` (servisler); ayrıntı için "OOP kavramları" bölümü | `Domain/*Tests` sınıfları |
| Hem tek klinik/hastane hem çok şubeli kuruluş desteği | `HealthOrganization` -> `ClinicFacility` (şube, `OrganizationId` zorunlu) -> `Department`; tek şubeli kurum tek `ClinicFacility` ile tanımlanır. Uç noktalar: `GET/POST /api/organizations`, `GET/POST /api/organizations/{organizationId}/branches`, `GET /api/branches` | `Application/OrganizationAndBranchServiceTests`, `Domain/OrganizationAndServiceTests` |
| Platform bağımsız, dil/teknoloji serbest | .NET 10 / ASP.NET Core ve React; katmanlı yapı (`Clinic.Domain`, `Clinic.Application`, `Clinic.Infrastructure`, `Clinic.Api`, `frontend`) | Kısmen, bkz. "Karşılanmayan / kısmen karşılanan" |

## 2. Hastane/Klinik tanımlamaları

| Gereksinim | Karşılandığı yer | Testler |
|---|---|---|
| Hastane/klinikler yetki çerçevesinde tanımlanabilir | Kurum ekleme ve silme yalnızca `SystemAdmin`; kurum güncelleme `ClinicManager` (kendi kurumu). `OrganizationsController`, `AccessGuard`, `[Authorize(Roles = ...)]`. Sayfalar: `frontend/src/pages/admin/OrganizationsPage.tsx` | `Application/ScopeTests` (ör. `Organizations_AreCreatedAndDeletedOnlyByASystemAdministrator`), `Api/AuthorizationGuardTests` |
| Çalışma saatleri tanımlanabilir | `WorkingHour` (gün + açılış/kapanış, yerel saat), `ClinicFacility.AddWorkingHour`, `ClinicFacility.IsOpenDuring`. Uç noktalar: `GET/POST /api/branches/{branchId}/working-hours`, `DELETE /api/branches/{branchId}/working-hours/{workingHourId}`. Sayfa: `frontend/src/pages/manager/WorkingHoursCard.tsx`. Yeni şube varsayılan hafta içi 08:00-17:00 ile açılır (`AddDefaultWorkingHours`) | `Domain/ClinicFacilityTests` (ör. `IsOpenDuring_UsesLocalTime_NotUtc_AtTheClosingBoundary`), `Application/ScopeTests` (`WorkingHours_*`) |
| Bölümler tanımlanabilir | `Department`, `ClinicFacility.AddDepartment`. Uç noktalar: `GET/POST /api/branches/{branchId}/departments`, `GET/PUT/DELETE /api/departments/{departmentId}`. Sayfa: `frontend/src/pages/manager/DepartmentsPage.tsx` | `Application/DepartmentAndServiceCatalogTests` |
| Sağlık hizmetleri; her hizmetin süresi ve ücreti detaylı tanımlanabilir | `Service` (`Name`, `ServiceType` = `Examination` veya `Diagnostic`, `Duration`, `BasePrice`); doğrulama varlığın içinde (ücret 0-99.999.999,99 ve en çok iki ondalık, süre sıfırdan büyük ve en çok 24 saat). Uç noktalar: `GET/POST /api/departments/{departmentId}/services`, `GET/PUT/DELETE /api/services/{serviceId}`. Sayfa: `frontend/src/pages/manager/ServiceFormModal.tsx` | `Domain/OrganizationAndServiceTests`, `Domain/ManagementRulesTests`, `Application/DepartmentAndServiceCatalogTests` |

## 3. Çalışan yönetimi

| Gereksinim | Karşılandığı yer | Testler |
|---|---|---|
| Doktor ve sağlık personeli sisteme kaydedilebilir | `StaffMember` (soyut) -> `Doctor`, `HealthTechnician`. Uç noktalar: `POST /api/departments/{departmentId}/doctors`, `POST /api/departments/{departmentId}/technicians`, `GET /api/staff`, `GET/PUT/DELETE /api/staff/{staffId}`, `POST /api/staff/{staffId}/deactivate` ve `/activate`. Sayfa: `frontend/src/pages/manager/StaffPage.tsx` | `Application/StaffServiceTests`, `Domain/StaffMemberTests` (ör. `HealthTechnician_IsAStaffMemberWithDefaultSlotDuration_AndItsOwnTitle`) |
| Her doktorun uzmanlık alanı tanımlanabilir | Ayrı bir uzmanlık alanı yok: doktorun uzmanlığı bağlı olduğu bölümün adıdır (`StaffMember.DepartmentId` -> `Department.Name`); `ClinicFacilityId` bölümden okunur. `GET doctors/{doctorId}/profile` bölüm adını döndürür | `Domain/StaffMemberTests` (`StaffMember_TakesItsFacilityFromTheDepartment`) |
| Doktorun sunduğu hizmetler tanımlanabilir | `StaffMember.ServicesProvided`, `AddService`, `SetServices`, `OffersService`; doktor yalnızca kendi bölümünün `Examination` hizmetlerini, teknisyen yalnızca `Diagnostic` hizmetlerini sunabilir (`OfferedServiceType`). `PUT /api/staff/{staffId}/services`. Sayfa: `frontend/src/pages/manager/StaffServicesModal.tsx` | `Domain/StaffMemberTests` (`AddService_Throws_WhenServiceBelongsToAnotherDepartment`), `Domain/DoctorTests` (`OffersService_ReturnsTrueOnlyForServicesTheDoctorProvides`), `Application/StaffServiceTests` |
| Doktor uygunluk zaman dilimleri tanımlanır; hastalar yalnızca bu aralıklara randevu alabilir | `DoctorAvailability` (UTC aralık), `StaffMember.AddAvailability` (çakışan aralık reddedilir), `IsAvailableDuring`. Randevu kontrolü `AppointmentService.BuildExaminationAppointmentAsync` içindedir. Uç noktalar: `GET/POST /api/doctors/{doctorId}/availability`, `POST /api/doctors/{doctorId}/availability/bulk`, `DELETE /api/doctors/{doctorId}/availability/{slotId}`; otomatik takvim: `GET/PUT /api/doctors/{doctorId}/auto-schedule` (`DefaultScheduleService`, `DefaultScheduleWorker`). Sayfa: `frontend/src/pages/doctor/AvailabilityPage.tsx` | `Domain/DoctorTests`, `Application/DoctorAvailabilityServiceTests`, `Application/BulkAvailabilityTests`, `Application/DefaultScheduleServiceTests`, `Application/AppointmentServiceTests` (`Examination_Throws_WhenDoctorHasNoAvailabilityForTheTime`) |

## 4. Randevu sistemi

| Gereksinim | Karşılandığı yer | Testler |
|---|---|---|
| Kullanıcılar uygun çalışan ve işlem seçerek randevu oluşturabilir | `POST /api/appointments` (`AppointmentsController`, `AppointmentService.CreateAppointmentAsync`). Muayenede doktor + hizmet + slot, tetkikte hizmet + tarih/saat seçilir. Slot listeleri: `GET /api/doctors/{doctorId}/slots`, `GET /api/services/{serviceId}/slots` (`SlotService`, `SlotGrid`); doktor listesi: `GET /api/departments/{departmentId}/doctors?serviceId=`. Sayfa: `frontend/src/pages/patient/BookingPage.tsx` | `Application/AppointmentServiceTests`, `Application/SlotServiceTests`, `Domain/SlotGridTests` |
| Randevu oluşturulurken çakışma kontrolü yapılır | Doktor için `IAppointmentRepository.HasConflictAsync`, tetkikte aynı hizmet için `HasServiceConflictAsync`; `Appointment.OverlapsWith`; aktif = iptal edilmemiş ve reddedilmemiş. İstisnalar: `AppointmentConflictException`, `ServiceSlotConflictException`. Ek denetimler: şube çalışma saatleri (`OutsideWorkingHoursException`), slot ızgarası (`SlotNotAlignedException`), geçmiş zaman (`PastBookingException`), 14 günlük pencere (`OutsideBookingWindowException`), pasif doktor (`StaffInactiveException`) | `Application/AppointmentServiceTests` (ör. `Examination_Throws_WhenDoctorIsAlreadyBookedInThatSlot`, `Diagnostic_Throws_WhenSameServiceIsAlreadyBookedInThatSlot`, `Examination_SlotIsFreeAgain_AfterTheAppointmentIsCancelled`), `Domain/AppointmentTests` (`OverlapsWith_DetectsOverlapCorrectly`) |
| Randevu detayları (işlem, süre, ücret, çalışan, tarih/saat) kaydedilir | `Appointment`: `ServiceId`, `ScheduledStartUtc`/`ScheduledEndUtc`, `Price`, `StaffMemberId`, `PatientId`, `Status`. Ücret `IPricingStrategy` ile hesaplanır. Listeler: `GET /api/patients/{patientId}/appointments`, `GET /api/doctors/{doctorId}/appointments` (hasta, personel, şube, hizmet adları, zaman, ücret, durum); fiyat önizleme: `GET /api/services/{serviceId}/price-preview`. Sayfalar: `patient/MyAppointmentsPage.tsx`, `doctor/DoctorAppointmentsPage.tsx` | `Application/AppointmentServiceTests` (`Examination_OccupiesExactlyOneSlot_OfTheDoctorsSlotDuration`, `PatientAppointments_ListServiceTimePriceAndStatus_ByStartTime_IncludingUnassignedOnes`), `Domain/StrategyTests` |
| Randevu için onay mekanizması vardır | Başlangıç durumu `IConfirmationPolicy` ile belirlenir (`DoctorPreferenceConfirmationPolicy`); `Doctor.AutoConfirmAppointments`. `Appointment.Confirm()`, `Reject()`, `Cancel()` durum geçişlerini denetler. Uç noktalar: `POST /api/appointments/{id}/confirm`, `/reject`, `/cancel`; `PUT /api/doctors/{doctorId}/auto-confirm`. Sayfa: `frontend/src/pages/doctor/ApprovalCard.tsx` | `Domain/StrategyTests` (`DoctorPreferencePolicy_*`), `Domain/AppointmentTests` (`Confirm_Throws_WhenNotPending`), `Application/AppointmentServiceTests` |

## 5. Kullanıcı rolleri

| Gereksinim | Karşılandığı yer | Testler |
|---|---|---|
| En az üç rol: Hasta, Doktor/Sağlık Personeli, Hastane/Klinik Yöneticisi | Roller: `Patient`, `Doctor`, `ClinicManager` ve ek olarak `SystemAdmin` (`HealthTechnician` yalnızca personel kaydıdır, girişi yoktur). Sabitler `RoleNames`/`AppRoles`; JWT: `POST /api/auth/register`, `POST /api/auth/login` (`AuthService`, `JwtTokenGenerator`). Sahiplik ve kapsam: `AccessGuard`, `ICurrentUser`. Eski token denetimi: `TokenUserValidation`. Rol sayfaları: `frontend/src/pages/patient`, `doctor`, `manager`, `admin`, `account` | `Application/AuthTests`, `Application/ScopeTests`, `Application/TokenUserValidationTests`, `Api/AuthorizationGuardTests` (her eylemin yetki özniteliği ve rol eşlemesi), `Application/AccountServiceTests` |

## OOP kavramları ve koddaki örnekleri

| Kavram | Somut örnek | Test |
|---|---|---|
| **Kalıtım** | `Entity` -> `User` -> `Patient`, `ClinicManager`, `SystemAdmin`, `StaffMember` -> `Doctor`, `HealthTechnician`. Ortak kimlik ve iletişim alanları `User` içinde, çalışma saati/uygunluk/hizmet mantığı `StaffMember` içindedir; alt sınıflar yalnızca farklarını ekler (`Doctor.AutoConfirmAppointments`, `HealthTechnician.Title`, `Patient.InsuranceNumber`). Tüm ağaç EF Core TPH ile tek `Users` tablosunda (`UserType` ayırt edici, `ClinicDbContext`) | `Domain/StaffMemberTests` |
| **Çok biçimlilik** | `User.RoleName` soyut özelliği her alt sınıfta farklı değer döndürür ("Patient", "Doctor", "HealthTechnician", ...); `User.CanSignIn` sanal özelliği `StaffMember` ve `ClinicManager` için `IsActive`, `HealthTechnician` için `false` olarak ezilir; `StaffMember.OfferedServiceType` doktorda `Examination`, teknisyende `Diagnostic` döndürür. Strateji arayüzleri de çok biçimlidir: `IPricingStrategy` ve `IConfirmationPolicy` çağrıları, kayıtlı uygulamaya göre farklı sonuç verir | `Domain/StaffMemberTests` (`RoleName_IsResolvedPolymorphically_AcrossTheHierarchy`), `Domain/StrategyTests` |
| **Kapsülleme** | Durum değiştiren özellikler `private set`; geçersiz durum yaratılamaz: doğrulamalı yapıcılar, `StaffMember.SetSlotDuration` (yalnızca 10, 15, 20, 30, 60), `Appointment.Confirm()/Reject()/Cancel()` (yanlış durumda `InvalidOperationException`), `ClinicFacility.AddWorkingHour` (çakışmayı reddeder), `Service.Update`. Koleksiyonlar özel listedir ve `IReadOnlyCollection` olarak açılır (`ClinicFacility.Departments`, `StaffMember.ServicesProvided`, `StaffMember.AvailabilitySlots`) | `Domain/StaffMemberTests` (`SlotDuration_RejectsValuesThatAreNotAllowed`), `Domain/AppointmentTests`, `Domain/ManagementRulesTests` |
| **Soyutlama / arayüzler** | Soyut sınıflar `Entity`, `User`, `StaffMember`; arayüzler `IPricingStrategy`, `IConfirmationPolicy`, `IRepository<T>`, `IAppointmentRepository`, `IDoctorRepository`, `IUnitOfWork`, `IAppointmentService`, `ICurrentUser`, `IUserStatusChecker`. Application katmanı yalnızca arayüzleri bilir; uygulamalar `Clinic.Infrastructure` içindedir ve `DependencyInjection` ile bağlanır | `Application/Fakes.cs` (sahte repository'lerle servis testleri) |
| **Strategy deseni** | `IPricingStrategy` -> `StandardPricingStrategy`, `InsuranceDiscountPricingStrategy` (%20 sigorta indirimi); `IConfirmationPolicy` -> `AutoConfirmationPolicy`, `ApprovalRequiredConfirmationPolicy`, `DoctorPreferenceConfirmationPolicy`. `AppointmentService` hiçbir somut sınıfı bilmez; seçim `Clinic.Infrastructure/DependencyInjection.cs` içinde tek satırdır | `Domain/StrategyTests` |
| **Repository deseni (+ Unit of Work)** | Genel `IRepository<T>` ve odaklı arayüzler (`IAppointmentRepository.HasConflictAsync`, `IDoctorRepository`, `IStaffMemberRepository` vb.); uygulamalar `Clinic.Infrastructure/Repositories` altında EF Core ile (`Repository<T>`, `AppointmentRepository`); kayıt işlemi `IUnitOfWork` / `UnitOfWork` ile | `Application/*Tests` (sahte repository'ler) |
| **Bağımlılık enjeksiyonu** | Yapıcıdan enjeksiyon (`AppointmentService(...)`), `Clinic.Infrastructure/DependencyInjection.cs` içinde kayıt, `TimeProvider` ile testte sabitlenebilen saat | `Application/AppointmentServiceTests` |

## 6. Teslimat ve sunum

| Gereksinim | Durum | Not |
|---|---|---|
| Proje GitHub'da paylaşılır | Depo git ile tutulur (`.gitignore`, `LICENSE`, `README.md`) | Uzak depoya yüklemeyi proje sahibi yapar |
| Sabis sistemine yükleme | Bu depodan bağımsız bir adımdır | Proje sahibi tarafından yapılacak |
| Sunum | Demo senaryosu `README.md` içindedir | Sunum tarihlerine göre |
| GitHub'da farklı günlerde en az 10 commit | **Karşılanmadı** | Bkz. aşağıda |

## Karşılanmayan / kısmen karşılanan maddeler

**Karşılanmayan**

- **En az 10 farklı günde commit:** Bu belge yazıldığında depoda 47 commit vardır, ancak bunlar yalnızca **4 farklı güne**
  yayılmıştır (`git log --format=%ad --date=short` çıktısındaki ayrı tarih sayısı). "10 commit, farklı günlerde" şartı
  geriye dönük olarak sağlanamaz; yeni commit'ler farklı günlere yayılsa bile toplam gün sayısı ancak ilerleyen
  günlerde artar.

**Kısmen karşılanan**

- **Platform bağımsızlığı:** Kod .NET 10 ve React ile her platformda derlenir, ancak varsayılan veritabanı SQL Server
  LocalDB'dir (yalnızca Windows). Başka platformda `ConnectionStrings` ayarının farklı bir SQL Server örneğine
  yönlendirilmesi gerekir.
- **Doktorun uzmanlık alanı:** Ayrı bir alan olarak tutulmaz; bölüm adından türetilir. Bir doktorun birden fazla uzmanlığı
  veya bölümden bağımsız bir uzmanlığı tanımlanamaz; doktor başka bir bölüme taşınamaz.
- **Sağlık personeli:** `HealthTechnician` kaydedilebilir ve tetkik hizmetlerine bağlanabilir, ancak randevu, slot ve
  uygunluk mantığında kullanılmaz (tetkik randevusu personelsiz kaydedilir) ve teknisyenin giriş ekranı yoktur.
- **Çakışma kontrolü:** Kontrol uygulama katmanında yapılır; aynı slota aynı anda gelen iki isteğe karşı eşzamanlılık
  koruması (kilit, eşzamanlılık belirteci veya benzersiz kısıt) yoktur.
- **Uygunluk modeli:** Haftalık şablon ve istisna günleri yerine tarih aralığı modeli kullanılır; klinik yöneticisi doktorun
  uygunluğunu düzenleyemez.
- **Hesap yönetimi:** İlk girişte şifre değiştirmeye zorlama, e-posta doğrulama ve "şifremi unuttum" akışı yoktur.
