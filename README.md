# Hastane/Klinik Randevu Sistemi

## Proje tanımı

Bu proje, ders ödevinin **Seçenek A** konusudur: Hastane/Klinik Randevu Sistemi (ödev metni: `docs/OdevDosyasi.docx`).
Sistem, tek şubeli veya çok şubeli sağlık kuruluşlarının şubelerini, bölümlerini, hizmetlerini, personelini, doktor
uygunluklarını ve hasta randevularını yönetmesini sağlar. Hasta bir bölüm ve hizmet seçer, muayene için doktor ve saat,
tetkik için tarih ve saat belirler; sistem çakışmayı, çalışma saatlerini ve randevu penceresini denetler, fiyatı
hesaplar ve randevuyu onay kuralına göre kaydeder.

Proje nesneye dayalı programlama ilkelerini (kalıtım, çok biçimlilik, kapsülleme, soyutlama) ve tasarım desenlerini
(Strategy, Repository) göstermek için tasarlanmıştır. Ayrıntılı belgeler:

- `docs/uml-class-diagram.md`: UML sınıf diyagramları (Mermaid)
- `docs/requirements-map.md`: ödev gereksinimlerinin kodda nerede karşılandığı ve hangi testlerin kapsadığı
- `CLAUDE.md`: mimari, alan modeli ve kuralların ayrıntılı geliştirici notları (İngilizce)

## Özellikler

### Hasta
- Kayıt olma ve giriş yapma; **Hesabım** sayfasında ad, telefon, sigorta numarası ve şifre değiştirme.
- Randevu alma: kurum, şube, bölüm, hizmet, (muayenede) doktor, tarih ve saat adımları. Bölümde tek hizmet varsa otomatik seçilir.
- Özet ekranında hastanın ödeyeceği tutar (sigortalıysa %20 indirimli) randevu alınmadan önce görünür.
- **Randevularım**: yaklaşan ve geçmiş randevular, durumları (Onay bekliyor, Onaylandı, Reddedildi, İptal edildi) ve randevu iptali.

### Doktor
- Kendi uygunluk aralıklarını ekler, siler ve toplu ekler; randevu süresini (10, 15, 20, 30, 60 dakika) seçer.
- Otomatik çalışma takvimini açıp kapatır (varsayılan: hafta içi 09:00-12:00 ve 13:00-16:00).
- Randevularını görür; onay gerektiren ayarda bekleyen randevuları onaylar veya reddeder, kendi randevusunu iptal eder.
- Randevu onay ayarını değiştirir (otomatik onay veya kendi onayı).

### Klinik Yöneticisi
- Kendi kurumunun şubelerini, çalışma saatlerini, bölümlerini ve hizmetlerini (tür, süre, ücret) yönetir.
- Doktor ve sağlık teknisyeni ekler, düzenler, pasife alır, sunduğu hizmetleri ve randevu süresini belirler.
- Kurumunun şubelerindeki randevuları onaylar, reddeder veya iptal eder.

### Sistem Yöneticisi
- Tüm kurumlarda klinik yöneticisinin yapabildiklerini yapar.
- Kurum ekler ve siler; şubelere klinik yöneticisi hesabı açar, yöneticileri düzenler, pasife alır ve etkinleştirir.

Sağlık teknisyenleri yalnızca personel kaydıdır: yönetici onları ekler ve tetkik hizmetlerine bağlar, ancak teknisyenin
giriş yapması mümkün değildir (`User.CanSignIn` teknisyen için `false`).

## Teknolojiler

- **Arka uç:** C#, .NET 10, ASP.NET Core Web API, Entity Framework Core 10, SQL Server LocalDB
- **Kimlik doğrulama:** JWT (8 saat), `PasswordHasher` ile tuzlanmış şifre özeti, rol tabanlı yetkilendirme
- **Ön yüz:** React 19, Vite 7, TypeScript, Tailwind CSS 4, React Router (arayüz metinleri Türkçe)
- **Test:** xUnit (yalnızca `Assert` çağrıları)
- **API dokümantasyonu:** Geliştirme ortamında Scalar (`/scalar`)

