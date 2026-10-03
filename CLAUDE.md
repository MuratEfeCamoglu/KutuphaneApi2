# KutuphaneApi — Proje Talimatları

Bu dosya projenin ana talimat dosyasıdır. Proje iskeleti ve çalışma döngüsü ayrı dosyalardadır ve aşağıda içe aktarılır:

@ISKELET.md
@AGENT.md

## Proje Özeti

.NET 10 ve ASP.NET Core ile yazılan bir **Kütüphane Yönetim REST API'si**. Yazarlar, kategoriler, kitaplar, üyeler ve ödünç alma/iade işlemlerini yönetir.

## Kullanıcı Hakkında

- Kullanıcı C# biliyor ancak ASP.NET Core ve EF Core'da yeni.
- Kodu sen yazıyorsun; kullanıcı yazdığın kodu okuyarak ve `NOTLAR.md` dosyasındaki açıklamalarla **öğreniyor**.
- Bu yüzden basit, okunabilir ve sektörde yaygın olan çözümleri seç. Gösteriş amaçlı soyutlama ve gereksiz katman ekleme.
- Kullanıcıya çalışma sırasında soru sorma. Çalışma biçimi `AGENT.md` dosyasında tanımlıdır.

## Çalışma Dizini

```
Deneme/                    ← kök dizin (Claude Code burada çalışır)
├── CLAUDE.md, ISKELET.md, AGENT.md
├── NOTLAR.md              ← sen oluşturur ve güncellersin
├── KutuphaneApi.slnx
├── KutuphaneApi/          ← API projesi
├── KutuphaneApi.Tests/    ← test projesi
└── WebApplication1/       ← KULLANICININ DENEME PROJESİ, ASLA DOKUNMA
```

---

## Projenin Sınırları

### Kapsam İçi
- Yazar, kategori, kitap ve üye için CRUD işlemleri
- Kitap–yazar (bire-çok) ve kitap–kategori (çoka-çok) ilişkileri
- Ödünç alma ve iade işlemleri, iş kurallarıyla birlikte (`ISKELET.md` → İş Kuralları)
- Kitap listesinde sayfalama, arama, filtreleme ve sıralama
- FluentValidation ile girdi doğrulama
- Merkezi hata yönetimi (ProblemDetails formatında yanıtlar)
- Unit ve integration testler
- Geliştirme ortamı için örnek (seed) veri

### Kapsam Dışı (Bu projede YAPMA)
- Kimlik doğrulama ve yetkilendirme (JWT, Identity) → sonraki proje olan Blog API'sinin konusu
- Çok projeli Clean Architecture, CQRS, mikroservis → sonraki projelerin konusu
- Docker, Redis, mesaj kuyrukları, Serilog, bulut dağıtımı
- Frontend veya arayüz
- Eşzamanlılık (concurrency) kontrolü, e-posta bildirimi, ceza/ücret hesaplama

### Teknik Sınırlar
- Hedef framework: `net10.0`. Paketlerin .NET 10 ile uyumlu sürümlerini kullan (EF Core için 10.x).
- Sadece iki proje: `KutuphaneApi` (Web API) ve `KutuphaneApi.Tests` (xUnit).
- Veritabanı: SQLite, EF Core ile. Bağlantı cümlesi `appsettings.json` içinde durur, koda gömülmez.
- Controller tabanlı API. Minimal API endpoint'i yazma.
- Minimal hosting modeli (`Program.cs`). `Startup.cs` oluşturma.
- API dokümantasyonu: `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore`. Swashbuckle kullanma.

### Kullanılmayacak Paket ve Desenler
- **AutoMapper ve MediatR:** Ticari lisansa geçtiler ve bu proje için gereksiz karmaşıklık katarlar. Eşleme (mapping) elle yazılır.
- **FluentValidation.AspNetCore:** Otomatik doğrulama yaklaşımı artık önerilmiyor. Sadece `FluentValidation.DependencyInjectionExtensions` kullan, validator'ları servislerde elle çağır.
- **Generic Repository / Unit of Work:** `DbContext` zaten bu görevi görür. Servisler `AppDbContext`'i doğrudan kullanır.
- **Mock kütüphaneleri (Moq vb.):** Servis testleri SQLite in-memory veritabanıyla yapılır, mock'a gerek yok.

### Dosya Sistemi ve Güvenlik Sınırları
- Sadece kök dizindeki dokümanlar, `KutuphaneApi/` ve `KutuphaneApi.Tests/` içinde dosya oluştur veya değiştir.
- `WebApplication1/` klasörüne ve kök dizin dışındaki hiçbir yere dokunma, silme yapma.
- `git push` yapma, uzak depo (remote) ekleme.
- Global araç kurma. `dotnet-ef` yerel araç (tool manifest) olarak kurulur.
- `dotnet watch` gibi bitmeyen komutları ön planda çalıştırma; oturumu kilitler.

---

## Kod Kuralları

- Sınıf, metot, değişken ve endpoint adları **İngilizce**; yorumlar ve notlar **Türkçe**.
- Yorumlar öğreticidir ama abartılı değildir: sadece yeni bir kavramın ilk kullanıldığı yerde "bu ne işe yarar" açıklaması yaz, her satıra yorum ekleme.
- File-scoped namespace kullan. Nullable reference types açık kalır.
- Hedef: `dotnet build` sonucunda **0 hata, 0 uyarı**. Uyarıları bastırma (`#pragma warning disable`, `NoWarn` yasak), düzelt.
- DTO'lar `record` olarak yazılır. Controller'lar **asla entity döndürmez**, her zaman DTO döndürür.
- Tüm veritabanı işlemleri `async`'tir ve `CancellationToken` alır.
- Okuma sorgularında `AsNoTracking()` ve `Select` ile DTO'ya projeksiyon kullan; N+1 sorgu üretme.
- Servisler interface üzerinden DI'a kaydedilir (`AddScoped<IBookService, BookService>()`). DI kayıtları `Extensions/ServiceCollectionExtensions.cs` içinde toplanır, `Program.cs` sade kalır.
- Zaman için `DateTime.UtcNow` yerine DI ile alınan `TimeProvider` kullanılır (testlerde zamanı kontrol edebilmek için).
- HTTP durum kodları: oluşturma `201 Created` (`CreatedAtAction`), güncelleme ve silme `204 No Content`, bulunamadı `404`, doğrulama hatası `400`, iş kuralı ihlali `409 Conflict`.
- Hatalar `ProblemDetails` formatında döner. Controller'larda try-catch yazma; hataları merkezi exception handler yönetir.
- Her yeni veya değişen endpoint için `KutuphaneApi.http` dosyasına örnek istek ekle.
