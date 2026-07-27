# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Projeye Genel Bakış

DevBlog Starter, .NET Minimal API backend ve Angular frontend'den oluşan, JWT auth kullanan örnek bir blog uygulamasıdır. Bilinçli olarak bir starter/eğitim projesidir — kodda birçok TODO, dev-only kısayolları işaretler (bkz. "Mimari Kararlar > Bilinen Borçlar"); bunlar istenmeden sorulmadan "düzeltilmemelidir", zira bir eğitim/demo senaryosu için kasıtlı olabilirler.

### Komutlar

**Backend** (`src/DevBlog.Api`, .NET 10 Minimal API)

```bash
dotnet run --project src/DevBlog.Api/DevBlog.Api.csproj   # API'yi çalıştır (başlangıçta migration + seed uygular)
dotnet build DevBlog.slnx                                  # build
dotnet ef migrations add <Name> --project src/DevBlog.Api  # EF Core migration ekle
```

**Frontend** (`devblog-ui`, Angular 22)

```bash
cd devblog-ui
npm install
npm start          # ng serve
npm run build      # ng build
npm run watch       # ng build --watch --configuration development
```

### Mevcut Mimari (kod tabanının bugünkü hali)

- Backend endpoint'leri `src/DevBlog.Api/Endpoints/` altında feature'a göre gruplanır (`AuthEndpoint`, `PostsEndpoint`, `CommentsEndpoint`), her biri route'ları kaydeden statik bir `Map(WebApplication app)` metodu sunar.
- `LikesEndpoint` (`POST /posts/{slug}/like`) ve `SearchEndpoint` (`GET /search/{arg}`) da aynı grupta yer alır; ikisi de hedef mimariye uygun şekilde `IPostService`/`ILikeService` + repository katmanı üzerinden çalışır — yeni endpoint eklerken örnek alınacak referanslar bunlardır.
- `Program.cs` tek composition root'tur: DbContext, CORS, JWT auth ve OpenAPI orada yapılandırılır; başlangıçta `db.Database.Migrate()` ve ardından `DataSeeder.Seed(db)` çalışır — bu yüzden SQLite DB (`devblog.db`) API her başladığında otomatik oluşturulur/migrate edilir/seed edilir.
- Data katmanı: `AppDbContext` (`src/DevBlog.Api/Data/AppDbContext.cs`), `Users`, `Posts`, `Comments` DbSet'lerini ve `OnModelCreating` içindeki FK/index yapılandırmasını tanımlar. `DataSeeder`, `Users` tablosu boşsa `admin`/`admin` kullanıcısı ile örnek post/comment seed eder.
- Modeller (`src/DevBlog.Api/Models/`) düz EF entity'leridir: `Post` (bir `User` yazara ait, birden çok `Comment`'e sahip), `Comment`, `User` (`Role` alanı var, ör. `Admin`/`Author`).
- Frontend, standalone-component'li bir Angular uygulamasıdır (NgModule yok). Route'lar `src/app/app.routes.ts` içinde `loadComponent` ile lazy-load edilir. `AuthService` ve `PostService` (`src/app/services/`), API'ye giden tek HTTP sınırlarıdır; ikisi de `environment.apiUrl`'i okur. `authInterceptor`, `localStorage`'daki JWT'yi her isteğe ekler.

## Mimari Kararlar

### Hedef Mimari

Backend için hedeflenen katmanlaşma:

- **Endpoint** katmanı: sadece HTTP request/response mapping yapar (route tanımı, model binding, `Results.*`), iş kuralı içermez.
- **Service** katmanı: business logic burada yaşar (ör. yetkilendirme kuralları, slug üretimi/validasyonu, token üretimi). Endpoint'ler service'leri DI ile çağırır.
- **Repository** katmanı: data access burada yaşar (EF Core sorguları, `AppDbContext` kullanımı). Service'ler repository'leri DI ile çağırır.
- **Kural**: `AppDbContext` hiçbir endpoint'e doğrudan enjekte edilmez. Bir endpoint imzasında `AppDbContext` görülüyorsa bu hedefe uymuyor demektir.

### Bilinen Borçlar