## Mimari

Çözüm dört katmandan oluşur; bağımlılık yönü her zaman içe doğrudur
(`Api -> Infrastructure -> Application -> Domain`; `Clinic.Api` ayrıca Application ve Domain projelerine de başvurur).

```
Clinic.slnx
src/
  Clinic.Domain/           Varlıklar, enum'lar, strateji arayüzleri, slot ve kural sınıfları, repository arayüzleri
  Clinic.Application/      Uygulama servisleri (AppointmentService vb.), DTO'lar, AccessGuard, istisnalar
  Clinic.Infrastructure/   EF Core (ClinicDbContext, migration'lar), repository'ler, JWT, tohum veri (Seed)
  Clinic.Api/              Controller'lar, DI kaydı, JWT doğrulama, hata yönetimi (ApiExceptionHandler)
tests/
  Clinic.Tests/            xUnit testleri (Domain, Application, Api, Infrastructure)
frontend/                  Vite + React + TypeScript istemcisi
docs/                      Ödev dosyası, UML diyagramları, gereksinim eşlemesi
```

**Clinic.Domain**, başka katmana bağımlı değildir. `User` soyut sınıfı ve alt sınıfları (`Patient`, `ClinicManager`,
`SystemAdmin`, `StaffMember` -> `Doctor`, `HealthTechnician`), kurum yapısı (`HealthOrganization`, `ClinicFacility`,
`Department`, `Service`, `WorkingHour`), `Appointment`, `DoctorAvailability`, strateji arayüzleri (`IPricingStrategy`,
`IConfirmationPolicy`) ve zaman kuralları (`SlotGrid`, `BookingRules`, `DefaultSchedule`, `ClinicTimeZone`) buradadır.

**Clinic.Application**, kullanım senaryolarını yürütür: `AppointmentService`, `SlotService`, `StaffService`,
`DoctorAvailabilityService`, `DefaultScheduleService`, `AuthService` ve yönetim servisleri. Sahiplik ve kapsam
denetimini `AccessGuard` ve `ICurrentUser` yapar; hatalar istisna olarak fırlatılır.

**Clinic.Infrastructure**, EF Core ile veri erişimini sağlar: `ClinicDbContext` (kullanıcı hiyerarşisi tek `Users`
tablosunda, `UserType` ayırt edicisiyle), repository uygulamaları, `JwtTokenGenerator` ve tohum veri
(`DbInitializer`, `SeedCatalog`, `DemoAppointments`). Uygulama açılışında migration'lar otomatik uygulanır.

**Clinic.Api**, HTTP katmanıdır: controller'lar her eylemde `[AllowAnonymous]` veya `[Authorize(Roles = ...)]` taşır,
`ApiExceptionHandler` istisnaları 400/404/409 yanıtlarına çevirir, `DefaultScheduleWorker` varsayılan çalışma
takvimini açılışta ve 6 saatte bir doldurur.

**frontend**, rol bazlı sayfalardan oluşur (hasta, doktor, yönetici, sistem yöneticisi, hesabım); Türkçe arayüz,
Tailwind ile kendi bileşenleri (`frontend/src/components`).

## Kurulum ve çalıştırma

