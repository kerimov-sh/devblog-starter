---
name: uncommitted-review
description: DevBlog Starter reposunda commit edilmemiş değişiklikleri (staged + unstaged + untracked) özetlemek ve olası riskleri işaretlemek için kullan. Kullanıcı "/uncommitted-review" çağırdığında, ya da "commit edilmemiş değişiklikleri özetle", "working tree'de ne var", "bunu commit'lemeden önce riskleri göster", "değişiklikleri gözden geçir" dediğinde tetikle. git status/diff üzerinden dosya/katman bazlı bir özet çıkarır; mimari borç ihlalleri, güvenlik sızıntıları, yanlışlıkla commit edilecek dosyalar, kapsam dışı/karışık değişiklikler ve eksik migration gibi somut riskleri ciddiyet seviyesiyle raporlar. Otomatik düzeltme uygulamaz, sadece rapor üretir.
---

# Uncommitted Review (DevBlog Starter)

Bu skill, henüz commit edilmemiş değişiklikleri (staged, unstaged, untracked) tek bir taramada özetler ve bu değişikliklerin taşıdığı riskleri işaretler. Amaç, kullanıcı `git commit` çalıştırmadan önce "ne değişti" ve "bunda dikkat edilmesi gereken ne var" sorularına hızlı bir cevap vermektir.

## Veri toplama

Rapor yazmadan önce şunları çalıştır:

1. `git status` — hangi dosyalar staged, hangileri unstaged, hangileri untracked.
2. `git diff` (unstaged) ve `git diff --staged` (staged) — gerçek içerik değişikliği.
3. Untracked dosyalar için içeriklerine göz at (özellikle yeni eklenen `.cs`/`.ts` dosyaları, config dosyaları).
4. `git diff --stat` ile büyüklük/kapsamın genel görünümünü al (kaç dosya, kaç satır) — çok büyük ve alakasız bir diff varsa bunu ayrıca not düş.

Hiç commit edilmemiş değişiklik yoksa bunu kısaca belirt ve dur; rapor üretme.

## Özet bölümü

Değişiklikleri bu reponun katmanlarına göre grupla (bkz. CLAUDE.md "Mevcut Mimari"):

- **Backend**: hangi `Endpoint`/`Service`/`Repository`/`Model` dosyaları, hangi davranış eklendi/değişti/kaldırıldı.
- **Frontend**: hangi component/service/route/interceptor etkilendi.
- **Data/Migration**: `AppDbContext`, `Models/`, ya da `Migrations/` altında değişiklik var mı.
- **Config/Diğer**: `Program.cs`, `appsettings*`, `environment*.ts`, `.csproj`/`package.json` bağımlılık değişiklikleri.

Her grup için 1-3 cümlelik somut bir özet yaz (diff'i satır satır tekrarlama, "ne değişti" sorusuna cevap ver). Boş kalan gruplar rapora dahil edilmez.

## Risk kontrol listesi

Özetten sonra, aşağıdaki kategorilere göre değişiklikleri tara ve sadece **gerçekten diff'te karşılığı olan** riskleri raporla — kategori listesini mekanik biçimde uygulama:

### Mimari/borç riskleri
- Yeni eklenen bir endpoint/route handler, `AppDbContext`'i doğrudan enjekte ediyor mu (hedef Endpoint→Service→Repository ayrımına aykırı, bkz. CLAUDE.md "Bilinen Borçlar")? Mevcut borçlu dosyalarda (`AuthEndpoint`, `PostsEndpoint`, `CommentsEndpoint`) küçük bir düzenlemeyse bunu tekrar raporlama; ama **yeni** bir endpoint aynı deseni tekrarlıyorsa işaretle.
- Yeni bir response tipi anonim `new { ... }` olarak mı döndürülüyor, yoksa adlandırılmış `*Response` record'u mu kullanılıyor (naming convention)?
- Model (`Post`/`Comment`/`User`) veya `AppDbContext.OnModelCreating` değişmiş ama karşılığında yeni bir `Migrations/` dosyası yoksa: şemayla migration'ın senkron olmama riski — kullanıcıya `ef-migration` skill'ini hatırlat.

### Güvenlik riskleri
- Diff içinde hardcoded secret/API key/connection string/parola benzeri bir literal eklenmiş mi (özellikle `appsettings*.json`, `environment*.ts`, `Program.cs`, `AuthEndpoint.cs`)?
- Yeni bir endpoint eklenmişse `.RequireAuthorization()` bekleniyor mu, eksikse işaretle (bkz. CLAUDE.md yetkilendirme kararları — emin değilsen bunun kasıtlı olup olmadığını kullanıcıya sor, sessizce "eksik" deme).
- Girdi doğrulaması olmayan yeni bir DTO/record eklenmiş mi (`MaxLength` vb. yok)?
- Kapsamlı bir güvenlik taraması gerekiyorsa (bu skill sadece diff'e bakar, uçtan uca taramaz) `security-audit` skill'ini öner.

### Yanlışlıkla commit edilecek dosyalar
- Untracked listede `devblog.db`, `bin/`, `obj/`, `node_modules/`, `.env`, `*.user`, IDE'ye özel dosyalar var mı? `.gitignore`'da olup olmadığını kontrol et; `.gitignore` kapsamındaysa zaten stage'e girmemiştir ama yine de `git status` çıktısında beliriyorsa nedenini araştır.
- `git diff` içinde debug amaçlı bırakılmış `Console.WriteLine`/`console.log`, yorum satırına alınmış kod bloğu, ya da `TODO`/`FIXME` (bu proje kasıtlı TODO'lar barındırdığı için CLAUDE.md'deki mevcut TODO'larla karıştırma — sadece **bu diff'te yeni eklenenleri** işaretle) var mı?

### Kapsam/hijyen riskleri
- Diff, birbiriyle ilgisiz birden fazla değişikliği (ör. bir bug fix + alakasız bir refactor + formatlama) tek arada karıştırıyor mu? Böyleyse kullanıcıya ayrı commit'lere bölmeyi öner.
- Çok büyük bir diff (`git diff --stat` ile onlarca dosya/yüzlerce satır) tek seferde mi hazırlanmış — review edilebilirlik açısından not düş.

## Ciddiyet seviyeleri

`security-audit` skill'iyle tutarlı ölçek kullan: **Critical**, **High**, **Medium**, **Low**, **Info**. Sızdırılmış secret/parola gibi bulgular her zaman en az High'dır.

## Çıktı formatı

```
## Özet
<Katman bazlı kısa özet>

## Riskler
[Severity] <Başlık> — <açıklama> (bkz. <dosya:satır>)
Öneri: <somut aksiyon>
```

Bulgu yoksa "Belirgin bir risk bulunamadı" yaz — boş kategori listesi üretme.

## Sınırlar

- Bu skill **sadece rapor üretir**, hiçbir dosyayı otomatik düzeltmez, stage etmez veya commit etmez.
- Kullanıcı bulgulardan birini "düzelt" derse, bunu ayrı bir onay adımı olarak ele al (birden fazla düzeltme varsa hepsini birden sessizce uygulama).
- Commit mesajı yazma bu skill'in kapsamı dışındadır — kullanıcı commit etmek isterse `commit-message` skill'ine yönlendir.
