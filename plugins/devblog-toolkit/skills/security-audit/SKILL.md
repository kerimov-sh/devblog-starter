---
name: security-audit
description: DevBlog Starter reposunda genel veya hedefli bir güvenlik denetimi gerektiğinde kullan. Kullanıcı "güvenlik taraması yap", "security audit", "OWASP kontrolü yap", "bu endpoint güvenli mi", "penetrasyon testi", "açıkları bul" dediğinde, ya da yeni bir endpoint/auth akışı/frontend formu eklendiğinde bu skill'i çalıştır. Backend (ASP.NET Core Minimal API, JWT auth, EF Core), frontend (Angular — token saklama, XSS yüzeyi) ve bağımlılık (NuGet/npm) güvenliğini OWASP Top 10 eksenli, bu reponun gerçek koduna dayalı somut kontrol listesiyle uçtan uca ya da belirli bir endpoint için tarar; her bulgu için ciddiyet seviyesi ve düzeltme önerisi sunar, onay almadan hiçbir düzeltmeyi otomatik uygulamaz.
---

# Güvenlik Denetimi (DevBlog Starter)

Bu skill iki modda çalışır — hangisinin istendiği net değilse kullanıcıya sor:

- **Uçtan uca (tam) tarama**: "genel güvenlik taraması", "OWASP kontrolü", "tüm sistemi tara" gibi isteklerde. Backend + frontend + bağımlılıkların tamamını, aşağıdaki tüm kategorileri kullanarak tarar.
- **Hedefli (endpoint) tarama**: "şu endpoint'i incele", "/posts route'unu tara" gibi belirli bir hedef verildiğinde. Sadece o özelliğin tüm zincirini (Endpoint → Service → Repository, varsa ilgili frontend service/component) tarar; global kategorilerden (CORS, güvenlik header'ları, bağımlılık taraması gibi) sadece doğrudan ilgili olanları rapora dahil et.

## Bilinen borçlarla ilişkisi

CLAUDE.md'nin "Bilinen Borçlar" bölümü bu projede bilerek bırakılmış bazı zaafları (Base64 parola "hash"leme, hardcoded JWT secret, `AllowAnyOrigin/Method/Header` CORS) zaten listeliyor. Bu skill bunları **"zaten dokümante edilmiş, o yüzden önemsiz" diyerek atlamaz** — bir güvenlik denetiminin amacı gerçek riski göstermektir, dokümantasyon durumu riski azaltmaz. Bulgu olarak normal ciddiyetiyle (genelde Critical/High) raporla, ama notunda "CLAUDE.md'de bilinen/kasıtlı borç olarak işaretli" diye belirt. Diğer skill'lerle (`ef-migration`, `seo-settings`) tutarlı şekilde: **bulguyu raporla ve düzeltme öner, ama kullanıcı açıkça onaylamadan hiçbir kod/config değişikliği uygulama.**

## Kontrol Kategorileri

Aşağıdaki kategoriler bu reponun mevcut koduna göre yazıldı — her taramada dosyaların **o anki** halini oku, burada anlatılanları geçmiş bir anlık görüntü olarak değil, "nereye bakılacağının" bir haritası olarak kullan; kod zamanla değişmiş olabilir.

### A. Kimlik doğrulama ve parola güvenliği (OWASP A02/A07)
- `AuthEndpoint.cs` içinde parola karşılaştırması gerçek bir hash algoritması (BCrypt/Argon2/PBKDF2) ile mi yapılıyor, yoksa `Convert.ToBase64String` gibi geri döndürülebilir bir kodlama mı kullanılıyor (bkz. `AuthEndpoint.cs`, `DataSeeder.cs`)? Base64 hash değildir — tuzsuz ve tersine çevrilebilir, bunu bulgularda net şekilde belirt.
- JWT imzalama secret'ı: hardcoded string mi, `appsettings`/secret manager/env var'dan mı okunuyor? Aynı secret birden fazla dosyada elle senkronize mi tutuluyor (bkz. `Program.cs` ve `AuthEndpoint.cs`) — bu bir bakım/güvenlik riskidir (biri güncellenip diğeri unutulursa token doğrulaması bozulur ya da eski secret sızarsa fark edilmez).
- `TokenValidationParameters` içinde `ValidateIssuer`/`ValidateAudience`/`ValidateLifetime` `false` mu bırakılmış? Bu, token'ın amaçlanan hedef dışında da kabul edilebileceği anlamına gelir.
- `/auth/login` üzerinde rate limiting / brute-force koruması var mı (başarısız deneme sayısı sınırlandırılıyor mu)? Şu an böyle bir mekanizma bulunmuyor — bunu bir bulgu olarak işaretle.
- Frontend'de token nasıl saklanıyor (`AuthService`, `authInterceptor`) — `localStorage` mı, `httpOnly` cookie mi? `localStorage`, XSS ile token çalınmasına açıktır (bkz. B. maddesi ile birlikte değerlendir).

### B. Yetkilendirme / Erişim kontrolü (OWASP A01)
- Her endpoint için `.RequireAuthorization()` (Minimal API) ya da `[Authorize]` var mı, ve bu endpoint'in etkisiyle (okuma mı, yazma mı, kimin verisini etkiliyor) tutarlı mı? Özellikle: tüm yorumları döndüren `GET /comments` ve yeni yorum ekleyen `POST /posts/{slug}/comments` şu an yetkilendirmesiz — bunun bilinçli bir tasarım kararı mı (herkes yorum yapabilsin) yoksa bir gözden kaçırma mı olduğunu kullanıcıya sor, kendiliğinden "düzelt" deme.
- IDOR (Insecure Direct Object Reference): bir kullanıcının başka bir kullanıcıya ait kaynağı (post/comment) ID/slug vererek görüntüleyebildiği/değiştirebildiği bir durum var mı? Şu an update/delete endpoint'i olmadığı için risk düşük — ama yeni bir PUT/DELETE endpoint'i eklendiğinde bunu mutlaka kontrol et (kaynağın sahibi mi işlem yapan, yoksa herhangi bir authenticated kullanıcı mı yapabiliyor).
- Rol tabanlı yetkilendirme (`User.Role`, ör. Admin/Author) gerçekten endpoint'lerde kontrol ediliyor mu, yoksa sadece token içinde taşınan ama hiç okunmayan bir claim mi?

### C. Girdi doğrulama / Injection (OWASP A03)
- Ham SQL kullanımı var mı (`FromSqlRaw`, `ExecuteSqlRaw`, `ExecuteSqlInterpolated` içinde string concatenation)? EF Core LINQ sorguları parametreli olduğu için normalde düşük risk — ama her yeni sorguda bunu doğrula.
- DTO/record'larda (`CreatePostRequest`, `CreateCommentRequest`, `LoginRequest`) uzunluk/format sınırı var mı? Şu an `Title`/`Content`/`Tags`/`Slug`/`AuthorName`/`Body` üzerinde hiçbir `MaxLength`/validation attribute'u yok — bu hem depolama istismarına hem de sınırsız payload'a açık kapı bırakıyor.
- `Slug` alanı bir URL segmenti olarak kullanılıyor (`/posts/{slug}`) — oluşturulurken/kabul edilirken karakter kısıtlaması (yalnızca `a-z0-9-` gibi) var mı, yoksa rastgele karakterler mi kabul ediliyor?

### D. Güvenlik yanlış yapılandırması (OWASP A05)
- CORS politikası (`Program.cs`) `AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()` mı? Prod için bu, herhangi bir sitenin API'ye credential'lı istek atabilmesi anlamına gelir.
- `app.UseHttpsRedirection()` çağrısı var mı? Şu an `Program.cs`'de bulunmuyor — HTTP üzerinden token/parola aktarımı mümkün demektir.
- Güvenlik header'ları (`Strict-Transport-Security`, `X-Content-Type-Options`, `X-Frame-Options`/CSP `frame-ancestors`, `Referrer-Policy`) hiç ayarlanmıyor — ekleniyor mu diye kontrol et.
- OpenAPI/Scalar dokümantasyonu (`MapOpenApi`) sadece `Development` ortamında mı açık kalıyor (şu an `if (app.Environment.IsDevelopment())` ile doğru gated) — bu korumanın bozulmadığını her taramada doğrula.
- Global bir exception handling middleware var mı, yoksa unhandled exception'lar stack trace'i doğrudan response'a mı sızdırıyor?

### E. Bilinen zafiyetli bileşenler (OWASP A06)
- Backend: proje dizininde `dotnet list package --vulnerable --include-transitive` çalıştır (bu komut zaten `dotnet ef`/`dotnet build` sırasında NU1903 uyarıları — ör. `Microsoft.OpenApi`, `SQLitePCLRaw.lib.e_sqlite3` — göstermişti; bunları teyit edip rapora ekle).
- Frontend: `devblog-ui` içinde `npm audit` çalıştır, Critical/High bulunanları özellikle öne çıkar.
- Sonuçları CVE/advisory linkiyle birlikte rapora ekle, ciddiyet seviyesini paket yöneticisinin verdiği seviyeyle eşleştir.

### F. Frontend'e özgü riskler
- `innerHTML`, `bypassSecurityTrustHtml`/`bypassSecurityTrustScript`, doğrudan `nativeElement` DOM manipülasyonu kullanımı var mı (XSS yüzeyi)? Şu an projede bulunmuyor — Angular'ın varsayılan interpolation escaping'ine güveniliyor; her yeni component/template eklendiğinde bunun bozulmadığını doğrula.
- JWT `localStorage`'da tutuluyor (bkz. A. maddesi) — bu, XSS olursa token'ın doğrudan çalınabileceği anlamına gelir; `httpOnly` cookie alternatifiyle karşılaştır.
- Bearer token + `Authorization` header kullanıldığı için klasik cookie-tabanlı CSRF riski düşüktür — bunu yanlışlıkla bir zafiyet olarak raporlama; sadece token cookie'ye taşınırsa CSRF token'ının da eklenmesi gerektiğini not düş.
- `environment.ts`/`environment.prod.ts` içinde sızdırılmış bir secret/API key var mı?

### G. Loglama ve izlenebilirlik (OWASP A09)
- Başarısız login denemeleri loglanıyor mu (brute-force tespiti için)? Şu an hiçbir loglama yok.
- Loglarda parola/token gibi hassas veriler açık şekilde yazılıyor mu (EF Core'un debug seviyesinde SQL parametrelerini loglaması dahil)?

## Ciddiyet Seviyeleri

Her bulguyu şu ölçekle etiketle: **Critical** (uzaktan, kimlik doğrulamasız istismar edilebilir + ciddi veri/hesap ele geçirme), **High** (kimlik doğrulamalı ya da sınırlı koşullu ciddi risk), **Medium** (savunma katmanı eksikliği, doğrudan istismar zor ama olası), **Low** (iyi pratik eksikliği, düşük etki), **Info** (bilgi amaçlı, aksiyon gerektirmeyebilir).

## Çıktı Formatı

Her bulgu için:

```
[Severity] <Başlık> — <açıklama> (bkz. <dosya:satır>)
Öneri: <somut düzeltme, gerekirse kod örneğiyle>
Not: <CLAUDE.md'de bilinen borç ise burada belirt>
```

**Hedefli (endpoint) tarama**: bulgular sadece sohbette raporlanır, kullanıcı ayrıca isterse dosyaya kaydedilir.

**Uçtan uca tarama**: bulgular sohbette Critical→Info sıralı bir özet olarak sunulur, ayrıca tam rapor `security-reports/<YYYY-MM-DD>-<konu-slug>.md` altına kaydedilir (dizin yoksa oluştur). Bu dosyaları oluşturmadan önce `.gitignore`'da `security-reports/` girdisinin olduğunu doğrula — yoksa ekle, çünkü bu raporlar istismar edilebilir zafiyet detayları içerir ve repoya commit edilmemelidir.

## Düzeltme uygulama kuralı

Bu skill **rapor + öneri** üretir, otomatik düzeltme uygulamaz. Kullanıcı belirli bir bulgu için "düzelt" derse, o düzeltmeyi ayrı bir onay adımı olarak uygula (ör. `git commit` öncesi onay istendiği gibi) — birden fazla Critical/High bulgu varsa, kullanıcıya hepsini mi yoksa hangilerini önce görmek istediğini sor, sessizce hepsini birden değiştirme.
