# KutuphaneApi — Öğrenme Notları

## İlerleme
- [x] Adım 0 — Hazırlık
- [x] Adım 1 — Entity'ler
- [x] Adım 2 — DbContext
- [x] Adım 3 — Migration ve seed
- [x] Adım 4 — API dokümantasyonu
- [ ] Adım 5 — Hata altyapısı
- [ ] Adım 6 — Authors
- [ ] Adım 7 — Doğrulama
- [ ] Adım 8 — Categories
- [ ] Adım 9 — Books
- [ ] Adım 10 — Kitap listesi
- [ ] Adım 11 — Members
- [ ] Adım 12 — Ödünç ve iade
- [ ] Adım 13 — Ödünç sorguları
- [ ] Adım 14 — Unit testler
- [ ] Adım 15 — Integration testler
- [ ] Adım 16 — Son gözden geçirme

## Kararlar
- Adım 0: Git deposu `Deneme/` klasöründe ayrıca başlatıldı (`git init -b main`). Üst dizindeki (`C:\Users\Efe`) depoya commit atılmasın diye bu klasör kendi deposuna sahip.
- Adım 0: 13:34'ten beri çalışan eski şablon uygulaması (`KutuphaneApi.exe`) build'i kilitlediği için durduruldu.
- Adım 2: 13:33'te başlatılmış `dotnet watch` oturumu uygulamayı sürekli yeniden başlatıp build'i kilitlediği için durduruldu. Geliştirme bitince `dotnet watch` yeniden açılabilir.

## Çözülemeyen Sorunlar
Yok

## Adım Notları

### Adım 0 — Hazırlık
**Ne yapıldı:** `Deneme/` klasöründe git deposu açıldı, `dotnet new gitignore` ile .NET için hazır `.gitignore` üretildi ve sonuna `WebApplication1/` ile `*.db` satırları eklendi. `KutuphaneApi.slnx` çözüm dosyası oluşturulup API projesi eklendi, şablondaki WeatherForecast örneği silindi.
**Yeni kavramlar:**
- *`.slnx`*: Yeni XML tabanlı çözüm (solution) formatı. Eski `.sln`'e göre çok daha okunur; içinde sadece proje yolları var (`KutuphaneApi.slnx`).
- *`.gitignore`*: Git'in takip etmeyeceği dosyalar. `bin/`, `obj/` derleme çıktılarıdır, depoya girmemeli. `*.db` ise yerel veritabanı dosyasıdır; herkes kendi makinesinde migration ile üretir.
**Neden böyle:** Çözüm dosyası, ileride eklenecek test projesiyle birlikte `dotnet build KutuphaneApi.slnx` tek komutuyla her şeyi derlemeyi sağlar.
**Karşılaşılan hatalar:** İlk build `MSB3021: ... KutuphaneApi.exe üzerine kopyalanamıyor` hatası verdi. Kök neden: uygulamanın eski bir kopyası arka planda çalışıyordu ve Windows çalışan `.exe` dosyasının üzerine yazmaya izin vermez. Çözüm: süreci durdurup tekrar derlemek. Bu hatayı ileride `dotnet run` açık bırakıp build aldığında da göreceksin.
**Bilerek boz:** `.gitignore`'dan `bin/` satırını silip `git status` çalıştır. Ne kadar dosya görünüyor?
**Kendini kontrol et:**
- `.slnx` dosyası olmasaydı `dotnet build` hangi klasörde çalıştırılmalıydı?
- Veritabanı dosyasını neden depoya koymuyoruz?

### Adım 1 — Entity'ler
**Ne yapıldı:** `Entities/` klasörüne beş sınıf eklendi: `Author`, `Category`, `Book`, `Member`, `Loan`. Her biri bir tabloyu temsil eder; aralarındaki ilişkiler navigation property'lerle kuruldu.
**Yeni kavramlar:**
- *Entity*: EF Core'un tabloya çevirdiği düz C# sınıfı. `Id` adlı özellik otomatik olarak birincil anahtar (primary key) kabul edilir (`Entities/Author.cs`).
- *Navigation property*: `Book.Author` veya `Author.Books` gibi, ilişkili kayda nesne olarak erişmeyi sağlayan özellik. Veritabanında sütun değildir; sütun olan `AuthorId`'dir (foreign key) (`Entities/Book.cs`).
- *Skip navigation (çoka-çok)*: `Book.Categories` ve `Category.Books` iki tarafta da koleksiyon olduğu için EF Core `BookCategory` ara tablosunu kendisi üretecek; ayrıca entity yazmadık.
- *`= null!`*: Nullable açıkken derleyici "bu referans null kalabilir" uyarısı verir. EF Core bu alanı dolduracağı için `null!` ile uyarıyı bilinçli olarak kapatıyoruz (bu bir bastırma değil, "null-forgiving" operatörüdür).
**Neden böyle:** Müsait kopya sayısı ve ödünç durumu (Active/Overdue/Returned) entity'de alan olarak yok; bunlar her sorguda hesaplanacak. Saklanan değer zamanla gerçek durumdan sapabilir, hesaplanan değer sapmaz.
**Karşılaşılan hatalar:** Yok.
**Bilerek boz:** `Book.Author` satırındaki `= null!` kısmını sil ve build al. Hangi uyarı (CS8618) çıkıyor ve neden?
**Kendini kontrol et:**
- `AuthorId` ile `Author` özelliği arasındaki fark nedir?
- Ödüncün "Overdue" olduğunu neden bir `Status` sütununda saklamıyoruz?
- Koleksiyonları neden `new List<Book>()` ile başlatıyoruz?

