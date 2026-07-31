# Comment Count — Tasarım

**Tarih:** 2026-07-31
**Durum:** Onaylandı

## Amaç

Post listesi/arama sonuçlarında her post kartında o postun kaç yorum içerdiğini gösteren bir etiket eklemek. Backend'de post response'larına `CommentCount` alanı eklenir, frontend'de post kartlarında bu değer gösterilir.

## Kapsam Dışı

Post entity'sine kalıcı bir `CommentCount` DB kolonu eklenmiyor. `LikeCount` alanı nasıl runtime'da (PostLikes tablosundan) hesaplanıyorsa, `CommentCount` da aynı şekilde Comments tablosundan runtime'da hesaplanan bir alan olacak — migration gerekmiyor.

## Backend Değişiklikleri

Mevcut `LikeCount` deseni (`ILikeRepository.GetLikeCountsAsync`, `PostSummaryResponse.LikeCount`) referans alınarak birebir aynı yaklaşım uygulanacak:

- **`ICommentRepository`**: `Task<Dictionary<int, int>> GetCommentCountsAsync(IEnumerable<int> postIds)` metodu eklenir.
- **`CommentRepository`**: yukarıdaki metodu, `LikeRepository.GetLikeCountsAsync` ile aynı `GroupBy` deseniyle `db.Comments` üzerinden implemente eder.
- **`PostService`**: constructor'a `ICommentRepository` bağımlılığı eklenir; `GetPostsAsync` ve `SearchPostsAsync` içinde like count'larla birlikte comment count'lar da çekilir ve `PostSummaryResponse`'a aktarılır.
- **`PostSummaryResponse`** (`PostsEndpoint.cs`): `int CommentCount` alanı eklenir.
- **`GET /posts/{slug}`** (`PostsEndpoint.cs`, bilinen mimari borç — `AppDbContext` doğrudan enjekte ediliyor, bu borca dokunulmuyor): mevcut anonim projeksiyona `CommentCount = p.Comments.Count()` eklenir. `Comments` zaten include edilmiş durumda, tek satırlık eklemedir.

## Frontend Değişiklikleri

- **`post.service.ts`**: `PostSummary` interface'ine `commentCount: number` eklenir (`PostDetail` bu interface'i extend ettiği için otomatik kapsanır).
- **`post-list.component.html`**: her post kartında, mevcut like butonunun yanına yorum sayısını gösteren küçük bir etiket eklenir (ör. "💬 N").
- **`post-list.component.scss`**: yeni etiket için `.like-button` ile görsel olarak tutarlı, sade bir stil eklenir.

## Test / Doğrulama

Projede backend/frontend test altyapısı yok (bkz. CLAUDE.md "Test Stratejisi — Bilinen Borç"), bu nedenle otomatik test eklenmiyor. Değişiklik sonrası:

- Backend: `dotnet build` ile derleme doğrulanır, `dotnet run` ile API ayağa kaldırılıp `GET /posts` yanıtında `commentCount` alanı manuel kontrol edilir.
- Frontend: `ng serve` ile post listesi sayfası açılıp etiketlerin doğru sayıyla göründüğü manuel doğrulanır.
