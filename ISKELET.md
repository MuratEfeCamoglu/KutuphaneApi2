# KutuphaneApi — Proje İskeleti

## 1. Mimari

Tek proje içinde **katmanlı mimari** kullanılır. Katmanlar ayrı projeler değil, aynı proje içindeki klasörlerdir. Bu seçim bilinçlidir: kullanıcı önce katmanların sorumluluklarını öğrenecek, çok projeli Clean Architecture'a bir sonraki projede (Blog API) geçecek.

### İstek Akışı

```
HTTP isteği
   ↓
Middleware pipeline (HTTPS yönlendirme, GlobalExceptionHandler)
   ↓
Controller        → isteği alır, servisi çağırır, HTTP yanıtını oluşturur
   ↓
Service           → doğrulama + iş kuralları + veritabanı işlemleri
   ↓  (Validator)
AppDbContext      → EF Core, LINQ sorgularını SQL'e çevirir
   ↓
SQLite (kutuphane.db)
```

### Katmanların Sorumlulukları

| Katman | Sorumluluk | Yapmaması gereken |
|---|---|---|
| Controllers | Route, HTTP durum kodu, servisi çağırmak | İş kuralı, veritabanı erişimi |
| Services | Doğrulama, iş kuralları, EF Core sorguları, mapping | HTTP'ye özgü işler (`IActionResult` döndürmek) |
| Data | DbContext, entity yapılandırmaları, migration, seed | İş kuralı |
| Entities | Veritabanı tablolarının C# karşılığı | Dışarıya (API yanıtına) açılmak |
| Dtos | API'nin aldığı ve döndürdüğü veri şekilleri | Entity'ye referans vermek |

Hata akışı: Servis bir sorun bulursa özel exception fırlatır (`NotFoundException`, `BusinessRuleException`, `ValidationException`). `GlobalExceptionHandler` bunu yakalar ve uygun durum koduyla `ProblemDetails` yanıtına çevirir.

---

## 2. Klasör Yapısı

```
Deneme/
├── CLAUDE.md
├── ISKELET.md
├── AGENT.md
├── NOTLAR.md                       # Öğrenme notları ve ilerleme (agent yazar)
├── README.md                       # Son adımda yazılır
├── .gitignore
├── .config/dotnet-tools.json       # dotnet-ef yerel aracı
├── KutuphaneApi.slnx
├── WebApplication1/                # DOKUNULMAZ
│
├── KutuphaneApi/
│   ├── Controllers/
│   │   ├── AuthorsController.cs
│   │   ├── CategoriesController.cs
│   │   ├── BooksController.cs
│   │   ├── MembersController.cs
│   │   └── LoansController.cs
│   ├── Services/
│   │   ├── Interfaces/             # IAuthorService, IBookService ...
│   │   └── AuthorService.cs ...
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   ├── Configurations/         # IEntityTypeConfiguration<T> sınıfları
│   │   ├── Migrations/
│   │   └── Seed/                   # Geliştirme ortamı örnek verisi
│   ├── Entities/                   # Author, Category, Book, Member, Loan
│   ├── Dtos/
│   │   ├── Authors/ Categories/ Books/ Members/ Loans/
│   ├── Validators/                 # FluentValidation sınıfları
│   ├── Mappings/                   # Elle yazılmış entity → DTO extension metotları
│   ├── Common/
│   │   ├── Exceptions/             # NotFoundException, BusinessRuleException
│   │   └── Pagination/             # PagedResult<T>, sayfalama parametreleri
│   ├── Infrastructure/
│   │   └── GlobalExceptionHandler.cs
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── KutuphaneApi.http
│
└── KutuphaneApi.Tests/
    ├── Unit/                       # Servis testleri (SQLite in-memory)
    └── Integration/                # WebApplicationFactory ile endpoint testleri
```

---

## 3. Veri Modeli

| Entity | Alanlar | İlişkiler |
|---|---|---|
| **Author** | Id, FirstName, LastName, BirthYear (int?) | 1 yazar → çok kitap |
| **Category** | Id, Name (benzersiz) | Kitaplarla çoka-çok |
| **Book** | Id, Title, Isbn (benzersiz, 13 hane), PublishedYear, StockCount (≥ 0), AuthorId | Yazara bağlı; kategorilerle çoka-çok; çok ödünç |
| **Member** | Id, FirstName, LastName, Email (benzersiz), PhoneNumber (string?), CreatedAt (UTC) | 1 üye → çok ödünç |
| **Loan** | Id, BookId, MemberId, LoanDate, DueDate, ReturnDate (DateTime?) | Kitaba ve üyeye bağlı |

