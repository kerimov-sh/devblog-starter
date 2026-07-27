---
name: seo-settings
description: DevBlog Starter reposunda herhangi bir route değişikliği (app.routes.ts), yeni bir sayfa/component (loadComponent ile eklenen), ya da post-list/post-detail gibi mevcut sayfaların HTML template'lerinde değişiklik olduğunda kullan. Bu bir blog uygulaması olduğu için makale başlığı, etiketler (Tags) ve yorumlar SEO açısından doğrudan ilgilidir. Kullanıcı "SEO kontrolü yap", "bu sayfa SEO açısından uygun mu", "yeni route ekledim SEO'yu kontrol et" dediğinde de tetikle. Angular'ın Title/Meta servislerinin kullanılıp kullanılmadığını, başlık hiyerarşisini, semantik HTML'i ve slug/etiket tabanlı iç linklemeyi bu reponun mevcut yapısına göre denetler.
---

# SEO Ayarları (DevBlog Starter'a Özel)

Bu skill, Angular standalone component + lazy-loaded route (`app.routes.ts`) mimarisine sahip bu blog uygulamasında yeni/değişen bir sayfanın SEO temellerini karşılayıp karşılamadığını denetler. Skill kod değiştirmez, yalnızca raporlar — bulunan eksiklikler için somut bir düzeltme önerisi sunar ve kullanıcı isterse uygulamayı teklif eder, ama otomatik olarak dosya değiştirmez.

## Neden bu kontroller önemli

Bu proje şu an **client-side rendering (CSR)** yapıyor — Angular Universal/SSR yapılandırılmamış (bkz. `app.config.ts`, sadece `provideRouter`/`provideHttpClient` var). Bu, arama motorunun sayfayı JS çalıştırmadan taradığı senaryolarda `<title>`, meta description ve hatta post içeriğinin hiç görünmeyebileceği anlamına gelir. Bu, skill'in her seferinde çözmeye çalışacağı bir şey değil (SSR eklemek ayrı, büyük bir mimari karar) — ama her denetimde şeffaf şekilde not edilmesi gereken bir arka plan gerçeği: CSR-only bir SPA'da, sayfa başına doğru `Title`/`Meta` servis çağrıları olmadan aşağıdaki maddelerin hiçbiri pratikte işe yaramaz, çünkü statik `index.html` içindeki jenerik `<title>DevBlog</title>` tüm route'larda aynı kalır.

## Kontrol Listesi

Bir route/sayfa/component değişikliğinde şunları, ilgili dosyaya referans vererek tek tek denetle:

### 1. Title tag — route'a özel mi?

Bu repoda `devblog-ui/src/index.html` içinde statik `<title>DevBlog</title>` var ve şu an **hiçbir component** bunu `@angular/platform-browser`'ın `Title` servisiyle güncellemiyor (bu bilinen bir eksikliktir, ör. `PostDetailComponent.ngOnInit` sadece `postService.getPost(...)` çağırıyor, `Title.setTitle(...)` yok). Yeni/değişen sayfa için:
- İçerik yüklendiğinde (`ngOnInit` içindeki subscribe callback'inde, post gelince) `Title.setTitle(...)` çağrılıyor mu?
- Post detail gibi içerik-özel sayfalarda title, post başlığını içeriyor mu (ör. `${post.title} | DevBlog`), jenerik "DevBlog" olarak mı kalıyor?

### 2. Meta description — route'a özel mi?

Aynı şekilde `Meta` servisi (`@angular/platform-browser`) kullanılarak `<meta name="description">` her route için güncelleniyor mu? Post detail'de description, `post.content`'in HTML etiketlerinden arındırılmış ilk ~150-160 karakteri olmalı (Post modelinde ayrı bir summary/excerpt alanı yok — bu yüzden Content'ten türetilirken HTML/markdown temizliği gerektiğini not et). Liste sayfası gibi içerik-nötr sayfalarda jenerik ama açıklayıcı bir description yeterli.

### 3. Başlık hiyerarşisi

Mevcut `post-detail.component.html` zaten doğru bir hiyerarşi kuruyor: `<article><h1>{{post.title}}</h1></article>`, ardından `<section><h2>Comments</h2>` ve `<h3>Add a comment</h3>`. Yeni eklenen/değişen sayfalarda bu düzenin korunduğunu doğrula: sayfa başına tek `<h1>`, alt başlıklar sırayla (h1→h2→h3, seviye atlanmadan) ilerliyor mu?

### 4. Slug tabanlı, insan-okunur URL

Route yapısı zaten `/posts/:slug` şeklinde (query-string id değil) — bu iyi bir SEO temeli. Yeni bir route eklenirken aynı prensip korunmalı: id yerine slug/insan-okunur segment kullan. Not: CLAUDE.md'nin "Bilinen Borçlar" bölümünde belirtildiği gibi `POST /posts` üzerinde slug tekilliği validasyonu yok — bu skill'in konusu değil ama slug'a dayalı bir SEO stratejisi kuruluyorsa çakışan/boş slug riskinin URL yapısını bozabileceğini bir ⚠️ olarak not düş.

### 5. Etiketler (Tags) — iç linkleme fırsatı

`Post.Tags` düz virgülle ayrılmış bir string olarak saklanıyor ve şu an template'lerde düz metin olarak render ediliyor (`post-list.component.html`: `<em>{{ post.tags }}</em>`, `post-detail.component.html` de benzer). Etiketlerin ayrı, tıklanabilir (`routerLink` ile bir etiket/filtre sayfasına giden) semantik öğeler olması iç linkleme ve etiket bazlı keşif açısından değer katar. Bu bir MUST değil — mevcut düz-metin haliyle "çalışıyor" ama bir fırsat kaybı; ⚠️ olarak raporla, zorla değiştirme.

### 6. Yorumlar — kısmen SEO sinyali

Yorumlar (`Comment.AuthorName`/`Body`) zaten `<article>` dışında ayrı bir `<section><h2>Comments</h2>` altında, semantik olarak ayrıştırılmış durumda — bu doğru bir yapı, devam eden sayfalarda bu ayrımın bozulmadığını kontrol et. Yorumların CSR-only render nedeniyle arama motoru tarafından hiç görülmeyebileceğini (bkz. yukarıdaki SSR notu) hatırla. Yorum içeriğinin SEO amaçlı manipülasyonu (keyword stuffing vb.) bu skill'in kapsamı dışında — güvenlik/moderasyon konusu, burada değerlendirme.

### 7. Semantik HTML

`<div>` yığını yerine anlamlı etiket kullanımı (`<article>`, `<section>`, `<nav>`) bu repoda kısmen zaten var (ör. `post-list.component.html`'deki `<nav class="pagination">`). Yeni eklenen HTML'in bu pattern'i bozup düz `<div>` yığınına dönmediğini doğrula.

## Çıktı Formatı

Her madde için:

```
[✅ | ⚠️ | ❌] <Madde adı> — <kısa gerekçe> (bkz. <dosya:satır>)
```

Rapor sonunda, CSR-only mimarinin (bkz. "Neden bu kontroller önemli") o an denetlenen sayfa için pratik etkisini tek cümlede özetle (ör. "Title/Meta güncellemesi eklense bile, arama motoru JS çalıştırmıyorsa bu içerik hâlâ görünmeyebilir"). Skill bir düzeltme yamasını otomatik uygulamaz; bulguları sunduktan sonra kullanıcı "düzelt" derse ilgili component'e `Title`/`Meta` servis enjeksiyonunu ve `ngOnInit` güncellemesini uygulaman istenebilir — bu ayrı bir onay/uygulama adımıdır.
