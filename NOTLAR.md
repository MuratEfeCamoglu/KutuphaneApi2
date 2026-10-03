# KutuphaneApi — Öğrenme Notları

## İlerleme
- [x] Adım 0 — Hazırlık
- [x] Adım 1 — Entity'ler
- [ ] Adım 2 — DbContext
- [ ] Adım 3 — Migration ve seed
- [ ] Adım 4 — API dokümantasyonu
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

## Son Rapor
<!-- Adım 16'da yazılır -->
