# Comment Count Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `CommentCount` field to post responses and show it as a badge on each post card in the UI.

**Architecture:** Mirror the existing `LikeCount` pattern end to end: a repository method that batch-counts comments per post ID, wired through `PostService` into `PostSummaryResponse`, plus a one-line addition to the `GET /posts/{slug}` projection. Frontend adds the field to the `PostSummary` type and renders it next to the existing like button.

**Tech Stack:** .NET 10 Minimal API + EF Core (SQLite), Angular 22 standalone components.

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-31-comment-count-design.md`
- No persisted `CommentCount` column — it is computed at query time from the `Comments` table, exactly like `LikeCount` is computed from `PostLikes`.
- `GET /posts/{slug}` is documented known debt (`AppDbContext` injected directly into the endpoint) — do not refactor it, only add the one field to its existing projection.
- No backend or frontend test project exists in this repo (CLAUDE.md "Test Stratejisi — Bilinen Borç"). Verification is: `dotnet build` for backend compile-correctness, and manual `dotnet run` / `ng serve` checks for behavior — not automated tests.
- Response DTOs use the `*Response` naming convention (already followed by `PostSummaryResponse`).
- Commit messages follow this repo's style: short, lowercase, `feat:`/`fix:` prefix.

---

### Task 1: Repository — batch comment counts

**Files:**
- Modify: `src/DevBlog.Api/Repositories/ICommentRepository.cs`
- Modify: `src/DevBlog.Api/Repositories/CommentRepository.cs`

**Interfaces:**
- Produces: `Task<Dictionary<int, int>> GetCommentCountsAsync(IEnumerable<int> postIds)` — maps `postId -> commentCount`, missing keys mean zero comments (same contract as `ILikeRepository.GetLikeCountsAsync`).

- [ ] **Step 1: Add the method to `ICommentRepository`**

```csharp
using DevBlog.Api.Models;

namespace DevBlog.Api.Repositories;

public interface ICommentRepository
{
    Task<(IReadOnlyList<Comment> Comments, int TotalCount)> GetAllAsync(int page, int pageSize);
    Task<Dictionary<int, int>> GetCommentCountsAsync(IEnumerable<int> postIds);
}
```

- [ ] **Step 2: Implement it in `CommentRepository`**

Add this method to the class, following the same shape as `LikeRepository.GetLikeCountsAsync`:

```csharp
public async Task<Dictionary<int, int>> GetCommentCountsAsync(IEnumerable<int> postIds)
{
    var ids = postIds.ToList();

    return await db.Comments
        .Where(c => ids.Contains(c.PostId))
        .GroupBy(c => c.PostId)
        .Select(g => new { PostId = g.Key, Count = g.Count() })
        .ToDictionaryAsync(x => x.PostId, x => x.Count);
}
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build DevBlog.slnx`
Expected: `Build succeeded.` with no errors.

- [ ] **Step 4: Commit**

```bash
git add src/DevBlog.Api/Repositories/ICommentRepository.cs src/DevBlog.Api/Repositories/CommentRepository.cs
git commit -m "feat: add batch comment count query to CommentRepository"
```

---

### Task 2: Wire CommentCount into post list/search responses

**Files:**
- Modify: `src/DevBlog.Api/Services/PostService.cs`
- Modify: `src/DevBlog.Api/Endpoints/PostsEndpoint.cs:66-75` (`PostSummaryResponse` record)

**Interfaces:**
- Consumes: `ICommentRepository.GetCommentCountsAsync(IEnumerable<int> postIds)` from Task 1.
- Produces: `PostSummaryResponse` now has a trailing `int CommentCount` property; later frontend tasks depend on the wire field name `commentCount` (camelCase, via default ASP.NET JSON serialization).

- [ ] **Step 1: Add `CommentCount` to `PostSummaryResponse`**

In `src/DevBlog.Api/Endpoints/PostsEndpoint.cs`, change:

```csharp
public record PostSummaryResponse(
    int Id,
    string Title,
    string Slug,
    string Tags,
    DateTime PublishedAt,
    string Author,
    int LikeCount,
    bool LikedByCurrentUser
);
```

to:

```csharp
public record PostSummaryResponse(
    int Id,
    string Title,
    string Slug,
    string Tags,
    DateTime PublishedAt,
    string Author,
    int LikeCount,
    bool LikedByCurrentUser,
    int CommentCount
);
```

- [ ] **Step 2: Inject `ICommentRepository` into `PostService` and populate the field**

In `src/DevBlog.Api/Services/PostService.cs`, change the class declaration:

```csharp
public class PostService(IPostRepository postRepository, ILikeRepository likeRepository, ICommentRepository commentRepository) : IPostService
```

In `GetPostsAsync`, after the existing `likeCounts`/`likedPostIds` lookups, add:

```csharp
var commentCounts = await commentRepository.GetCommentCountsAsync(postIds);
```

and update the `items` projection to:

```csharp
var items = posts.Select(p => new PostSummaryResponse(
    p.Id, p.Title, p.Slug, p.Tags, p.PublishedAt, p.Author.Username,
    likeCounts.GetValueOrDefault(p.Id), likedPostIds.Contains(p.Id),
    commentCounts.GetValueOrDefault(p.Id)
)).ToList();
```

Apply the identical two changes (add `commentCounts` lookup, extend the `items` projection with `commentCounts.GetValueOrDefault(p.Id)`) inside `SearchPostsAsync`, which has the same shape.

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build DevBlog.slnx`
Expected: `Build succeeded.` with no errors.

