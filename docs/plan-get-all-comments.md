# Plan: Tüm Yorumları Listeleyen Endpoint (`GET /comments`)

## Context

Şu anda `CommentsEndpoint.cs` sadece tek bir post'a yorum eklemeye yarayan `POST /posts/{slug}/comments` route'unu içeriyor; belirli bir post'a bakmadan tüm yorumları (ör. moderasyon/genel bakış amacıyla) çekebileceğim bir GET endpoint'i yok. CLAUDE.md'de tanımlanan **hedef mimari** (Endpoint → Service → Repository → AppDbContext, adlandırılmış `*Response` DTO'ları) bugün hiçbir yerde uygulanmıyor — tüm mevcut endpoint'ler `AppDbContext`'i doğrudan enjekte ediyor ("Bilinen Borçlar"). Bu görev, o hedef mimariyi ilk kez, sadece bu yeni endpoint için (mevcut borçlu endpoint'lere dokunmadan) hayata geçiriyor.

Kullanıcı ile netleştirilen kapsam:
- **Public** endpoint, yetkilendirme yok (`GET /posts` ile tutarlı).
- Sadece **sayfalama** (`page`, `pageSize`), post'a göre filtre yok — `GET /posts`'taki konvansiyonla birebir aynı (clamp: `page >= 1`, `pageSize` 1-100 arası, response şekli `Items/Page/PageSize/TotalCount/TotalPages`).

## Yeni Dosyalar

**`src/DevBlog.Api/Repositories/ICommentRepository.cs`**
```csharp
using DevBlog.Api.Models;

namespace DevBlog.Api.Repositories;

public interface ICommentRepository
{
    Task<(IReadOnlyList<Comment> Comments, int TotalCount)> GetAllAsync(int page, int pageSize);
}
```

**`src/DevBlog.Api/Repositories/CommentRepository.cs`**
```csharp
using DevBlog.Api.Data;
using DevBlog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DevBlog.Api.Repositories;

public class CommentRepository(AppDbContext db) : ICommentRepository
{
    public async Task<(IReadOnlyList<Comment> Comments, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        var query = db.Comments.AsNoTracking().Include(c => c.Post);

        var totalCount = await query.CountAsync();

        var comments = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (comments, totalCount);
    }
}
```
Repository, tüm EF Core/`AppDbContext` işini üstlenir (`AsNoTracking`, `Include(Post)`, sıralama, skip/take, count). Entity döner, DTO mapping yapmaz.

**`src/DevBlog.Api/Services/ICommentService.cs`**
```csharp
using DevBlog.Api.Endpoints;

namespace DevBlog.Api.Services;

public interface ICommentService
{
    Task<PagedCommentsResponse> GetAllCommentsAsync(int page, int pageSize);
}
```

**`src/DevBlog.Api/Services/CommentService.cs`**
```csharp
using DevBlog.Api.Endpoints;
using DevBlog.Api.Repositories;

namespace DevBlog.Api.Services;

public class CommentService(ICommentRepository commentRepository) : ICommentService
{
    public async Task<PagedCommentsResponse> GetAllCommentsAsync(int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (comments, totalCount) = await commentRepository.GetAllAsync(page, pageSize);

        var items = comments.Select(c => new CommentResponse(
            c.Id,
            c.AuthorName,
            c.Body,
            c.CreatedAt,
            c.PostId,
            c.Post.Slug,
            c.Post.Title
        )).ToList();

        return new PagedCommentsResponse(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize)
        );
    }
}
```
Service, business/application kuralları (page/pageSize clamp) ve entity → DTO mapping işini üstlenir.

DTO'lar mevcut konvansiyona uyacak şekilde (`CreateCommentRequest`'in `CommentsEndpoint.cs` içinde tanımlanması gibi) `Endpoints` namespace'inde, `CommentsEndpoint.cs` dosyasının altında tanımlanacak — ayrı bir `DTOs/` klasörü açılmayacak.

## Değişecek Dosyalar

### `src/DevBlog.Api/Endpoints/CommentsEndpoint.cs`
- Üste `using DevBlog.Api.Services;` eklenir (mevcut `using DevBlog.Api.Data;` ve `using Microsoft.EntityFrameworkCore;` POST route hâlâ kullandığı için kalır).
- `Map` metoduna yeni route eklenir:
```csharp
app.MapGet("/comments", async (ICommentService commentService, int page = 1, int pageSize = 20) =>
{
    var result = await commentService.GetAllCommentsAsync(page, pageSize);
    return Results.Ok(result);
});
```
  Yetkilendirme yok (`.RequireAuthorization()` eklenmez).
- Dosyanın altına, mevcut `CreateCommentRequest` record'unun yanına iki yeni response record'u eklenir:
```csharp
public record CommentResponse(
    int Id,
    string AuthorName,
    string Body,
    DateTime CreatedAt,
    int PostId,
    string PostSlug,
    string PostTitle
);

public record PagedCommentsResponse(
    IReadOnlyList<CommentResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
```

### `src/DevBlog.Api/Program.cs`
- Üste `using DevBlog.Api.Repositories;` ve `using DevBlog.Api.Services;` eklenir.
- `AddDbContext<AppDbContext>` bloğundan hemen sonra, CORS bloğundan önce yeni bir DI kaydı bloğu eklenir:
```csharp
// 2. Repositories & Services
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ICommentService, CommentService>();
```
  (Sonraki adım numaraları — CORS, JWT, Authorization, OpenAPI — buna göre kaydırılır.)
- `CommentsEndpoint.Map(app)` çağrısında değişiklik gerekmez; yeni route aynı statik `Map` metodunun içinde tanımlandığı için otomatik kayıt olur.

## Dokunulmayacaklar
- `POST /posts/{slug}/comments` route'u ve mevcut `AppDbContext` doğrudan enjeksiyonu — bilinçli teknik borç, bu görev kapsamında değil.
- `AuthEndpoint`, `PostsEndpoint` — retrofit edilmeyecek.

## Doğrulama
1. `dotnet build DevBlog.slnx` ile derleme hatası olmadığını kontrol et.
2. `dotnet run --project src/DevBlog.Api/DevBlog.Api.csproj` ile API'yi başlat (migration + seed otomatik uygulanır, seed veri en az bir post + comment içerir).
3. `GET http://localhost:<port>/comments` ve `GET http://localhost:<port>/comments?page=1&pageSize=5` isteklerini (Scalar/OpenAPI UI veya curl ile) çağırıp response şeklinin (`items[].postSlug`, `postTitle` dahil) ve `totalCount`/`totalPages` alanlarının doğru geldiğini doğrula.
4. Auth header olmadan da 200 döndüğünü teyit ederek "public" gereksinimini doğrula.

## Checkpoint'ler

- [ ] `Repositories/ICommentRepository.cs` oluşturuldu
- [ ] `Repositories/CommentRepository.cs` oluşturuldu
- [ ] `Services/ICommentService.cs` oluşturuldu
- [ ] `Services/CommentService.cs` oluşturuldu
- [ ] `Endpoints/CommentsEndpoint.cs` içine `GET /comments` route'u ve `CommentResponse`/`PagedCommentsResponse` record'ları eklendi
- [ ] `Program.cs` içine repository/service DI kayıtları ve using'ler eklendi
- [ ] `dotnet build DevBlog.slnx` başarılı
- [ ] `GET /comments` ve `GET /comments?page=&pageSize=` manuel olarak test edildi (auth'suz 200 dönüyor, response şekli doğru)
