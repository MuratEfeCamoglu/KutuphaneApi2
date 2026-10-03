# KutuphaneApi — Öğrenme Notları

## İlerleme
- [x] Adım 0 — Hazırlık
- [x] Adım 1 — Entity'ler
- [x] Adım 2 — DbContext
- [x] Adım 3 — Migration ve seed
- [x] Adım 4 — API dokümantasyonu
- [x] Adım 5 — Hata altyapısı
- [x] Adım 6 — Authors
- [x] Adım 7 — Doğrulama
- [x] Adım 8 — Categories
- [x] Adım 9 — Books
- [x] Adım 10 — Kitap listesi
- [x] Adım 11 — Members
- [x] Adım 12 — Ödünç ve iade
- [x] Adım 13 — Ödünç sorguları
- [x] Adım 14 — Unit testler
- [x] Adım 15 — Integration testler
- [ ] Adım 16 — Son gözden geçirme

## Kararlar
- Adım 0: Git deposu `Deneme/` klasöründe ayrıca başlatıldı (`git init -b main`). Üst dizindeki (`C:\Users\Efe`) depoya commit atılmasın diye bu klasör kendi deposuna sahip.
- Adım 0: 13:34'ten beri çalışan eski şablon uygulaması (`KutuphaneApi.exe`) build'i kilitlediği için durduruldu.
- Adım 2: 13:33'te başlatılmış `dotnet watch` oturumu uygulamayı sürekli yeniden başlatıp build'i kilitlediği için durduruldu. Geliştirme bitince `dotnet watch` yeniden açılabilir.
- Adım 7: `TimeProvider` DI kaydı Adım 12 yerine burada yapıldı; yazar doğum yılının gelecekte olmaması kuralı "bugün"ü bilmeyi gerektiriyor ve `DateTime.UtcNow` kullanmak kod kurallarına aykırı.
- Adım 7: Doğrulama hata anahtarları C# özellik adıyla (`FirstName`) döner; camelCase'e çevirmek ek kod gerektirdiği için sade tutuldu.
- Adım 8: Kategori adı ve e-posta benzersizliği SQLite `NOCASE` ile sağlanıyor; bu sadece ASCII harflerde büyük/küçük harf duyarsız ("KLASİK" ≠ "Klasik"). Normalize sütun eklemek sadelik için yapılmadı.
- Adım 9: İstek gövdesindeki `authorId`/`categoryIds` bulunamazsa 404 (`NotFoundException`) dönülüyor; yeni bir exception türü eklememek için mevcut kalıp kullanıldı.
- Adım 9: Loan ilişkileri Restrict kaldı; iade edilmiş ödünç geçmişi olan kitap/üye silinirken geçmiş servis tarafından aynı transaction'da açıkça siliniyor (kural 7 sadece iade edilmemiş ödüncü yasaklıyor).
- Adım 12: `GET /api/loans/{id}` Adım 13 yerine burada yazıldı; `POST /api/loans` yanıtındaki `CreatedAtAction` bu endpoint'e ihtiyaç duyuyor.
- Adım 12: İade (`POST /api/loans/{id}/return`) mevcut kaydı güncellediği için 204 dönüyor (CLAUDE.md'deki güncelleme kuralı).

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

### Adım 5 — Hata altyapısı
**Ne yapıldı:** İki özel exception (`NotFoundException`, `BusinessRuleException`) ve bunları HTTP yanıtına çeviren `GlobalExceptionHandler` yazıldı. `AddProblemDetails`, `UseExceptionHandler` ve `UseStatusCodePages` ile tüm hata yanıtları aynı JSON biçiminde dönüyor.
**Yeni kavramlar:**
- *Özel exception* (`Common/Exceptions/`): Servis "bu kayıt yok" veya "bu kurala aykırı" dediğinde HTTP'den habersiz şekilde exception fırlatır. Servis `404` veya `409` bilmez; bu çeviri tek bir yerde yapılır.
- *IExceptionHandler* (`Infrastructure/GlobalExceptionHandler.cs`): .NET 8 ile gelen arayüz. `UseExceptionHandler()` middleware'i yakalanmamış exception'ı buraya verir. `switch` ifadesiyle exception türüne göre durum kodu seçilir.
- *ProblemDetails*: Hata yanıtları için standart format (`type`, `title`, `status`, `detail`, `traceId`). İstemci her hatayı aynı şekilde okuyabilir. Content-Type `application/problem+json` olur.
- *Middleware sırası* (`Program.cs`): `UseExceptionHandler` en başa konur ki kendinden sonraki tüm adımlardaki hataları yakalasın.
- *UseStatusCodePages*: Gövdesi boş hata yanıtlarına (örneğin olmayan bir adres) ProblemDetails gövdesi ekler.
**Neden böyle:** Alternatif, her controller metodunda try-catch yazmaktı; bu hem tekrar hem de unutulmaya açık. 500 hatalarında `detail` boş bırakılır ve hata loglanır; böylece SQL veya stack trace istemciye sızmaz.
**Karşılaşılan hatalar:** Yok.
**Bilerek boz:** `Program.cs`'de `app.UseStatusCodePages();` satırını sil ve `curl -i http://localhost:5041/api/nope` çalıştır. Yanıt gövdesi nasıl değişti?
**Kendini kontrol et:**
- Servis neden doğrudan `return NotFound()` yazmıyor da exception fırlatıyor?
- 500 hatasında neden `exception.Message` istemciye gönderilmiyor?
- `UseExceptionHandler` pipeline'ın sonunda olsaydı ne olurdu?

### Adım 6 — Authors
**Ne yapıldı:** İlk tam dikey dilim: `Dtos/Authors/` altında üç record, `Mappings/AuthorMappings.cs`, `IAuthorService`/`AuthorService` ve `AuthorsController`. Beş endpoint (`GET`, `GET {id}`, `POST`, `PUT {id}`, `DELETE {id}`) çalışıyor; kitabı olan yazarı silmek 409, olmayan yazar 404 dönüyor. `KutuphaneApi.http` dosyasına örnek istekler eklendi.
**Yeni kavramlar:**
- *DTO ve record* (`Dtos/Authors/AuthorDto.cs`): API sözleşmesi entity'den ayrıdır. Entity'yi döndürseydik `Books` koleksiyonu yüzünden döngüsel JSON veya istemeden sızan alanlar oluşurdu. `CreateAuthorRequest` ile `UpdateAuthorRequest` şu an aynı görünse de ayrı tutulur; ileride farklılaşabilirler.
- *IQueryable projeksiyonu* (`Mappings/AuthorMappings.cs` → `SelectDto`): `Select` bir `IQueryable` üzerinde çağrıldığı için EF Core onu SQL'e çevirir. `a.Books.Count` bir `COUNT` alt sorgusuna dönüşür; kitaplar belleğe yüklenmez ve N+1 olmaz.
- *AsNoTracking*: Okuma sorgularında EF Core'un "bu nesne değişti mi" takibini kapatır.
- *Tracking ile güncelleme* (`AuthorService.UpdateAsync`): `FindAsync` ile okunan entity takip edilir. Özelliklerini değiştirip `SaveChangesAsync` çağırınca EF Core sadece değişen sütunlar için `UPDATE` üretir.
- *`?? throw`*: Sorgu `null` dönerse aynı satırda exception fırlatmanın kısa yolu.
- *ActionResult&lt;T&gt; ve CreatedAtAction* (`Controllers/AuthorsController.cs`): `CreatedAtAction(nameof(GetById), new { id }, dto)` 201 durum kodu, yeni kaydın adresini veren `Location` başlığı ve gövdeyi birlikte üretir.
- *Route constraint* (`{id:int}`): `/api/authors/abc` gibi istekler bu metoda hiç düşmez, 404 döner.
- *LowercaseUrls* (`ServiceCollectionExtensions.cs`): `[controller]` sınıf adını olduğu gibi aldığı için Location başlığı `/api/Authors/6` oluyordu; bu ayarla `/api/authors/6` olur.
**Neden böyle:** Controller'da sadece "servisi çağır, doğru durum kodunu dön" kaldı; 404 ve 409 kararlarını servis exception ile verir. Oluşturma sonrası DTO'yu tekrar `GetByIdAsync` ile okuyoruz; bir sorgu fazladan çalışır ama dönen veri her zaman GET ile aynı olur (örneğin `bookCount`).
**Karşılaşılan hatalar:** Duman testinde `"Yaşar"` içeren POST 400 döndü: `The JSON value could not be converted`. Kök neden API'de değildi; Windows'ta curl'e komut satırından verilen metin UTF-8 yerine sistem kod sayfasıyla gönderiliyor ve `ş` bozuk bayta dönüşüyordu. JSON'u UTF-8 dosyadan (`--data-binary @body.json`) gönderince düzeldi. Ders: bir hata gördüğünde önce "hata gerçekten benim kodumda mı?" diye sor.
**Bilerek boz:** `AuthorService.GetAllAsync` içindeki `.SelectDto()` satırını kaldırıp metodun `List<Author>` döndürmesini sağla ve controller'dan doğrudan entity dön. `GET /api/authors` ne döndürür? `Books` alanı neden boş?
**Kendini kontrol et:**
- `FindAsync` ile `FirstOrDefaultAsync` arasındaki fark nedir?
- Controller neden `try { ... } catch (NotFoundException) { return NotFound(); }` yazmıyor?
- `DELETE` neden gövdesiz 204 döner?
**Kendin yazmayı dene:** `GET /api/authors/{id}/books` endpoint'i ekle: yazarın kitaplarının başlıklarını `List<string>` olarak dönsün (projeksiyonla).

### Adım 7 — Doğrulama
**Ne yapıldı:** `FluentValidation.DependencyInjectionExtensions` eklendi. `Validators/` altında yazar istekleri için iki validator yazıldı; DI'a toplu kaydedildi. Servis, işe başlamadan önce `ValidateAndThrowAsync` çağırıyor. `GlobalExceptionHandler` artık `ValidationException`'ı alan bazlı hatalarla birlikte **400**'e çeviriyor.
**Yeni kavramlar:**
- *AbstractValidator&lt;T&gt;* (`Validators/CreateAuthorRequestValidator.cs`): Kurallar `RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100)` gibi zincirle yazılır. Hazır kural yoksa `Must(...)` ile kendi koşulunu yazarsın.
- *Elle doğrulama* (`AuthorService.CreateAsync`): Validator, servise `IValidator<CreateAuthorRequest>` olarak enjekte edilir ve açıkça çağrılır. Doğrulamanın nerede ve ne zaman çalıştığı kodda görünür.
- *ValidationProblemDetails* (`Infrastructure/GlobalExceptionHandler.cs`): ProblemDetails'e `errors` sözlüğü ekler: `{"FirstName": ["'First Name' boş olmamalı."]}`. Hatalar `GroupBy(PropertyName)` ile alana göre gruplanır.
- *TimeProvider* (`ServiceCollectionExtensions.cs`): "Doğum yılı gelecekte olamaz" kuralı bugünün tarihini bilmeli. `DateTime.UtcNow` yerine DI'dan gelen `TimeProvider` kullanıldı; testte saat sabitlenebilir.
- *SuppressImplicitRequiredAttributeForNonNullableReferenceTypes* (`Program.cs`): Nullable açıkken ASP.NET Core `string FirstName` gibi alanları gizlice `[Required]` sayar ve FluentValidation'dan önce kendi İngilizce 400'ünü döner. Bu ayarla eksik alan `null` gelir ve mesajı FluentValidation üretir.
**Neden böyle:** `FluentValidation.AspNetCore` paketinin otomatik doğrulaması artık önerilmiyor (async kuralları desteklemiyor, ne zaman çalıştığı gizli). Elle çağırmak bir satır fazladan kod ama akış açık. Mesajlar `LanguageManager.Culture = "tr"` ile her makinede Türkçe. `TimeProvider` kaydı ISKELET'te Adım 12'de geçiyor; doğum yılı kuralı ona ihtiyaç duyduğu için bu adımda yapıldı (bkz. Kararlar).
**Karşılaşılan hatalar:** Yok. Not: Bozuk JSON (`{"firstName":123}`) veya boş gövde hâlâ ASP.NET Core'un kendi 400 yanıtıyla döner, çünkü bu durumda istek nesnesi hiç oluşturulamaz ve servise ulaşılamaz. İkisi de ProblemDetails formatında olduğu için istemci aynı şekilde okuyabilir.
**Bilerek boz:** `Program.cs`'teki `SuppressImplicitRequired...` ayarını kaldır ve `{"lastName":"X"}` gönder. Hata mesajı ve hata anahtarı nasıl değişti?
**Kendini kontrol et:**
- Doğrulamayı neden controller'da değil serviste çağırıyoruz?
- `ValidationException` neden 409 değil de 400?
- Validator'lar `AddValidatorsFromAssemblyContaining` ile nasıl bulunuyor?
**Kendin yazmayı dene:** `FirstName` için "sadece harf ve boşluk içerebilir" kuralı ekle (`Matches(...)` ile).

### Adım 8 — Categories
**Ne yapıldı:** Adım 6–7 kalıbı aynen tekrarlandı: DTO'lar, `CategoryMappings`, `ICategoryService`/`CategoryService`, `CategoriesController`, iki validator ve `.http` örnekleri. Aynı adla ikinci kategori (harf büyüklüğü farklı olsa bile, örn. `roman`) **409** dönüyor.
**Yeni kavramlar:**
- *Benzersizlik kontrolü serviste* (`CategoryService.EnsureNameIsUniqueAsync`): Kaydetmeden önce `AnyAsync` ile aynı ad var mı diye bakılır ve `BusinessRuleException` fırlatılır. Veritabanındaki unique index ikinci savunma hattıdır.
- *Güncellemede kendini hariç tutmak* (`c.Id != excludeId`): "Tarih" kategorisini yine "Tarih" olarak kaydetmek çakışma değildir. `excludeId` `null` ise EF Core C#'taki null anlamını korur; SQL'de `Id <> NULL` hatasına düşmez.
- *Çoka-çok silme*: Kategori silinince `BookCategories` ara tablosundaki satırlar Cascade ile silinir, kitaplar yerinde kalır.
**Neden böyle:** Unique index ihlali olursa EF Core `DbUpdateException` fırlatır ve bu bizim handler'da 500 olur. Bu yüzden ihlali önceden kontrol edip anlamlı bir 409 mesajı veriyoruz. Aynı anda iki istek aynı adı eklerse ikincisi yine 500 alabilir; eşzamanlılık bu projenin kapsamı dışında.
**Karşılaşılan hatalar:** Hata değil ama öğretici bir sınır: `"KLASİK"` adı `"Klasik"` ile çakışma sayılmadı. SQLite'ın `NOCASE` karşılaştırması sadece ASCII harfleri (A–Z) büyük/küçük harf duyarsız karşılaştırır; Türkçe `İ/i`, `Ş/ş` gibi harfler bu kapsama girmez. Tam çözüm normalize edilmiş ayrı bir sütun (`NormalizedName`) tutmak olurdu; sadelik için yapılmadı (bkz. Kararlar).
**Bilerek boz:** `EnsureNameIsUniqueAsync` çağrısını `CreateAsync`'ten kaldır ve `"Roman"` adıyla POST gönder. Hangi durum kodu dönüyor ve log'da hangi exception görünüyor?
**Kendini kontrol et:**
- `excludeId` parametresi olmasaydı bir kategoriyi kendi adıyla güncellemek ne döndürürdü?
- Unique index varken servisteki kontrol neden gerekli?
- `DELETE /api/categories/1` kitapları da siler mi?

### Adım 9 — Books
**Ne yapıldı:** Kitap CRUD'u yazıldı. Detay yanıtı yazarı (`author`), kategorileri (`categories`) ve hesaplanan müsait kopya sayısını (`availableCopies`) içeriyor. Listede daha hafif bir `BookListItemDto` kullanılıyor. İş kuralları: ISBN benzersiz (409), stok aktif ödünç sayısının altına inemez (kural 9 → 409), iade edilmemiş ödüncü olan kitap silinemez (kural 7 → 409). Olmayan yazar veya kategori Id'si 404 dönüyor.
**Yeni kavramlar:**
- *İç içe DTO* (`Dtos/Books/BookDetailDto.cs`): `BookAuthorDto` ve `BookCategoryDto` sadece kitabın ihtiyaç duyduğu alanları taşır; `CategoryDto`'yu (içinde `BookCount` var) burada tekrar kullanmadık.
- *Projeksiyonda koleksiyon* (`Mappings/BookMappings.cs`): `b.Categories.Select(...).ToList()` ve `b.Loans.Count(l => l.ReturnDate == null)` tek bir SQL sorgusuna çevrilir (JOIN + COUNT alt sorgusu). Uygulama loglarında `GET /api/books/5` için tek bir `SELECT` görebilirsin.
- *Include* (`BookService.UpdateAsync`): Okuma sorgusunda `Select` yeterliyken, güncellemede mevcut kategori listesini değiştireceğimiz için entity'yi ilişkisiyle birlikte yükleriz. `Categories.Clear()` + `Add` yapınca EF Core ara tabloda gereken `DELETE`/`INSERT`'leri kendisi üretir.
- *`Contains` → `IN`* (`GetCategoriesAsync`): Birden çok Id'yi tek sorguda getirir. `Except` ile istenen ama bulunamayan Id tespit edilir.
- *Regex ve RuleForEach* (`Validators/CreateBookRequestValidator.cs`): `Matches(@"^\d{13}$")` biçim kontrolü yapar, `RuleForEach` listedeki her elemana kural uygular.
- *Tek transaction'da birden çok değişiklik* (`BookService.DeleteAsync`): Ödünç geçmişini `RemoveRange` ile silip kitabı `Remove` ettik; ikisi aynı `SaveChangesAsync` içinde, yani ya hepsi olur ya hiçbiri.
**Neden böyle:** Gövdede verilen `authorId` bulunamazsa 404 dönülüyor (mevcut `NotFoundException` yeniden kullanıldı; alternatif 400/422 olabilirdi, bkz. Kararlar). Loan → Book ilişkisi Restrict olduğundan, iade edilmiş geçmişi olan kitabı silmek veritabanı hatasıyla (500) sonuçlanırdı; bu yüzden servis geçmişi açıkça siliyor. Create ve Update validator'ları bilerek ayrı ve tekrarlı: biri değişirse diğeri etkilenmez, okuması kolay.
**Karşılaşılan hatalar:** Yok. Duman testi sırasında logda sadece `Failed to determine the https port for redirect` uyarısı var; `http` profiliyle çalıştırınca HTTPS portu olmadığı için `UseHttpsRedirection` yönlendirme yapamaz. Zararsızdır; `https` profiliyle çalıştırınca kaybolur.
**Bilerek boz:** `BookService.DeleteAsync` içinde `RemoveRange(returnedLoans)` satırını sil ve iade edilmiş ödünç geçmişi olan `DELETE /api/books/1` isteğini gönder. Hangi durum kodu ve logda hangi SQLite hatası (`FOREIGN KEY constraint failed`) çıkıyor?
**Kendini kontrol et:**
- `availableCopies` neden veritabanında bir sütun değil?
- `GetByIdAsync` içinde neden `Include` kullanmadık da `UpdateAsync` içinde kullandık?
- `categoryIds: [1, 2, 2]` gönderilirse ne olur, neden?
**Kendin yazmayı dene:** `BookListItemDto`'ya kategori adlarını virgülle birleştiren bir `Categories` alanı ekle (ipucu: `string.Join` SQL'e çevrilemez; listeyi `IReadOnlyList<string>` olarak döndür).

