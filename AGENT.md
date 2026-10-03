# AGENT — Otonom Geliştirme Döngüsü

## Rolün

Sen bu projenin geliştiricisisin. `ISKELET.md` dosyasındaki adımları sırayla, **kullanıcıya geri dönmeden** tamamlarsın. Kullanıcı sonuçları `NOTLAR.md` ve commit geçmişi üzerinden takip edip öğrenecek.

## Otonomi İlkeleri

- Kullanıcıya soru sorma, onay bekleme, "devam edeyim mi?" deme.
- Belirsiz bir durumda `CLAUDE.md` ve `ISKELET.md` kurallarına uyan **en basit makul** kararı ver, `NOTLAR.md` → **Kararlar** bölümüne bir satırla gerekçesini yaz ve devam et.
- `CLAUDE.md` içindeki sınırları ve yasakları otonomi gerekçesiyle asla esnetme.
- Durman gereken tek durumlar: tüm adımlar bitti, ya da "Durma Koşulları" bölümündeki durum oluştu.

## Oturum Başlangıcı

1. `NOTLAR.md` varsa **İlerleme** listesini oku ve işaretlenmemiş ilk adımdan devam et.
2. `git status` ile yarım kalmış değişiklik var mı bak. Varsa önce o adımı tamamla ya da `git restore .` ile son commit'e dön ve adımı baştan yap.
3. Yoksa Adım 0'dan başla.

---

## Döngü

Her adım için aşağıdaki döngüyü uygula. Bir adım bitince sıradaki adımın **1. TASARLA** aşamasına dön.

```
1. TASARLA → 2. UYGULA → 3. KONTROL ET ──hata──→ 4. DÜZELT ─┐
                              ↑                            │
                              └────────────────────────────┘
                              │ temiz
                              ↓
                    5. GÖZDEN GEÇİR ──sorun──→ 4. DÜZELT
                              │ temiz
                              ↓
                  6. KAYDET → 7. ÖĞRET → sonraki adımın 1. TASARLA'sı
```

### 1. TASARLA
- `ISKELET.md` içinde adımın "Yapılacaklar" ve "Bitti sayılır" kısmını oku.
- Oluşturulacak ve değişecek dosyaları, kullanılacak kavramları zihninde listele.
- Mevcut kodla tutarlılığı kontrol et: önceki adımlarda kurulan kalıpları (DTO adlandırma, servis yapısı, mapping biçimi) aynen sürdür.
- Tasarım `ISKELET.md` ile çelişiyorsa `ISKELET.md` kazanır.

### 2. UYGULA
- Sadece o adımın kapsamındaki kodu yaz. Sonraki adımların işini öne çekme.
- Paket eklerken `dotnet add <proje> package <paket>` kullan; `.csproj` dosyasını elle düzenleyerek paket ekleme.
- Yeni endpoint'ler için `KutuphaneApi.http` dosyasına örnek istek ekle.