### Önkoşullar
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (Visual Studio ile veya SQL Server Express'in LocalDB seçeneğiyle gelir)
- [Node.js](https://nodejs.org/) (yalnızca ön yüz için)

### API
```
dotnet run --project src/Clinic.Api --launch-profile http
```
API `http://localhost:5268` adresinde dinler. Açılışta `ClinicDb` LocalDB veritabanı oluşturulur, migration'lar
uygulanır ve Türkçe örnek veri yüklenir; elle bir veritabanı adımı gerekmez. Geliştirme ortamında API başvuru sayfası
`http://localhost:5268/scalar` adresindedir (`POST /api/auth/login` ile token alıp Bearer alanına yapıştırın).

### Ön yüz
```
cd frontend
npm install
npm run dev
```
Ön yüz `http://localhost:5173` adresinde çalışır (API bu adrese CORS izni verir: `Cors:AllowedOrigins`).
Üretim derlemesi için `npm run build`, kod denetimi için `npm run lint` kullanılır.

### Testler
```
dotnet build
dotnet test
```

### Veritabanını sıfırlama
Tohum veri yalnızca boş veritabanında yüklenir. Örnek veri değiştiğinde veya demo öncesinde: API'yi durdurun,
```
dotnet ef database drop --force --project src/Clinic.Infrastructure --startup-project src/Clinic.Api
```
komutunu çalıştırın ve API'yi yeniden başlatın (migration ve tohum veri otomatik yüklenir).

### Üretim notu
`appsettings.json` içindeki JWT anahtarı yalnızca geliştirme içindir. Gerçek bir dağıtımda `Jwt__Key` ortam
değişkeniyle en az 32 karakterlik rastgele bir anahtar verilmelidir (kısa veya eksik anahtarla uygulama başlamaz);
demo hesapları değiştirilmeli veya kaldırılmalıdır.

## Demo hesapları ve demo senaryosu

Tüm demo hesaplarının şifresi **`Demo123!`**.

| Rol | E-posta |
|---|---|
| Sistem yöneticisi | `admin@clinic.example` |
| Klinik yöneticisi | `ayse.yilmaz@sakaryasehir.example` (Merkez), `mehmet.kara@sakaryasehir.example` (Adapazarı Şubesi), `ali.erdogan@marmarapoliklinik.example` (Serdivan Kliniği) |
| Doktor | `hakan.yildiz@sakaryasehir.example` (Dahiliye), `kemal.aslan@sakaryasehir.example` (Kardiyoloji, onay gerekir), `selin.aydin@sakaryasehir.example` (Gastroenteroloji), `can.aksoy@marmarapoliklinik.example` (Serdivan); örnek verideki her doktorun `ad.soyad@sakaryasehir.example` veya `@marmarapoliklinik.example` biçiminde hesabı vardır |
| Hasta | `ahmet.celik@example.com` (sigortalı), `elif.demir@example.com` (sigortasız), `fatma.yildirim@example.com` |

Veritabanı sıfırlandığında randevular çalıştırma gününe göre yeniden oluşturulur (geçmiş, **bugün** ve önümüzdeki
14 gün içindeki hafta içi günler; hafta sonu çalıştırılırsa bugün için randevu eklenmez). Merkez'de İç Hastalıkları
(Dr. Hakan Yıldız, Dr. Elif Şahin, Dr. Ayhan Demirci) ve Çocuk Sağlığı ve Hastalıkları (Dr. Tuğba Yalçın,
Dr. Serkan Bulut, Dr. Nilüfer Kaplan) bölümlerinde üçer doktor vardır; diğer bölümlerde ikişer.

| Adım | Hesap | Ne gösterilir |
|---|---|---|
| 1. Hasta paneli | `ahmet.celik@example.com` (sigortalı hasta) | Ana sayfada takvim, sıradaki randevu, geçmişte onaylı, iptal ve reddedilmiş randevular; **Randevularım** sayfasında "Yaklaşan" ve "Geçmiş" sekmeleri |
| 2. Randevu alma | Ahmet Çelik | Randevu al: kurum, şube, bölüm, hizmet (Dahiliye'de "Genel Muayene" ve "Kontrol Muayenesi"), doktor, tarih ve saat. Özet ekranında sigortalı tutar (%20 indirim) görünür |
| 3. Sigortasız fiyat karşılaştırması | `elif.demir@example.com` (Elif Demir, sigortasız hasta) | Ahmet ile aynı doktor ve hizmeti seçin: Ahmet'in özetinde sigortalı tutar (%20 indirimli), Elif'in özetinde tam fiyat görünür |
| 4. Onay gerektiren doktor | `kemal.aslan@sakaryasehir.example` (Kardiyoloji) | **Randevular > Bekleyen** sekmesinde onay bekleyen randevular: "Onayla" ve "Reddet". Doktor, ayarı aynı sayfadaki "Randevu onayı" bölümünden değiştirir |
| 5. Hasta tarafında sonuç | Ahmet Çelik | Dr. Kemal Aslan ile alınan randevu "Onay bekliyor" iken onaylanınca "Onaylandı", reddedilince "Reddedildi" görünür. Kardiyoloji'de yeni randevu almak da "Onay bekliyor" durumunu üretir |
| 6. Doktorun bugünü | `hakan.yildiz@sakaryasehir.example` ve `kemal.aslan@sakaryasehir.example` | Doktor ana sayfasındaki **Bugünün randevuları** listesinde bugün için sabit saatlerde randevular görünür (Dr. Hakan Yıldız 4, Dr. Kemal Aslan 3; Dr. Kemal Aslan'da biri onay bekler, Ahmet Çelik ve Elif Demir de bu listededir). Hafta sonu bu liste boştur |
| 7. Otomatik onaylı doktorlar | `hakan.yildiz@sakaryasehir.example` (Dahiliye) ve `selin.aydin@sakaryasehir.example` (Gastroenteroloji) | Randevuları hemen "Onaylandı" olur (Dr. Kemal Aslan'ın "Onay bekliyor" akışıyla karşılaştırın); **Uygunluk** sayfasında takvim, çalışma saatleri ve randevu süresi |
| 8. Klinik yöneticisi | `ayse.yilmaz@sakaryasehir.example` (Merkez) | Ana sayfada personel sayıları ve çalışma saatleri; **Bölümler** ve **Personel** sayfalarında doktor ve sağlık teknisyeni yönetimi |
| 9. Sistem yöneticisi | `admin@clinic.example` | **Kurumlar** (şube ve çalışma saatleri) ve **Yöneticiler** sayfaları |

Diğer sigortalı hastalar: `mehmet.yilmaz@example.com`, `mustafa.kaya@example.com`, `hasan.cetin@example.com`,
`emre.sahin@example.com`. Sigortasızlar: `elif.demir@example.com`, `zeynep.arslan@example.com`,
`ayse.korkmaz@example.com`, `selin.aydogan@example.com`. Teknisyenlerin giriş hesabı yoktur; yönetici onları
**Personel** sayfasında görür.

## Önemli kurallar

- **Randevu penceresi:** hastalar bugünden başlayarak bugün + 14 güne kadar (Europe/Istanbul takvim günleri, iki uç dahil)
  randevu alabilir. Kural tek yerde, `BookingRules.WindowDays` içindedir. Pencere dışında slot listesi boş döner;
  pencere dışı randevu isteği **422** ile reddedilir. Geçmişe randevu da 422 döner (tam şu an başlayan slot hâlâ alınabilir).
- **Slot mantığı:** Hizmet türü akışı belirler. **Muayene** (`Examination`): hasta doktoru ve slotu seçer; randevu tam bir
  slot kaplar, süresi doktorun randevu süresidir (hizmet süresi değil). Slotlar doktorun uygunluk aralıklarından, aralığın
  başlangıcına bağlanarak kesilir. **Tetkik** (`Diagnostic`): doktor seçilmez; randevu hizmetin sabit süresi kadardır, şubenin
  açılış saatinden başlayan ızgaraya oturur ve personelsiz (atanmamış) kaydedilir. Her iki akışta çakışan aktif randevu
  kabul edilmez. Slot listesi ile randevu denetimi aynı `SlotGrid` sınıfını kullandığından birbirleriyle çelişmez.
- **Şube çalışma saatleri:** Slotlar şubenin o günkü çalışma saatlerinin dışına taşamaz; tamamen içinde kalmayan slotlar
  listelenmez ve alınamaz. Çalışma saatleri şubenin yerel saatiyle (Europe/Istanbul) tutulur; tüm tarih-saatler UTC saklanır.
  Saatleri daraltmak, dışarıda kalan uygunlukları hastalara göstermemesine yol açar. Aynı gün için çakışan aralıklar
  tanımlanamaz.
- **Silme politikası:** İçi boş kayıt silinebilir; bağlı veri varsa **409** döner. Kurum şubesiz, şube bölümsüz ve
  yöneticisiz, bölüm hizmetsiz ve personelsiz olmalıdır. Randevusu olan hizmet silinemez; yaklaşan randevusu olan personel
  silinemez ve pasife alınamaz. Geçmişi korumak için personel silinmek yerine pasife alınır (pasif personel randevu almaz ve giriş yapamaz).
- **Otomatik onay ve onay gerektiren mod:** Her doktorun `AutoConfirmAppointments` ayarı vardır (varsayılan açık).
  `DoctorPreferenceConfirmationPolicy` doktorsuz (tetkik) veya otomatik onaylayan doktorlu randevuyu `Confirmed`,
  diğerini `Pending` başlatır. Atanan doktor `Pending` randevuyu onaylar veya reddeder; reddedilen randevu slotu serbest bırakır.
  Tüm sistemi otomatik onaya çevirmek `DependencyInjection` içinde tek satırlık değişikliktir.
- **%20 sigorta indirimi:** `InsuranceDiscountPricingStrategy`, sigorta numarası olan hastaya hizmet fiyatının %20 indirimlisini
  uygular (iki ondalık). Fiyat önizlemesi (`GET /api/services/{serviceId}/price-preview`) aynı stratejiyi kullanır, bu yüzden
  ekranda görülen tutar kaydedilen tutarla aynıdır.
- **Varsayılan çalışma takvimi:** Otomatik takvimi açık her doktora hafta içi 09:00-12:00 ve 13:00-16:00 (12:00-13:00 öğle arası)
  uygunluğu eklenir. `DefaultScheduleWorker`, `IDefaultScheduleService` üzerinden bunu açılışta ve 6 saatte bir bugünden
  14 gün sonrasına kadar uzatır. Yalnızca son uygunluk gününden sonrasını ekler; doktorun sildiği (izin) günler kapalı kalır.
  Yeni doktor eklenince takvimi hemen doldurulur. Doktor anahtarı kapatıp kendi takvimini girebilir.
- **Yetkilendirme:** Rol uç noktayı belirler, `AccessGuard` kaynağın çağıranın olup olmadığını denetler: hasta yalnızca kendi
  randevularını, doktor yalnızca kendi uygunluk ve randevularını, klinik yöneticisi yalnızca token'daki kurumun kaynaklarını yönetir.
  Geçersiz token 401, yetkisiz rol veya başkasının kaynağı 403 döner. Pasife alınmış veya silinmiş kullanıcının token'ı 401 ile reddedilir.

## Bilinen sınırlamalar

- Aynı slota aynı anda iki istek gelirse yarış durumu (concurrency) ele alınmamıştır (kodda eşzamanlılık belirteci yoktur).
- Teknisyen rolünün giriş ekranı ve işlevi yoktur; yalnızca personel kaydıdır.
- Klinik yöneticisi doktorun uygunluğunu düzenleyemez; uygunluğu yalnızca doktor yönetir.
- Uygunluk, haftalık şablon ve istisna günleri yerine tarih aralığı modeliyle tutulur; model bilerek basit bırakılmıştır.
- Otomatik takvim açıkken doktor son günleri silerse (örneğin tüm gelecek haftayı), bu günler geri eklenir; önce anahtar kapatılmalıdır.
- Otomatik takvim aradaki boşlukları (örneğin "bugün") doldurmaz, yalnızca son günden sonrasını uzatır; bu yüzden demo öncesinde veritabanı sıfırlanır.
- Toplu uygunluk ekleme önizlemesi bir üst sınırdır; çakışan günleri yalnızca API bilir ve atlar.
- Doktor başka bir bölüme taşınamaz.
- İlk girişte şifre değiştirmeye zorlama, e-posta doğrulama ve "şifremi unuttum" akışı yoktur.
- Tohum veri yalnızca boş veritabanında yüklenir; örnek veri değiştiğinde veritabanı sıfırlanmalıdır.
- Projede farklı günlere yayılmış en az 10 commit şartı sağlanamamıştır (ayrıntı için `docs/requirements-map.md`).