### Adım 10 — Kitap listesi
**Ne yapıldı:** `GET /api/books` artık sayfalı `PagedResult<BookListItemDto>` dönüyor ve `search`, `authorId`, `categoryId`, `onlyAvailable`, `sortBy`, `sortDirection`, `page`, `pageSize` parametrelerini destekliyor. Geçersiz değerler (`pageSize=100`, `sortBy=price`, `page=0`) 400 dönüyor.
**Yeni kavramlar:**
- *Generic record* (`Common/Pagination/PagedResult.cs`): `PagedResult<T>` her tür liste için tekrar kullanılabilir. `TotalPages` saklanmaz, `TotalCount` ve `PageSize`'dan hesaplanır ama JSON'a yazılır.
- *Query string'den nesne bağlama* (`[FromQuery] BookQueryParameters`): ASP.NET Core `?sortBy=year&page=2` değerlerini aynı adlı özelliklere (harf büyüklüğüne bakmadan) yazar. Varsayılan değerler (`Page = 1`) parametre gelmezse kullanılır.
- *Kalıtımlı parametre ve validator* (`PagingParameters`, `PagingParametersValidator`): Sayfalama kuralları ortak bir sınıfta. `BookQueryParametersValidator` bunları `Include(...)` ile kendi kurallarına ekler. Adım 13'teki ödünç listesi de aynısını kullanacak.
- *Koşullu sorgu kurma* (`BookService.GetPagedAsync`): `IQueryable`'a sadece gelen parametreler için `Where` eklenir. Sorgu ancak `CountAsync`/`ToListAsync` çağrılınca SQL'e çevrilip çalışır (deferred execution). Sonuçta tek bir `WHERE ... AND ...` cümlesi oluşur.
- *EF.Functions.Like*: SQL `LIKE` operatörü. `string.Contains` SQLite'ta büyük/küçük harf duyarlı `instr` fonksiyonuna çevrilir; `LIKE` ise ASCII harflerde duyarsızdır (`?search=KAR` → "Kar").
- *Skip/Take*: `OFFSET`/`LIMIT` olur. Sıralama olmadan sayfalama tutarsızdır; aynı değere sahip kayıtlar için `ThenBy(b => b.Id)` sırayı sabitler.
**Neden böyle:** Sayfa boyutu 50 ile sınırlandı ki tek istekle tüm tablo çekilemesin. Sınırı aşan değer sessizce 50'ye düşürülmek yerine 400 dönüyor; istemci hatasını fark etsin. `onlyAvailable` filtresi de müsait kopya hesabını SQL'de yapıyor; kitaplar belleğe alınıp C#'ta filtrelenmiyor.
**Karşılaşılan hatalar:** Konsolda `İçimizdeki Şeytan` başlığı bozuk görünüyordu. Yanıtın baytlarına (`od -c`) bakınca `Ş` için doğru UTF-8 baytlarının (`C5 9E`) geldiği görüldü. Sorun API'de değil, Windows konsolunun kod sayfasındaydı.
**Bilerek boz:** `PaginationExtensions` içinde önce `Skip/Take` yapıp sonra `CountAsync` çağır (yani sayfanın içindeki kayıtları say). `?pageSize=3` ile `totalCount` ne olur?
**Kendini kontrol et:**
- `ToPagedResultAsync` kaç SQL sorgusu çalıştırır, neden?
- `?page=99` neden hata değil de boş `items` döner?
- Arama için `Contains` yerine neden `EF.Functions.Like` seçildi?
**Kendin yazmayı dene:** `minYear` ve `maxYear` filtrelerini ekle; validator'a `minYear <= maxYear` kuralını yaz.