Notlar:
- Kitap–kategori ilişkisi EF Core'un otomatik ara tablosuyla (skip navigation) kurulur; ayrı bir `BookCategory` entity'si yazılmaz.
- Müsait kopya sayısı veritabanında tutulmaz, hesaplanır: `StockCount − iade edilmemiş ödünç sayısı`.
- Ödüncün durumu da hesaplanır: `ReturnDate` doluysa **Returned**, boşsa ve `DueDate` geçmişse **Overdue**, değilse **Active**.
- Tüm tarihler UTC tutulur.

### İş Kuralları

1. Ödünç süresi 14 gündür: `DueDate = LoanDate + 14 gün`.
2. Bir üyenin aynı anda en fazla **3** aktif (iade edilmemiş) ödüncü olabilir.
3. Müsait kopyası olmayan kitap ödünç verilemez.
4. Bir üye aynı kitabı, iade etmeden ikinci kez ödünç alamaz.
5. Gecikmiş ödüncü olan üye yeni ödünç alamaz.
6. Zaten iade edilmiş bir ödünç tekrar iade edilemez.
7. İade edilmemiş ödüncü olan kitap veya üye silinemez.
8. Kitabı olan yazar silinemez.
9. Kitabın `StockCount` değeri, o anki aktif ödünç sayısının altına düşürülemez.

İhlallerin hepsi `BusinessRuleException` → **409 Conflict** döner. Benzersizlik ihlalleri (ISBN, e-posta, kategori adı) de 409 döner.

---

## 4. API Endpoint'leri

| Kaynak | Endpoint'ler |
|---|---|
| Authors | `GET /api/authors`, `GET /api/authors/{id}`, `POST`, `PUT /{id}`, `DELETE /{id}` |
| Categories | `GET /api/categories`, `GET /{id}`, `POST`, `PUT /{id}`, `DELETE /{id}` |
| Books | `GET /api/books` (sayfalı + filtreli), `GET /{id}` (yazar, kategoriler, müsait kopya ile), `POST`, `PUT /{id}`, `DELETE /{id}` |
| Members | `GET /api/members`, `GET /{id}`, `GET /{id}/loans`, `POST`, `PUT /{id}`, `DELETE /{id}` |
| Loans | `GET /api/loans` (sayfalı, `status`, `memberId`, `bookId` filtreli), `GET /{id}`, `POST` (ödünç ver), `POST /{id}/return` (iade al) |

`GET /api/books` sorgu parametreleri: `search` (başlık veya ISBN içinde arar), `authorId`, `categoryId`, `onlyAvailable`, `sortBy` (`title` | `year`), `sortDirection` (`asc` | `desc`), `page` (varsayılan 1), `pageSize` (varsayılan 10, en fazla 50).

Sayfalı yanıt şekli: `items`, `page`, `pageSize`, `totalCount`, `totalPages`.

---

## 5. NuGet Paketleri