- [ ] **Step 4: Manually verify the field appears in `GET /posts`**

Run: `dotnet run --project src/DevBlog.Api/DevBlog.Api.csproj` in the background, wait for `Now listening on:` in the output, then:

```bash
curl -s http://localhost:5000/posts | grep -o '"commentCount":[0-9]*'
```

(Adjust the port to whatever the console output shows if not 5000.)

Expected: at least one `"commentCount":<number>` in the output, matching the seeded comment counts. Stop the running `dotnet run` process afterward.

- [ ] **Step 5: Commit**

```bash
git add src/DevBlog.Api/Services/PostService.cs src/DevBlog.Api/Endpoints/PostsEndpoint.cs
git commit -m "feat: include CommentCount in post summary responses"
```

---

### Task 3: Add CommentCount to the single-post projection

**Files:**
- Modify: `src/DevBlog.Api/Endpoints/PostsEndpoint.cs:20-50` (`GET /posts/{slug}`)

**Interfaces:**
- Consumes: nothing new — `p.Comments` is already `Include`d on this query.
- Produces: the `/posts/{slug}` JSON payload gains a `commentCount` field (in addition to the existing `comments` array), for consistency with the list/search responses from Task 2.

- [ ] **Step 1: Add `CommentCount` to the anonymous projection**

In the `GET /posts/{slug}` handler, change the `Select` projection to add `CommentCount` right after `Comments`:

```csharp
.Select(p => new
{
    p.Id,
    p.Title,
    p.Content,
    p.Slug,
    p.Tags,
    p.PublishedAt,
    Author = p.Author.Username,
    Comments = p.Comments.OrderBy(c => c.CreatedAt).Select(c => new
    {
        c.Id,
        c.AuthorName,
        c.Body,
        c.CreatedAt
    }),
    CommentCount = p.Comments.Count(),
    LikeCount = db.PostLikes.Count(l => l.PostId == p.Id),
    LikedByCurrentUser = currentUserId != null && db.PostLikes.Any(l => l.PostId == p.Id && l.UserId == currentUserId)
})
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build DevBlog.slnx`
Expected: `Build succeeded.` with no errors.

- [ ] **Step 3: Manually verify the field appears in `GET /posts/{slug}`**

With the API running (`dotnet run --project src/DevBlog.Api/DevBlog.Api.csproj` in the background), pick a seeded slug and run:

```bash
curl -s http://localhost:5000/posts/<a-seeded-slug> | grep -o '"commentCount":[0-9]*'
```

Expected: `"commentCount":<number>` matching the length of the `comments` array in the same response. Stop the running `dotnet run` process afterward.

- [ ] **Step 4: Commit**