### Adım 11 — Members
**Ne yapıldı:** Üye CRUD'u Adım 6–9 kalıbıyla yazıldı. E-posta biçimi (`EmailAddress`) ve isteğe bağlı telefon biçimi doğrulanıyor. Aynı e-posta (harf büyüklüğü farklı olsa bile) 409, aktif ödüncü olan üyeyi silmek 409 dönüyor. Yanıtta üyenin `activeLoanCount` değeri de var.
**Yeni kavramlar:**
- *Sunucunun belirlediği alan* (`CreatedAt`): İstek DTO'sunda yok; servis `TimeProvider.GetUtcNow()` ile doldurur. İstemci kayıt tarihini değiştiremez. Bu, DTO'ların entity'den ayrı olmasının bir faydası daha: istemci sadece izin verdiğimiz alanları gönderebilir (overposting koruması).
- *Koşullu kural* (`When`) (`Validators/CreateMemberRequestValidator.cs`): Telefon boşsa biçim kuralı hiç çalışmaz; verildiyse `^\+?\d{10,15}$` biçimine uymalıdır.
- *Servise TimeProvider enjekte etmek* (`MemberService`): Adım 7'de DI'a kaydedilen `TimeProvider` burada ilk kez bir serviste kullanıldı. Adım 14'te testler bunu `FakeTimeProvider` ile değiştirecek.
**Neden böyle:** E-posta benzersizliği kategori adıyla aynı biçimde çözüldü: serviste `AnyAsync` ile kontrol, veritabanında `NOCASE` + unique index. Silme davranışı kitapla tutarlı: aktif ödünç varsa 409, sadece iade edilmiş geçmiş varsa geçmişle birlikte silinir.
**Karşılaşılan hatalar:** Yok.
**Bilerek boz:** `CreateMemberRequest`'e `DateTime CreatedAt` ekle ve `ToEntity` içinde istekten gelen değeri kullan. Sonra `"createdAt": "1990-01-01T00:00:00Z"` gönder. Neden bu bir güvenlik ya da veri bütünlüğü sorunu?
**Kendini kontrol et:**
- `CreatedAt` için neden `DateTime.UtcNow` yerine `TimeProvider` kullanılıyor?
- `When(...)` olmasaydı telefonsuz bir üye eklenebilir miydi?
- `AYSE.YILMAZ@example.com` neden `ayse.yilmaz@example.com` ile çakışıyor?