Aşağıdakiler mevcut kodda hedef mimariye uymayan, bilinçli/bilinen teknik borçlardır — göreve özel olarak istenmedikçe sessizce "düzeltilmemelidir":

- **`AuthEndpoint.Map`**, `AppDbContext`'i doğrudan enjekte ediyor ve login/token üretim mantığını endpoint içinde yürütüyor — service/repository ayrımı yok.
- **`PostsEndpoint.Map`** kısmen hedef mimariye taşındı: `GET /posts` ve `POST /posts` artık `IPostService` kullanıyor, ama `GET /posts/{slug}` hâlâ `AppDbContext`'i doğrudan enjekte ediyor.
- **`CommentsEndpoint.Map`** kısmen hedef mimariye taşındı: `GET /comments` artık `ICommentService` kullanıyor, ama `POST /posts/{slug}/comments` hâlâ `AppDbContext`'i doğrudan enjekte ediyor.
- JWT imzalama secret'ı `Program.cs` ve `AuthEndpoint.cs` içinde hardcoded string olarak tutuluyor (elle senkron — biri değişirse diğeri de değişmeli).
- CORS politikası `AllowAnyOrigin/Method/Header`.
- `AuthEndpoint`, response için adlandırılmış bir `*Response` record'u yerine hâlâ anonim tip (`new { token = ... }`) döndürüyor; `PostsEndpoint`/`CommentsEndpoint`/`LikesEndpoint` artık adlandırılmış `*Response` record'larına geçti (bkz. Naming Convention) — kalan tek borç `AuthEndpoint`.

## Naming Convention

- **Backend (C#)**: PascalCase sınıf/metot/property (`PostsEndpoint`, `CreatePostRequest`); endpoint gruplama sınıfları `<Feature>Endpoint` şeklinde adlandırılır ve statik bir `Map(WebApplication app)` metodu sunar. Request DTO'ları `Create<Entity>Request` gibi record olarak tanımlanır (bkz. `CreatePostRequest`, `CreateCommentRequest`, `LoginRequest`). Response amaçlı DTO record'ları `Response` ile bitmelidir (ör. `PostSummaryResponse`, `CommentResponse`, `LikeToggleResponse`) — `PostsEndpoint`/`CommentsEndpoint`/`LikesEndpoint` bu kalıba geçti; `AuthEndpoint` hâlâ anonim tip (`new { token = ... }`) döndürüyor ve bu "Bilinen Borçlar" kapsamındadır.
- **Frontend (TypeScript/Angular)**: standalone component'ler dosya bazında `kebab-case` klasör/isimlendirme kullanır (`pages/post-list/post-list.component.ts`), sınıf isimleri PascalCase + `Component` soneki (`PostListComponent`). Servisler `<Domain>Service` (`AuthService`, `PostService`) olarak adlandırılır ve `src/app/services/` altında toplanır. Interceptor'lar camelCase fonksiyon adı + `Interceptor` soneki kullanır (`authInterceptor`).
- Route path'leri ve API URL segmentleri lowercase/kebab-case değil, düz İngilizce kelimelerle çoğul isim şeklindedir (`/posts`, `/posts/{slug}/comments`).

## Test Stratejisi

Şu anda ne backend ne de frontend tarafında test projesi/scripti bulunmuyor: solution'da bir test projesi yok, `devblog-ui/package.json` içinde de `test`/`lint` script'i tanımlı değil.

### Hedef Test Stratejisi

- Backend testleri **xUnit** ile yazılır.
- En az **%70 code coverage** hedeflenir.
- **Tüm endpoint'ler** integration testiyle kapsanır.

### Bilinen Borç

Şu an hiç test projesi/test kodu yok — hedef %70 coverage ve tüm endpoint'lerin integration testiyle kapsanması şartına uyulmuyor. Ayrıca mevcut endpoint'ler `AppDbContext`'e doğrudan bağımlı olduğu için (bkz. "Mimari Kararlar > Bilinen Borçlar") service/repository ayrımı olmadan anlamlı unit test yazmak zordur; test eklenmesi istendiğinde önce hedef mimariye uygun, mock'lanabilir sınırlar üzerinden ilerlemek gerekir.
