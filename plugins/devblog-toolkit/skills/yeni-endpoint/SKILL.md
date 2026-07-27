---
name: yeni-endpoint
description: DevBlog Starter reposunda YALNIZCA "/yeni-endpoint <HTTP_METHOD> <route>" komutuyla çağrılır (ör. "/yeni-endpoint POST /posts/{slug}/comments/{id}/replies"). Serbest metinle ("yeni endpoint ekle", "şunu implemente et" gibi) tetiklenmez. Verilen route için hedef mimariye (Endpoint→Service→Repository ayrımı, adlandırılmış *Request/*Response record'ları) uygun bir uygulama planı çıkarır, planı kullanıcıya onaylatır, ardından planı adım adım uygular — her dosya değişikliğinden önce ayrı onay ister.
---

# Yeni Endpoint (DevBlog Starter)

Bu skill sadece `/yeni-endpoint <HTTP_METHOD> <route>` komutuyla çağrılır. Girdi olarak bir HTTP metodu (`GET`/`POST`/`PUT`/`PATCH`/`DELETE`) ve bir route (ör. `/posts/{slug}/comments/{id}/replies`) alır. Amaç, backend'de **hedef mimariye** (bkz. CLAUDE.md "Mimari Kararlar > Hedef Mimari") uygun, `AppDbContext`'i endpoint'e doğrudan enjekte etmeyen bir endpoint üretmektir — mevcut borçlu dosyaların (`AuthEndpoint`, `PostsEndpoint`, `CommentsEndpoint` yazma yolu) desenini tekrar etmez.

**Kapsam**: yalnızca backend (`src/DevBlog.Api`). Frontend entegrasyonu (Angular service metodu, component) bu skill'in işi değildir — gerekirse kullanıcıya bunu ayrıca `frontend-specialist` ile yaptırmasını öner. Otomatik test yazımı da kapsam dışıdır (CLAUDE.md: test altyapısı henüz yok).

## 1. Girdi ayrıştırma ve analiz

1. Komuttan HTTP metodunu ve route'u ayıkla. Route eksik/metod geçersizse kullanıcıdan netleştirmesini iste.
2. Route'un ilk anlamlı segmentine bakarak (ör. `/posts/...` → `Posts`, `/comments/...` → `Comments`, `/likes/...` → `Likes`, `/auth/...` → `Auth`) `src/DevBlog.Api/Endpoints/`, `Services/`, `Repositories/` altında **var olan bir `<Feature>` seti** olup olmadığını kontrol et (`<Feature>Endpoint.cs`, `I<Feature>Service.cs`/`<Feature>Service.cs`, `I<Feature>Repository.cs`/`<Feature>Repository.cs`).
   - **Varsa**: yeni route'u bu mevcut sete eklemeyi planla (yeni bir `Map` içi route + mevcut Service/Repository'ye yeni metot). Mevcut dosyanın zaten borçlu olup olmadığını (`AppDbContext` doğrudan enjekte ediyor mu) kontrol et — borçluysa, **sadece bu yeni route için** hedef mimariye uygun bir Service/Repository yolu öner (mevcut borçlu route'ları refactor etmeyi otomatik olarak plana dahil etme, kullanıcı ayrıca isterse öner).
   - **Yoksa**: route'tan türetilen isimle sıfırdan bir `<Feature>Endpoint`/`I<Feature>Service`+`<Feature>Service`/`I<Feature>Repository`+`<Feature>Repository` seti planla.
3. Route'ta yeni bir kaynağı temsil eden bir segment varsa (ör. `replies`) ve bu bir `Model`e karşılık geliyorsa, mevcut `Models/` altında ilgili entity olup olmadığını kontrol et; yoksa yeni Model + `AppDbContext.OnModelCreating` + migration ihtiyacını plana ekle.
4. HTTP metoduna göre beklenen Service metot ismini belirle: `GET` (tekil) → `Get<Entity>Async`, `GET` (liste) → `GetAll<Entity>sAsync`/`Get<Entity>ListAsync`, `POST` → `Create<Entity>Async`, `PUT`/`PATCH` → `Update<Entity>Async`, `DELETE` → `Delete<Entity>Async`.

## 2. Netleştirilmesi gereken açık noktalar

Plan çıkarmadan önce, koddan çıkarılamayan şu noktaları kullanıcıya sor (tahmin yürütüp sessizce karar verme):

- **Yetkilendirme**: Bu endpoint `.RequireAuthorization()` gerektiriyor mu? Gerekiyorsa rol bazlı bir kısıtlama (`Admin`/`Author`) var mı?
- **Girdi doğrulama**: Request'teki hangi alanlar zorunlu, hangi alanlarda uzunluk/format sınırı olmalı (`MaxLength` vb.)?
- **Sahiplik/IDOR**: `PUT`/`PATCH`/`DELETE` ise, işlemi yalnızca kaynağın sahibi mi yapabilmeli, yoksa herhangi bir authenticated kullanıcı mı?
- **Migration**: Yeni/değişen bir Model alanı varsa, eski kayıtlara uygulanacak default değer ne olmalı (bkz. `ef-migration` skill'i ile aynı onay mantığı).

## 3. Planı sun ve onay al

Netleştirmelerden sonra, uygulanacak adımları **sıralı bir liste** olarak kullanıcıya göster (örnek adım sırası — route'un mevcut/yeni feature olmasına göre bazıları atlanabilir):

```
1. Request/Response record'ları (adlandırılmış, *Request / *Response ile bitecek şekilde)
2. Model/DbSet değişikliği + migration (gerekiyorsa)
3. Repository: I<Feature>Repository metodu + <Feature>Repository implementasyonu
4. Service: I<Feature>Service metodu + <Feature>Service implementasyonu (iş kuralı burada yaşar)
5. Program.cs DI kaydı (yeni Feature ise)
6. <Feature>Endpoint.Map içine route ekleme (yalnızca HTTP mapping + Results.*)
```

Bu genel planı kullanıcıya göster ve onay bekle. Onaylanmadan 4. bölüme geçme.

## 4. Adım adım uygulama

Onaylanan planı tek seferde uygulama — **her adımdan önce** o adımda değişecek dosyayı/dosyaları ve içeriğini özetle, kullanıcıdan onay al, onay gelince o adımı uygula, sonraki adıma geç. Bir adımda migration varsa, `ef-migration` skill'indeki veri kaybı/default değer onay akışını uygula (ayrı bir onay cümlesi iste).

Her adımda naming convention'a uy (bkz. CLAUDE.md "Naming Convention"):
- Request DTO: `Create<Entity>Request`/`Update<Entity>Request` (record).
- Response DTO: adlandırılmış `<Entity>Response`/`<Fiil><Entity>Response` — anonim `new { ... }` **kullanma**.
- Endpoint grup sınıfı: `<Feature>Endpoint`, statik `Map(WebApplication app)`.
- Service: `<Feature>Service` + `I<Feature>Service`, Repository: `<Feature>Repository` + `I<Feature>Repository`.

Endpoint katmanına **iş kuralı sızdırma** — endpoint yalnızca route/model binding/`Results.*` içerir, doğrulama ve iş mantığı Service'te yaşar (repository sadece EF Core erişimi yapar).

## 5. Sınırlar

- Bu skill **frontend değişikliği yapmaz** ve **test yazmaz** — kullanıcı isterse bunları ayrıca ister.
- Mevcut borçlu endpoint'leri (CLAUDE.md "Bilinen Borçlar") kendiliğinden refactor etmez; sadece yeni eklenen route için hedef mimariye uyar.
- Migration'ı kullanıcı onayı olmadan veritabanına uygulamaz (`dotnet ef database update` çalıştırmaz) — migration dosyasını oluşturduktan sonra uygulamayı ayrı bir onay adımı olarak sorar.
- Plan veya adımlardan biri reddedilirse/değiştirilirse, kalan adımları güncellenmiş plana göre yeniden sırala ve tekrar onaya sun.