```bash
git add src/DevBlog.Api/Endpoints/PostsEndpoint.cs
git commit -m "feat: include CommentCount in single post response"
```

---

### Task 4: Frontend — add commentCount to PostSummary

**Files:**
- Modify: `devblog-ui/src/app/services/post.service.ts:13-22` (`PostSummary` interface)

**Interfaces:**
- Consumes: the `commentCount` JSON field produced by Task 2/3.
- Produces: `PostSummary.commentCount: number` (and by extension `PostDetail.commentCount`, since `PostDetail extends PostSummary`), consumed by Task 5's template.

- [ ] **Step 1: Add the field to `PostSummary`**

```typescript
export interface PostSummary {
  id: number;
  title: string;
  slug: string;
  tags: string;
  publishedAt: string;
  author: string;
  likeCount: number;
  likedByCurrentUser: boolean;
  commentCount: number;
}
```

- [ ] **Step 2: Build to verify the frontend still compiles**

Run (from `devblog-ui/`): `npx tsc --noEmit -p tsconfig.app.json`
Expected: no new errors reported.

- [ ] **Step 3: Commit**

```bash
git add devblog-ui/src/app/services/post.service.ts
git commit -m "feat: add commentCount to PostSummary type"
```

---

### Task 5: Frontend — show comment count badge on post cards

**Files:**
- Modify: `devblog-ui/src/app/pages/post-list/post-list.component.html:12-19`
- Modify: `devblog-ui/src/app/pages/post-list/post-list.component.scss`

**Interfaces:**
- Consumes: `PostSummary.commentCount` from Task 4, `post` template variable already in scope (`@for (post of posts; track post.id)`).

- [ ] **Step 1: Add the badge markup next to the like button**

In `post-list.component.html`, change:

```html
          <button
            type="button"
            class="btn btn-sm like-button"
            [class.liked]="post.likedByCurrentUser"
            (click)="toggleLike(post)">
            {{ post.likedByCurrentUser ? '♥' : '♡' }} {{ post.likeCount }}
          </button>
```

to:

```html
          <div class="d-flex align-items-center gap-2">
            <button
              type="button"
              class="btn btn-sm like-button"
              [class.liked]="post.likedByCurrentUser"
              (click)="toggleLike(post)">
              {{ post.likedByCurrentUser ? '♥' : '♡' }} {{ post.likeCount }}
            </button>
            <span class="comment-count-badge">💬 {{ post.commentCount }}</span>
          </div>
```

- [ ] **Step 2: Style the badge in `post-list.component.scss`**

Add this rule, next to the existing `.like-button` rule:

```scss
.comment-count-badge {
  display: inline-flex;
  align-items: center;
  padding: 0.2rem 0.6rem;
  border: 1px solid #ccc;
  border-radius: 4px;
  color: #666;
  font-size: 0.85rem;
}
```

- [ ] **Step 3: Manually verify in the browser**

Run (from `devblog-ui/`): `npm start`, then open the app's post list page.
Expected: each post card shows a "💬 N" badge next to the like button, with `N` matching that post's actual comment count (cross-check by opening the post detail page and counting the comments shown there). Stop the dev server afterward.

- [ ] **Step 4: Commit**

```bash
git add devblog-ui/src/app/pages/post-list/post-list.component.html devblog-ui/src/app/pages/post-list/post-list.component.scss
git commit -m "feat: show comment count badge on post cards"
```

---

### Task 6: End-to-end verification

**Files:** none (verification only)

- [ ] **Step 1: Run backend and frontend together**

Terminal 1: `dotnet run --project src/DevBlog.Api/DevBlog.Api.csproj`
Terminal 2 (from `devblog-ui/`): `npm start`

- [ ] **Step 2: Verify the golden path**

Open the frontend post list page. Confirm every post card shows a comment badge with a plausible count. Open one post's detail page, count its comments, navigate back, and confirm the list badge for that post matches.

- [ ] **Step 3: Verify an edge case**

Find or create (via `POST /posts`) a post with zero comments. Confirm its badge reads "💬 0" rather than being blank or erroring.

- [ ] **Step 4: Stop both dev processes**

Stop the `dotnet run` and `npm start` processes started in Step 1.
