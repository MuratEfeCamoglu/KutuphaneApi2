# KutuphaneApi

.NET 10 ve ASP.NET Core ile yazılmış bir **Kütüphane Yönetim REST API'si**. Yazar, kategori, kitap ve üye kayıtlarını, ödünç alma ve iade işlemlerini iş kurallarıyla birlikte yönetir.

Öğrenme amaçlı bir projedir. Her adımın açıklaması, yeni kavramlar ve alıştırmalar [`NOTLAR.md`](NOTLAR.md) dosyasındadır.

## Teknolojiler

- ASP.NET Core 10 (controller tabanlı Web API, minimal hosting)
- EF Core 10 + SQLite
- FluentValidation (servislerde elle çağrılır)
- OpenAPI + Scalar (API arayüzü)
- xUnit, SQLite in-memory, `WebApplicationFactory`, `FakeTimeProvider`

## Çalıştırma

Gereksinim: .NET 10 SDK.

```bash
dotnet tool restore                      # dotnet-ef yerel aracını kurar
dotnet build KutuphaneApi.slnx
dotnet run --project KutuphaneApi        # http://localhost:5041
```

Development ortamında uygulama açılırken migration'lar uygulanır ve `KutuphaneApi/kutuphane.db` örnek veriyle oluşturulur. Veriyi sıfırlamak için bu dosyayı silip uygulamayı yeniden başlatman yeterli.

- API arayüzü: http://localhost:5041/scalar
- OpenAPI dokümanı: http://localhost:5041/openapi/v1.json
- Hazır istekler: [`KutuphaneApi/KutuphaneApi.http`](KutuphaneApi/KutuphaneApi.http) (VS Code REST Client, Visual Studio veya Rider ile çalıştırılabilir)

## Test

```bash
dotnet test KutuphaneApi.slnx
```

- `KutuphaneApi.Tests/Unit`: Servis testleri. Gerçek SQLite in-memory veritabanı kullanılır; iş kurallarının her biri test edilir.
- `KutuphaneApi.Tests/Integration`: API bellekte ayağa kaldırılır ve gerçek HTTP istekleriyle uçtan uca test edilir.

## Migration

```bash
dotnet ef migrations add <Ad> --project KutuphaneApi --output-dir Data/Migrations
dotnet ef database update --project KutuphaneApi
```

## Endpoint'ler

| Kaynak | Endpoint'ler |
|---|---|
| Authors | `GET /api/authors`, `GET /api/authors/{id}`, `POST`, `PUT /{id}`, `DELETE /{id}` |
| Categories | `GET /api/categories`, `GET /{id}`, `POST`, `PUT /{id}`, `DELETE /{id}` |
| Books | `GET /api/books` (sayfalı; `search`, `authorId`, `categoryId`, `onlyAvailable`, `sortBy=title\|year`, `sortDirection=asc\|desc`, `page`, `pageSize` ≤ 50), `GET /{id}`, `POST`, `PUT /{id}`, `DELETE /{id}` |
| Members | `GET /api/members`, `GET /{id}`, `GET /{id}/loans`, `POST`, `PUT /{id}`, `DELETE /{id}` |
| Loans | `GET /api/loans` (sayfalı; `status=Active\|Overdue\|Returned`, `memberId`, `bookId`), `GET /{id}`, `POST` (ödünç ver), `POST /{id}/return` (iade al) |

Durum kodları: oluşturma `201`, güncelleme/silme/iade `204`, doğrulama hatası `400`, bulunamadı `404`, iş kuralı ihlali `409`. Tüm hatalar `application/problem+json` (ProblemDetails) biçimindedir.

## İş Kuralları

1. Ödünç süresi 14 gündür.
2. Bir üyenin aynı anda en fazla 3 iade edilmemiş ödüncü olabilir.
3. Müsait kopyası olmayan kitap ödünç verilemez.
4. Üye aynı kitabı iade etmeden ikinci kez ödünç alamaz.
5. Gecikmiş ödüncü olan üye yeni ödünç alamaz.
6. İade edilmiş ödünç tekrar iade edilemez.
7. İade edilmemiş ödüncü olan kitap veya üye silinemez.
8. Kitabı olan yazar silinemez.
9. Kitap stoğu, o an ödünçte olan kopya sayısının altına düşürülemez.

Müsait kopya sayısı ve ödünç durumu veritabanında saklanmaz, her sorguda hesaplanır.

## Proje Yapısı

```
KutuphaneApi/
├── Controllers/      HTTP katmanı: route, durum kodu
├── Services/         İş kuralları, doğrulama çağrısı, EF Core sorguları
├── Data/             AppDbContext, Fluent API yapılandırmaları, migration'lar, seed
├── Entities/         Tablo karşılıkları
├── Dtos/             API'nin aldığı ve döndürdüğü veri şekilleri (record)
├── Mappings/         Elle yazılmış entity → DTO projeksiyonları
├── Validators/       FluentValidation kuralları
├── Common/           Özel exception'lar, sayfalama
├── Infrastructure/   GlobalExceptionHandler
└── Extensions/       DI kayıtları
KutuphaneApi.Tests/
├── Unit/
└── Integration/
```