### Adım 2 — DbContext
**Ne yapıldı:** EF Core SQLite ve Design paketleri eklendi. `AppDbContext` yazıldı; tablo kuralları `Data/Configurations/` altında her entity için ayrı bir sınıfta Fluent API ile tanımlandı. Bağlantı cümlesi `appsettings.json`'a kondu ve DbContext `Extensions/ServiceCollectionExtensions.cs` içinde DI'a kaydedildi.
**Yeni kavramlar:**
- *DbContext* (`Data/AppDbContext.cs`): Veritabanı oturumu. `DbSet<T>` özellikleri tablolardır; `context.Books.Where(...)` yazdığında EF Core bunu SQL'e çevirir.
- *Primary constructor* (`AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)`): C# 12 ile gelen kısa kurucu yazımı.
- *IEntityTypeConfiguration&lt;T&gt;* (`Data/Configurations/BookConfiguration.cs`): Bir entity'nin tablo kurallarını (uzunluk, index, ilişki) ayrı bir sınıfta toplar. `ApplyConfigurationsFromAssembly` hepsini otomatik bulur.
- *Benzersiz index* (`HasIndex(...).IsUnique()`): ISBN, e-posta ve kategori adı tekrar edemez; veritabanı da bunu garanti eder.
- *DeleteBehavior.Restrict*: İlişkili kayıt varken üst kaydın silinmesini engeller. Varsayılan `Cascade` olsaydı yazarı silmek tüm kitaplarını da silerdi.
- *Collation NOCASE*: SQLite'ın karşılaştırmayı harf büyüklüğüne duyarsız yapması. `ali@x.com` ile `ALI@x.com` aynı e-posta sayılır.
- *ValueConverter* (`Data/UtcDateTimeConverter.cs`): SQLite tarihi metin olarak saklar ve "bu UTC'ydi" bilgisini kaybeder. Dönüştürücü okurken `DateTimeKind.Utc` işaretler, böylece JSON çıktısında tarihler `...Z` ile biter.
- *Dependency Injection (DI)*: `AddDbContext` ile kaydedilen `AppDbContext`, her HTTP isteği için bir kez oluşturulur (scoped) ve ihtiyaç duyan sınıfa kurucu üzerinden verilir.
**Neden böyle:** Kurallar Data Annotations (`[MaxLength]`) yerine Fluent API ile yazıldı; entity'ler sade kalır ve ilişki/silme davranışı gibi annotation ile ifade edilemeyen ayarlar da aynı yerde durur. Restrict seçildi çünkü iş kuralları 7 ve 8 silmeyi engellemeyi istiyor; servis bu kuralları kontrol edip 409 dönecek, veritabanı ise son güvenlik ağı.
**Karşılaşılan hatalar:** Build yine `MSB3021 / dosya kilitli` hatası verdi. Kök neden: arka planda açık bir `dotnet watch` oturumu vardı; her dosya değişikliğinde uygulamayı yeniden başlatıp `.exe` dosyasını kilitliyordu. Öldürülen uygulamayı watch tekrar başlattığı için tek çözüm watch sürecinin kendisini durdurmaktı.
**Bilerek boz:** `BookConfiguration` içindeki `OnDelete(DeleteBehavior.Restrict)` satırını `Cascade` yap. Adım 3'te migration ürettiğinde migration dosyasında ne değişir?
**Kendini kontrol et:**
- Bağlantı cümlesini neden koda değil `appsettings.json`'a yazıyoruz?
- `UseCollation("NOCASE")` olmasaydı aynı e-postayla iki üye eklenebilir miydi?
- DbContext neden "scoped" yaşam süresiyle kaydedilir?