### Adım 12 — Ödünç ve iade
**Ne yapıldı:** `POST /api/loans` (ödünç ver), `POST /api/loans/{id}/return` (iade al) ve `CreatedAtAction` için gereken `GET /api/loans/{id}` yazıldı. İş kuralları 1–6 `LoanService`'te; kural 7 ve 9 önceki adımlarda kitap/üye servislerinde vardı. Her ihlal 409 dönüyor. Seed artık DI'daki `TimeProvider`'ı kullanıyor.
**Yeni kavramlar:**
- *Anonim tip projeksiyonu* (`LoanService.CreateAsync`): `Select(b => new { b.StockCount, ActiveLoanCount = ... })` sadece kural için gereken iki değeri tek sorguda getirir; DTO tanımlamaya gerek yok çünkü metodun dışına çıkmıyor.
- *Bir sorgu, birden çok kural*: Üyenin aktif ödünçleri (`BookId`, `DueDate`) bir kez okunur; kural 2 (sayı), 4 (aynı kitap) ve 5 (gecikme) bu küçük liste üzerinde bellekte kontrol edilir. Üç ayrı `AnyAsync`/`CountAsync` yerine tek sorgu.
- *Adlandırılmış sabitler* (`LoanPeriodDays = 14`, `MaxActiveLoansPerMember = 3`): Kod `AddDays(14)` yerine `AddDays(LoanPeriodDays)` diye okunur ve hata mesajı da aynı sabitten üretilir. `public` oldukları için testler de bu sabitleri kullanabilir.
- *Eylem endpoint'i* (`POST /api/loans/{id}/return`): "İade et" bir CRUD işlemi değil, bir eylemdir. REST'te bunu alt kaynak adresine `POST` olarak modellemek yaygındır. `PUT /api/loans/{id}` ile `returnDate` göndermek de mümkündü ama istemciye tarih yazdırmak istemedik.
- *Tek "şimdi"* (`var now = ...`): İstek boyunca saat bir kez okunur. Kural kontrolü ve `LoanDate` aynı ana dayanır.
**Neden böyle:** Kuralların kontrol sırası, en anlamlı hata mesajını verecek şekilde seçildi: önce kayıtların varlığı (404), sonra üyeyle ilgili engeller (gecikme, limit, aynı kitap), en son kitabın müsaitliği. Örneğin gecikmiş üyeye "kitap müsait değil" demek yanıltıcı olurdu. İade 204 dönüyor çünkü CLAUDE.md'ye göre mevcut kaydı güncelleyen işlemler 204; güncel hali `GET /api/loans/{id}` ile okunabilir. `GET /api/loans/{id}` aslında Adım 13'ün listesindeydi ama `CreatedAtAction` ona ihtiyaç duyduğu için burada yazıldı (bkz. Kararlar).
**Karşılaşılan hatalar:** Yok. Adım 2'deki `UtcDateTimeConverter`'ın `DateTime?` (`ReturnDate`) için de çalıştığı burada doğrulandı; yanıtta `"returnDate": "...Z"` görülüyor.
**Bilerek boz:** Kural 5'in `if` bloğunu kural 3'ün altına taşı. Gecikmiş ödüncü olan üye, müsait kopyası olmayan bir kitabı istediğinde hangi mesajı alır? Hangisi kullanıcı için daha doğru?
**Kendini kontrol et:**
- `DueDate < now` karşılaştırmasında neden her iki taraf da UTC olmalı?
- Kural 3'te neden `book.StockCount <= 0` değil de `StockCount - ActiveLoanCount <= 0` kontrol ediliyor?
- `memberActiveLoans` listesini bellekte kontrol etmek neden burada N+1 sayılmaz?
**Kendin yazmayı dene:** `POST /api/loans/{id}/extend` endpoint'i ekle: gecikmemiş aktif ödüncün `DueDate`'ini 7 gün uzatsın; gecikmişse 409 dönsün.

