# Plan: POST /posts için Slug Tekilliği Doğrulaması

## Context

Önceki incelemede tespit edildi: `PostsEndpoint.cs:74-93` içindeki `POST /posts` handler'ı ne uygulama kodunda ne de veritabanı seviyesinde slug tekilliğini kontrol ediyor (`AppDbContext.cs:14-15`'teki `HasIndex(p => p.Slug)` unique değil). Sonuç: aynı slug'la iki post oluşturulabiliyor; `GET /posts/{slug}` (`PostsEndpoint.cs:46-72`) `.FirstOrDefaultAsync()` kullandığı için ikinci post'a hiçbir zaman slug üzerinden ulaşılamıyor ve kullanıcıya sessizce yanlış post gösteriliyor. Bu CLAUDE.md'de "Bilinen Borç" olarak işaretliydi; kullanıcı artık bunun düzeltilmesini istiyor.

Çözüm, CLAUDE.md'nin "Mimari Kararlar > Hedef Mimari" bölümünde tanımlanan Endpoint → Service → Repository katmanlaşmasına uygun olacak — bu, `GET /comments` için zaten uygulanmış durumda (`src/DevBlog.Api/Services/ICommentService.cs`+`CommentService.cs`, `src/DevBlog.Api/Repositories/ICommentRepository.cs`+`CommentRepository.cs`, `Program.cs`'te `AddScoped` kaydı). Bu görev aynı deseni birebir `Post` create akışı için uygular.

Kapsam bilinçli olarak dar tutuluyor: yalnızca `POST /posts` (slug tekilliği kuralının yaşadığı yer) Service/Repository katmanına taşınıyor. `GET /posts` ve `GET /posts/{slug}` şu an olduğu gibi `AppDbContext`'i doğrudan kullanmaya devam ediyor — bu ayrı, istenmemiş bir "Bilinen Borç" ve bu görevin parçası değil.

Kullanıcıyla netleştirildi: hem service katmanında ön kontrol (`SlugExistsAsync`) hem de DB'de unique index eklenecek — salt kod kontrolü, aynı anda gelen iki isteğin (race condition) ikisinin de kontrolü geçip aynı slug'ı eklemesini engelleyemez; DB constraint bu senaryoya karşı asıl güvence.

## Uygulama Planı

### 1. `src/DevBlog.Api/Repositories/IPostRepository.cs` (yeni)

```csharp
using DevBlog.Api.Models;

namespace DevBlog.Api.Repositories;

public interface IPostRepository
{
    Task<bool> SlugExistsAsync(string slug);
    Task AddAsync(Post post);
}
```

### 2. `src/DevBlog.Api/Repositories/PostRepository.cs` (yeni)

`CommentRepository`'nin primary-constructor DI stilini birebir izler:

```csharp
using DevBlog.Api.Data;
using DevBlog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DevBlog.Api.Repositories;

public class PostRepository(AppDbContext db) : IPostRepository
{
    public Task<bool> SlugExistsAsync(string slug) =>
        db.Posts.AnyAsync(p => p.Slug == slug);

    public async Task AddAsync(Post post)
    {
        db.Posts.Add(post);
        await db.SaveChangesAsync();
    }
}
```

### 3. `src/DevBlog.Api/Services/IPostService.cs` (yeni)

```csharp
using DevBlog.Api.Endpoints;

namespace DevBlog.Api.Services;

public interface IPostService
{
    Task<CreatePostResult> CreatePostAsync(CreatePostRequest req, int authorId);
}
```

### 4. `src/DevBlog.Api/Services/PostService.cs` (yeni)

İş kuralı (slug tekilliği) burada yaşıyor; DB unique index'in yakaladığı race-condition durumunu da `DbUpdateException` yakalayarak aynı sonuca çeviriyor (kullanıcıya 500 yerine tutarlı bir 409 dönmesi için):

```csharp
using DevBlog.Api.Endpoints;
using DevBlog.Api.Models;
using DevBlog.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DevBlog.Api.Services;

public class PostService(IPostRepository postRepository) : IPostService
{
    public async Task<CreatePostResult> CreatePostAsync(CreatePostRequest req, int authorId)
    {
        if (await postRepository.SlugExistsAsync(req.Slug))
        {
            return new CreatePostResult(false, null, $"Slug '{req.Slug}' already exists.");
        }

        var post = new Post
        {
            Title = req.Title,
            Content = req.Content,
            Slug = req.Slug,
            Tags = req.Tags,
            PublishedAt = DateTime.UtcNow,
            AuthorId = authorId
        };

        try
        {
            await postRepository.AddAsync(post);
        }
        catch (DbUpdateException)
        {
            return new CreatePostResult(false, null, $"Slug '{req.Slug}' already exists.");
        }

        return new CreatePostResult(true, new CreatePostResponse(post.Id, post.Slug), null);
    }
}
```