### KutuphaneApi
| Paket | Neden |
|---|---|
| `Microsoft.AspNetCore.OpenApi` | Şablonla gelir. API'nin OpenAPI dokümanını üretir. |
| `Scalar.AspNetCore` | OpenAPI dokümanını tarayıcıda test edilebilir bir arayüzle gösterir (.NET 9'dan beri şablonda arayüz yok). |
| `Microsoft.EntityFrameworkCore.Sqlite` | EF Core'un SQLite sağlayıcısı. Kurulum gerektirmeyen dosya tabanlı veritabanı. |
| `Microsoft.EntityFrameworkCore.Design` | Migration oluşturmak için tasarım zamanı araçları. |
| `FluentValidation.DependencyInjectionExtensions` | Doğrulama kurallarını okunabilir sınıflarda yazmak ve DI'a toplu kaydetmek için. |

### KutuphaneApi.Tests
| Paket | Neden |
|---|---|
| xUnit şablon paketleri | `dotnet new xunit` ile gelir (xunit, test SDK, runner). |
| `Microsoft.AspNetCore.Mvc.Testing` | `WebApplicationFactory` ile API'yi bellekte ayağa kaldırıp gerçek HTTP istekleriyle test etmek için. |
| `Microsoft.Extensions.TimeProvider.Testing` | `FakeTimeProvider` ile zamanı ileri sarıp gecikme kurallarını test etmek için. |

### Araç
| Araç | Neden |
|---|---|
| `dotnet-ef` (yerel araç) | `dotnet ef migrations add` ve `dotnet ef database update` komutları için. |

---

## 6. Adımlar

Her adım küçük, bağımsız test edilebilir ve tek commit'tir. "Bitti" koşulları sağlanmadan sonraki adıma geçilmez.

| # | Adım | Yapılacaklar | Bitti sayılır |
|---|---|---|---|
| 0 | Hazırlık | `KutuphaneApi` yoksa `dotnet new webapi --use-controllers` ile oluştur. `git init`, `dotnet new gitignore` (+ `WebApplication1/` ve `*.db` satırları), `.slnx` oluşturup projeyi ekle, WeatherForecast örneğini sil, `NOTLAR.md` oluştur. | Build temiz, ilk commit atıldı |
| 1 | Entity'ler | Beş entity ve navigation property'ler | Build temiz |
| 2 | DbContext | `AppDbContext`, `Configurations/` altında Fluent API yapılandırmaları (benzersiz index'ler, uzunluklar, ilişkiler, silme davranışları), bağlantı cümlesi, DI kaydı | Build temiz |
| 3 | Migration ve seed | Yerel `dotnet-ef` aracı, ilk migration, Development ortamında başlangıçta migration uygulama ve örnek veri | Uygulama açılınca `kutuphane.db` oluşuyor, seed verisi var |
| 4 | API dokümantasyonu | Scalar'ı Development ortamında aç | `/scalar` adresi çalışıyor |
| 5 | Hata altyapısı | `NotFoundException`, `BusinessRuleException`, `GlobalExceptionHandler` (`IExceptionHandler` + `AddProblemDetails`) | Build temiz |
| 6 | Authors | DTO'lar, mapping, `IAuthorService`/`AuthorService`, `AuthorsController`, `.http` örnekleri — ilk tam dikey dilim | Beş endpoint çalışıyor |
| 7 | Doğrulama | FluentValidation kurulumu, validator kaydı, `ValidationException` → 400 eşlemesi, Author validator'ları | Geçersiz istek 400 + alan hataları dönüyor |
| 8 | Categories | Adım 6–7 kalıbıyla CRUD + validator | Endpoint'ler çalışıyor, benzersiz ad 409 |
| 9 | Books | CRUD, yazar ve kategori ilişkileri, müsait kopya hesabı, validator'lar | Detayda yazar ve kategoriler görünüyor |
| 10 | Kitap listesi | `PagedResult<T>`, arama, filtre, sıralama | Tüm sorgu parametreleri çalışıyor |
| 11 | Members | CRUD + validator'lar (e-posta formatı ve benzersizliği) | Endpoint'ler çalışıyor |
| 12 | Ödünç ve iade | `TimeProvider` kaydı, `POST /api/loans`, `POST /api/loans/{id}/return`, iş kuralları 1–7 ve 9 | Her kural ihlali 409 dönüyor |
| 13 | Ödünç sorguları | `GET /api/loans` filtreleri, `GET /api/members/{id}/loans`, hesaplanan durum | Active/Overdue/Returned filtreleri doğru |
| 14 | Unit testler | Test projesi, `.slnx`'e ekleme, SQLite in-memory test yardımcısı, `LoanService` için her iş kuralına en az bir test (`FakeTimeProvider` ile gecikme dahil) | `dotnet test` yeşil |
| 15 | Integration testler | `WebApplicationFactory` ile her kaynaktan en az bir mutlu yol ve bir hata yolu testi | `dotnet test` yeşil |
| 16 | Son gözden geçirme | Tüm projeyi `AGENT.md` kontrol listesine göre tara, `README.md` yaz, `NOTLAR.md` son raporu | Build ve testler temiz, rapor yazıldı |