### Adım 13 — Ödünç sorguları
**Ne yapıldı:** `GET /api/loans` sayfalı olarak ve `status`, `memberId`, `bookId` filtreleriyle eklendi. `GET /api/members/{id}/loans` üyenin tüm ödünçlerini döndürüyor. Her ödünç yanıtında hesaplanan `status` alanı (`Active` / `Overdue` / `Returned`) var.
**Yeni kavramlar:**
- *Hesaplanan durum ve CASE WHEN* (`Mappings/LoanMappings.cs`): `l.ReturnDate != null ? Returned : l.DueDate < now ? Overdue : Active` ifadesi SQL'de `CASE WHEN` olur. Durum hiçbir yerde saklanmadığı için zaman geçtikçe kendiliğinden doğru kalır: dün `Active` olan ödünç, son tarih geçince sorguda `Overdue` görünür.
- *Parametreli projeksiyon* (`SelectDto(this IQueryable<Loan> query, DateTime now)`): "Şu an" dışarıdan verilir ve sorguya SQL parametresi olarak gider. Projeksiyonun içinde `DateTime.UtcNow` yazsaydık hem test edilemezdi hem de SQLite'ın saatine bağımlı olurdu.
- *Filtre ile projeksiyonun tutarlılığı* (`WhereStatus`): Durum filtresi, `SelectDto`'daki hesapla birebir aynı koşulları kullanır ve iki metot yan yana durur; birini değiştiren diğerini de görür.
- *JsonStringEnumConverter* (`Program.cs`): Enum'lar JSON'da `2` yerine `"Returned"` olarak yazılır. Query string'de `?status=overdue` gibi harf büyüklüğünden bağımsız değerler de kabul edilir.
- *Alt kaynak endpoint'i* (`GET /api/members/{id}/loans`): "Bir üyenin ödünçleri" için REST'te yaygın adresleme. Üye yoksa boş liste değil 404 döner.
**Neden böyle:** `memberId` filtresi `GET /api/loans` üzerinde de var. `GET /api/members/{id}/loans` sayfasız ve üyenin varlığını kontrol eden, okunması kolay bir kısayol. Bir üyenin ödünç sayısı küçük olduğu için sayfalama gereksiz görüldü. Not: `Active`, "iade edilmemiş ve gecikmemiş" demektir. Kural 2'deki "aktif ödünç" ise iade edilmemiş tüm ödünçleri (gecikmişler dahil) sayar.
**Karşılaşılan hatalar:** Yok. `?status=Lost` veya `?status=7` gibi değerler model binding aşamasında 400 alır (FluentValidation'a ulaşmaz). Validator'daki `IsInEnum` ikinci bir güvenlik ağıdır.
**Bilerek boz:** `LoanMappings.SelectDto` içinde `l.DueDate < now` yerine `l.DueDate < DateTime.UtcNow` yaz. Uygulama çalışır mı? Adım 14'te `FakeTimeProvider` ile zamanı 30 gün ileri saran bir test yazınca ne olur?
**Kendini kontrol et:**
- Durumu veritabanında bir sütunda saklasaydık, ödünç gecikmeye düştüğünde o sütunu kim güncelleyecekti?
- `GET /api/members/3/loans` ile `GET /api/members/99/loans` neden farklı yanıt veriyor?
- `WhereStatus` içindeki `_ =>` dalı hangi durumu karşılıyor?

### Adım 14 — Unit testler
**Ne yapıldı:** `KutuphaneApi.Tests` xUnit projesi oluşturulup `.slnx`'e eklendi. `Unit/TestDatabase.cs` her test için bellekte bir SQLite veritabanı açıyor. `LoanServiceTests` kural 1–6'yı, `DeleteAndStockRuleTests` kural 7, 8 ve 9'u test ediyor; toplam 18 test yeşil.
**Yeni kavramlar:**
- *xUnit `[Fact]` ve test sınıfı yaşam döngüsü*: xUnit her test metodu için sınıfı yeniden oluşturur, bittiğinde `Dispose` çağırır. Bu yüzden kurucuda açılan veritabanı her test için temizdir ve testler birbirini etkilemez.
- *SQLite in-memory* (`TestDatabase`): `DataSource=:memory:` veritabanı sadece bağlantı açıkken yaşar; bağlantıyı alan olarak tutup testin sonunda kapatıyoruz. Gerçek SQL çalıştığı için `Count`, `CASE WHEN` ve foreign key gibi şeyler de test edilmiş olur (mock bunları yakalayamazdı).
- *EnsureCreated*: Migration geçmişine bakmadan güncel modelden tabloları oluşturur. Test için hızlı ve yeterli; gerçek veritabanında migration kullanılır.
- *Ayrı DbContext'ler* (`_db.CreateContext()`): Veri bir context ile eklenip servis başka bir context ile çalıştırılır. Aynı context kullanılsaydı EF Core bellekteki nesneleri döndürebilir ve "veritabanında gerçekten ne var" sorusunu atlayabilirdi.
- *FakeTimeProvider*: `_time.Advance(TimeSpan.FromDays(15))` ile saat ileri sarılır. Kural 5 (gecikme) ve hesaplanan durum testleri, gerçekten 15 gün beklemeden bu sayede yazılabildi. Adım 7'den beri `DateTime.UtcNow` yerine `TimeProvider` kullanmamızın karşılığı burada görülüyor.
- *Sınır testleri*: `CreateAsync_OnDueDate_LoanIsNotOverdue` ve `BookUpdate_StockEqualToActiveLoanCount_Succeeds` kuralın sınırını (`<` ile `<=` farkını) sabitler.
- *Test adlandırma*: `Metot_Durum_BeklenenSonuç` kalıbı. Test kırıldığında adı neyin bozulduğunu söyler.
**Neden böyle:** CLAUDE.md'ye uygun olarak mock kütüphanesi yok; servisler gerçek `AppDbContext` ve gerçek validator'larla çalışıyor. ISKELET'e göre `LoanService` için her kurala test istendi; kural 7–9 başka servislerde olduğu için ayrı bir sınıfta toplandı.
**Karşılaşılan hatalar:** Testler ilk çalıştırmada geçti. Testin gerçekten bir şey yakaladığından emin olmak için kural 5'in koşulu geçici olarak bozuldu (`DueDate < now.AddYears(-1)`); `CreateAsync_MemberHasOverdueLoan_...` testi kırmızıya döndü, kod geri alınınca tekrar yeşil oldu. Hiç kırmızı görmediğin bir teste güvenme.
**Bilerek boz:** `LoanService.CreateAsync` içinde kural 2'deki `>=` operatörünü `>` yap ve `dotnet test` çalıştır. Hangi test, hangi mesajla kırılıyor?
**Kendini kontrol et:**
- Neden `Moq` ile `AppDbContext`'i taklit etmek yerine gerçek SQLite kullandık?
- `TestDatabase` bağlantıyı neden kurucuda açıp `Dispose`'da kapatıyor?
- `FakeTimeProvider` olmasaydı kural 5'i nasıl test ederdin, sorun ne olurdu?
**Kendin yazmayı dene:** `CategoryService` için "aynı ad farklı harf büyüklüğüyle eklenirse `BusinessRuleException`" testini yaz.

### Adım 15 — Integration testler
**Ne yapıldı:** `Microsoft.AspNetCore.Mvc.Testing` eklendi. `Integration/KutuphaneApiFactory.cs`, API'yi bellekte ayağa kaldırıyor, test veritabanı ve sahte saat kullanıyor. Beş kaynağın her biri için en az bir başarılı ve bir hata yolu testi yazıldı (18 yeni test, toplam 36 test yeşil).
**Yeni kavramlar:**
- *WebApplicationFactory&lt;Program&gt;*: `Program.cs`'i gerçek bir port açmadan çalıştırır; `CreateClient()` ile alınan `HttpClient` istekleri doğrudan bellekteki sunucuya gönderir. İstek middleware, routing, model binding, controller, servis, EF Core ve SQLite zincirinin tamamından geçer. Unit testlerin yakalayamayacağı hatalar (yanlış route, eksik DI kaydı, yanlış durum kodu, JSON biçimi) burada yakalanır.
- *Ortam (environment) değiştirme* (`UseEnvironment("Testing")`): `Program.cs`'teki `IsDevelopment()` bloğu çalışmaz, yani seed verisi ve dosya veritabanı devreye girmez. Her test boş bir veritabanıyla başlar.
- *Ayar ezme* (`UseSetting("ConnectionStrings:DefaultConnection", ...)`): `appsettings.json`'daki bağlantı cümlesi test için değiştirilir. Uygulama kodu değişmez; bağlantı cümlesini koda gömmemenin faydası burada görülür.
- *Paylaşımlı in-memory SQLite* (`file:test-{guid}?mode=memory&cache=shared`): API'nin her istekte açtığı bağlantılar aynı bellek veritabanını görür. Factory'deki `_keepAliveConnection` açık kaldıkça veritabanı yaşar. Guid sayesinde her testin veritabanı ayrıdır.
- *ConfigureTestServices*: Uygulamanın DI kayıtlarından sonra çalışır. `TimeProvider` burada `FakeTimeProvider` ile değiştirildi; test `Factory.Time.Advance(...)` ile API'nin saatini ileri sarabiliyor (`LoansEndpointTests.Post_MemberWithOverdueLoan_Returns409`).
- *Program sınıfına erişim*: .NET 10'da top-level `Program` sınıfı testlerden görülebildiği için eski projelerdeki `public partial class Program;` satırına gerek kalmadı.
**Neden böyle:** Her test sınıfı örneği kendi factory'sini oluşturuyor (`IntegrationTestBase`). `IClassFixture` ile tek factory paylaşmak daha hızlı olurdu ama testler birbirinin verisini görür ve sıraya bağımlı hale gelebilirdi. 18 test yaklaşık 1–2 saniyede bittiği için izolasyon tercih edildi. Test verisi doğrudan veritabanına değil API üzerinden (`CreateAuthorAsync` vb.) ekleniyor; böylece testler gerçek bir istemcinin yaşayacağı akışı izliyor.
**Karşılaşılan hatalar:** Testler ilk çalıştırmada geçti. Testlerin dosya veritabanına yazmadığını doğrulamak için `kutuphane.db` silinip testler tekrar çalıştırıldı; dosya yeniden oluşmadı, yani bağlantı cümlesi ezme işlemi çalışıyor.
**Bilerek boz:** `KutuphaneApiFactory` içindeki `UseEnvironment("Testing")` satırını sil ve testleri çalıştır. Hangi testler kırılıyor ve neden (ipucu: seed verisi ve `kutuphane.db`)?
**Kendini kontrol et:**
- Unit test ile integration test arasındaki fark nedir? Bu projede hangisi hangi hatayı yakalar?
- `_keepAliveConnection` kapatılsaydı ne olurdu?
- Neden testte `DateTime.UtcNow` ile karşılaştırma yapmak yerine `Factory.Time.GetUtcNow()` kullanıldı?
**Kendin yazmayı dene:** `GET /api/loans?memberId=...&status=Returned` için bir integration test yaz.

## Son Rapor
<!-- Adım 16'da yazılır -->