### 5. `src/DevBlog.Api/Endpoints/PostsEndpoint.cs` (değişiklik)

`MapPost("/posts", ...)` handler'ı `AppDbContext` yerine `IPostService` enjekte eder; `// TODO: slug uniqueness validation eksik` yorumu kaldırılır. Anonim `new { post.Id, post.Slug }` yerine adlandırılmış `CreatePostResponse` kullanılır (Naming Convention: yeni/değiştirilen endpoint'ler `*Response` record kullanmalı).

```csharp
app.MapPost("/posts", async (CreatePostRequest req, IPostService postService, ClaimsPrincipal user) =>
{
    var authorId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await postService.CreatePostAsync(req, authorId);

    return result.Success
        ? Results.Created($"/posts/{result.Post!.Slug}", result.Post)
        : Results.Conflict(new { error = result.Error });
}).RequireAuthorization();
```

Dosyanın sonuna, `CreatePostRequest`'in yanına yeni record'lar eklenir (CommentsEndpoint.cs'teki `CommentResponse`/`PagedCommentsResponse` yerleşimiyle aynı desen):

```csharp
public record CreatePostResponse(int Id, string Slug);

public record CreatePostResult(bool Success, CreatePostResponse? Post, string? Error);
```

`GET /posts` ve `GET /posts/{slug}` handler'ları değişmeden kalır (kapsam dışı).

### 6. `src/DevBlog.Api/Data/AppDbContext.cs` (değişiklik)

Mevcut non-unique index unique yapılır:

```csharp
modelBuilder.Entity<Post>()
    .HasIndex(p => p.Slug)
    .IsUnique();
```

### 7. EF Core Migration (yeni)

```bash
dotnet ef migrations add AddUniqueSlugIndex --project src/DevBlog.Api
```

`Program.cs`'teki mevcut `db.Database.Migrate()` çağrısı (satır 53) API her başladığında bu migration'ı otomatik uygulayacak — ekstra bir adım gerekmiyor. Not: mevcut DB'de zaten çakışan slug'lı kayıtlar varsa migration uygulanırken hata verir; seed verisinde (`DataSeeder.cs`) çakışma olmadığı doğrulanmalı (kontrol edildi: `DataSeeder` tek bir örnek post oluşturuyor, sorun yok).

### 8. `src/DevBlog.Api/Program.cs` (değişiklik)

Mevcut Comment kayıtlarının yanına eklenir:

```csharp
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IPostService, PostService>();
```

## Kapsam Dışı (bilinçli olarak)

- `GET /posts`, `GET /posts/{slug}` — `AppDbContext`'i doğrudan kullanmaya devam ediyor, bu görevin parçası değil.
- `CommentsEndpoint.cs`'teki `POST /posts/{slug}/comments` — hâlâ `AppDbContext` doğrudan kullanıyor, ayrı bilinen borç.
- Slug'ın kendisinin biçim/validasyonu (boş, özel karakter vb.) — istenmedi, sadece tekillik ele alınıyor.

## Doğrulama

1. `dotnet build DevBlog.slnx` ile derleme hatası olmadığını doğrula.
2. `dotnet ef migrations add AddUniqueSlugIndex --project src/DevBlog.Api` çalıştır, oluşan migration dosyasını gözden geçir (yalnızca unique index değişikliği içermeli).
3. `dotnet run --project src/DevBlog.Api/DevBlog.Api.csproj` ile API'yi başlat — mevcut `devblog.db` migrate olurken hata vermemeli.
4. Aynı slug ile iki kez `POST /posts` çağır (JWT ile, `/auth/login` üzerinden alınan token'la): ilk istek `201 Created` + `CreatePostResponse` dönmeli, ikinci istek `409 Conflict` + hata mesajı dönmeli.
5. Farklı slug'larla post oluşturmanın hâlâ normal çalıştığını doğrula.
6. `GET /posts/{slug}` ile oluşturulan postun doğru döndüğünü doğrula.