### Adım 3 — Migration ve seed
**Ne yapıldı:** `dotnet-ef` yerel araç olarak kuruldu (`.config/dotnet-tools.json`), `InitialCreate` migration'ı `Data/Migrations/` altına üretildi. Development ortamında uygulama açılırken `Data/Seed/DbInitializer.cs` migration'ları uygular ve veritabanı boşsa 5 yazar, 4 kategori, 9 kitap, 3 üye ve 3 ödünç (iade edilmiş, aktif, gecikmiş) ekler.
**Yeni kavramlar:**
- *Yerel araç (tool manifest)*: `dotnet-ef` makineye global değil, projeye kurulur. Depoyu klonlayan biri `dotnet tool restore` ile aynı sürümü alır.
- *Migration* (`Data/Migrations/*_InitialCreate.cs`): Modeldeki değişikliği veritabanına uygulayan C# kodu. `Up` uygular, `Down` geri alır. `AppDbContextModelSnapshot.cs` modelin son halidir; bir sonraki migration bununla karşılaştırılarak üretilir.
- *`Database.MigrateAsync()`*: Henüz uygulanmamış migration'ları çalıştırır. Hangilerinin uygulandığını `__EFMigrationsHistory` tablosunda tutar.
- *Scope* (`services.CreateScope()`): DbContext scoped bir servistir; HTTP isteği dışında (uygulama açılışında) kullanmak için scope'u elle açarız.
- *Nesne grafiği ile ekleme*: Kitaplara `Author = orhan` gibi nesne verdik, Id vermedik. `SaveChanges` sırasında EF Core önce yazarı ekleyip Id'sini alır, sonra kitabın `AuthorId`'sini doldurur.
**Neden böyle:** EF Core'un `HasData` yöntemi yerine kodla seed seçildi: `HasData` sabit Id'ler ister ve her değişiklikte yeni migration üretir; ayrıca "bugünden 3 gün önce" gibi göreli tarihler yazılamaz. Seed sadece Development'ta çalışır, gerçek ortama örnek veri girmez. Seed'de `TimeProvider.System` kullanıldı çünkü `TimeProvider` DI kaydı Adım 12'de yapılacak.
**Karşılaşılan hatalar:**
1. `dotnet new tool-manifest` .NET 10'da dosyayı kök dizine (`dotnet-tools.json`) koydu; ISKELET `.config/` altında istediği için taşındı. `dotnet` iki konumu da arar.
2. Uygulama ilk açılışta `SQLite Error 1: 'no such table: Authors'` verdi. Kök neden: `dotnet run --no-build` ile eski DLL çalıştırıldı. `dotnet ef migrations add` projeyi migration dosyasını yazmadan *önce* derler, yani derlenmiş DLL'de yeni migration yoktu. `MigrateAsync` boş bir veritabanı açtı ama uygulanacak migration bulamadı. Çözüm: önce `dotnet build`, sonra çalıştırmak.
**Bilerek boz:** `kutuphane.db` dosyasını sil ve uygulamayı çalıştır. Dosya yeniden oluşuyor mu? Sonra `DbInitializer` içindeki `AnyAsync` kontrolünü kaldırıp uygulamayı iki kez başlat. İkinci açılışta ne olur (ipucu: benzersiz ISBN)?
**Kendini kontrol et:**
- `__EFMigrationsHistory` tablosu ne işe yarar?
- Seed verisini neden Production'da çalıştırmıyoruz?
- Kitaplara `AuthorId = 1` yerine `Author = orhan` vermenin avantajı ne?
**Kendin yazmayı dene:** Seed'e kendi sevdiğin bir yazar ve iki kitabını ekle, `kutuphane.db`'yi silip uygulamayı yeniden başlat.

### Adım 4 — API dokümantasyonu
**Ne yapıldı:** `Scalar.AspNetCore` paketi eklendi ve Development ortamında `app.MapScalarApiReference()` çağrıldı. Artık `http://localhost:5041/scalar` adresinde API'yi tarayıcıdan deneyebilirsin.
**Yeni kavramlar:**
- *OpenAPI dokümanı*: API'nin tüm endpoint'lerini, parametrelerini ve yanıt şekillerini anlatan JSON dosyası. `AddOpenApi()` + `MapOpenApi()` bunu `/openapi/v1.json` adresinde üretir.
- *Scalar*: Bu JSON'u okuyup istek gönderebileceğin bir arayüz çizer. .NET 9'dan önce şablonda Swagger UI (Swashbuckle) vardı; artık şablon sadece JSON üretiyor, arayüzü sen seçiyorsun.
**Neden böyle:** Dokümantasyon sadece Development'ta açık. Gerçek ortamda API'nin tüm yapısını herkese göstermek istemeyiz.
**Karşılaşılan hatalar:** Yok. Not: `/scalar` önce `302` ile `/scalar/` adresine yönlendirir; tarayıcı bunu otomatik takip eder, `curl` için `-L` gerekir.
**Bilerek boz:** `ASPNETCORE_ENVIRONMENT` değerini `launchSettings.json`'da `Production` yapıp uygulamayı aç. `/scalar` ne döner? (Sonra geri al.)
**Kendini kontrol et:**
- OpenAPI dokümanı ile Scalar arasındaki fark nedir?
- `paths` şu an neden boş?

## Son Rapor
<!-- Adım 16'da yazılır -->