### 3. KONTROL ET
Sırasıyla çalıştır (komutları tek tek çalıştır, `&&` ile zincirleme; Windows PowerShell'de çalışmayabilir):

1. `dotnet build KutuphaneApi.slnx` → **0 hata, 0 uyarı** olmalı.
2. Test projesi varsa: `dotnet test KutuphaneApi.slnx` → tüm testler geçmeli.
3. Endpoint içeren adımlarda duman testi: uygulamayı **arka planda** başlat, `curl` ile hem başarılı hem hatalı birer istek at, durum kodlarını ve yanıt gövdesini kontrol et, ardından işlemi mutlaka durdur. Arka planda çalıştırma mümkün değilse bu kontrolü Adım 14–15'teki testlere bırak ve `NOTLAR.md`'ye not düş.
4. Adımın "Bitti sayılır" koşulu gerçekten sağlandı mı? Kanıtı komut çıktısından gör, varsayma.

### 4. DÜZELT
- Hata mesajının tamamını oku. Belirtiyi değil **kök nedeni** bul.
- En küçük doğru düzeltmeyi yap ve **3. KONTROL ET**'e dön.
- Asla yapma: uyarı bastırmak, testi silmek veya atlamak (`Skip`), testi hatalı koda uyacak şekilde gevşetmek, hatayı boş `catch` ile yutmak, `CLAUDE.md`'de yasaklanan bir pakete kaçmak.
- Aynı hata 5 denemede çözülmezse farklı bir yaklaşım dene. Bunu `NOTLAR.md` → **Kararlar**'a yaz.

### 5. GÖZDEN GEÇİR
Bu adımda yazdığın kodu aşağıdaki listeye göre kendin incele. Sorun bulursan **4. DÜZELT**'e dön.

- [ ] Controller entity döndürüyor mu? (Döndürmemeli.)
- [ ] Okuma sorgularında `AsNoTracking()` ve `Select` projeksiyonu var mı? Döngü içinde sorgu (N+1) var mı?
- [ ] Tüm veritabanı çağrıları `async` ve `CancellationToken` iletiliyor mu?
- [ ] Durum kodları `CLAUDE.md`'deki kurallara uygun mu? (201, 204, 400, 404, 409)
- [ ] İş kuralları serviste mi? Controller'da iş mantığı kaldı mı?
- [ ] Bağlantı cümlesi veya sabit değer koda gömülmüş mü?
- [ ] Adlandırma önceki adımlarla tutarlı mı? Kullanılmayan `using`, ölü kod var mı?
- [ ] Gereksiz soyutlama eklendi mi? Kullanıcının okuyup anlayabileceği kadar sade mi?
- [ ] Yeni kavramın ilk kullanıldığı yerde kısa Türkçe açıklama yorumu var mı?

### 6. KAYDET
- `git add -A`
- `git commit -m "Adım N: <kısa açıklama>"`
- `NOTLAR.md` → **İlerleme** listesinde adımı işaretle (bu değişiklik aynı commit'e girsin).

### 7. ÖĞRET
`NOTLAR.md` → **Adım Notları** bölümüne aşağıdaki şablonla kayıt ekle. Kullanıcı projeyi bu notlardan öğrenecek; bu aşamayı asla atlama.

```markdown
### Adım N — <başlık>
**Ne yapıldı:** 2-3 cümle.
**Yeni kavramlar:** Her kavram için 1-2 cümle açıklama ve hangi dosyada görüleceği.
**Neden böyle:** Alınan tasarım kararları ve alternatifleri.
**Karşılaşılan hatalar:** Varsa hata, kök nedeni ve çözümü. (Kullanıcı için en öğretici kısım.)
**Bilerek boz:** Kullanıcının deneyebileceği bir değişiklik ve ne olacağı sorusu.
**Kendini kontrol et:** 2-3 soru. Cevapları yazma.
**Kendin yazmayı dene:** (İsteğe bağlı) Kullanıcının bu kalıpla kendi yazabileceği küçük bir ek.
```

---

## Durma Koşulları

Kesintisiz çalış; sadece şu durumlarda dur:

1. **Tüm adımlar tamamlandı.** Adım 16'yı bitir ve son raporu yaz.
2. **Çözülemeyen sorun.** Aynı adımda üç farklı yaklaşım da başarısız olduysa:
   - `git restore .` ile adımı son temiz commit'e geri al.
   - `NOTLAR.md` → **Çözülemeyen Sorunlar** bölümüne hatayı, denenen yaklaşımları ve tahmini nedeni yaz.
   - Sonraki adım bu adıma bağımlı değilse onunla devam et. Bağımlıysa son raporu yaz ve dur.

---

## NOTLAR.md Başlangıç Şablonu

Adım 0'da bu yapıyla oluştur:

```markdown
# KutuphaneApi — Öğrenme Notları

## İlerleme
- [ ] Adım 0 — Hazırlık
- [ ] Adım 1 — Entity'ler
(... ISKELET.md'deki tüm adımlar ...)

## Kararlar
<!-- Belirsiz durumlarda verilen kararlar, tek satır gerekçeyle -->

## Çözülemeyen Sorunlar
<!-- Boşsa "Yok" yaz -->

## Adım Notları
<!-- Her adım sonunda 7. ÖĞRET şablonuyla eklenir -->

## Son Rapor
<!-- Adım 16'da yazılır: ne yapıldı, nasıl çalıştırılır, kullanıcı için önerilen okuma sırası -->
```

## Son Rapor İçeriği

Adım 16'da `NOTLAR.md` → **Son Rapor** bölümüne:
- Projenin nasıl çalıştırılacağı ve test edileceği (komutlarla).
- Kullanıcının kodu okuması için önerilen dosya sırası (örn. Entity → DbContext → Service → Controller).
- Tamamlanamayan veya ödün verilen noktalar.
- Bir sonraki projede (Blog API: JWT, Clean Architecture) bu projeden taşınacak kalıplar.