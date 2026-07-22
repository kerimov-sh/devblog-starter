# Frontend Plan: Global Comments List Page (`GET /comments`)

## Context

Backend'de yeni bir global `GET /comments?page=&pageSize=` endpoint'i ekleniyor (bkz. uncommitted `CommentsEndpoint.cs`, `ICommentService`/`CommentService`, `ICommentRepository` — `src/DevBlog.Api/Services/`, `Repositories/`). Bu endpoint post'a özel değil: sistemdeki **tüm** yorumları, sayfalı ve her birine ait post bilgisiyle (`PostId`, `PostSlug`, `PostTitle`) birlikte döner. Kullanıcıyla netleştirildi: bu, giriş yapmış herkesin erişebileceği bir **admin/moderasyon tipi liste sayfası** olacak, `/comments` route'unda, guard olmadan (proje şu an hiç route guard içermiyor, tutarlılık için bu ilk guard olmayacak).

Amaç: mevcut mimariye (flat `pages/`+`services/` yapısı, `PostService`/`PostListComponent` pagination deseni) birebir uyan, minimum yeni soyutlama ekleyen bir uygulama planı çıkarmak.

## Mevcut Konvansiyonlar (doğrulandı)

- `src/app/services/post.service.ts`: DTO interface'leri (`PagedResult<T>`, `PostSummary`, `PostDetail`, `Comment`) dosyanın üstünde, servis sınıfının kendisi altında tanımlı. Ayrı bir `models/` klasörü yok.
- `PostService.getPosts(page = 1, pageSize = 20)` → `HttpParams` ile `GET /posts` çağırıyor, `PagedResult<PostSummary>` dönüyor. `catchError`/hata yönetimi yok, Observable doğrudan dönülüyor.
- `src/app/app.routes.ts`: her sayfa `loadComponent` ile lazy-load ediliyor (`posts`, `posts/:slug`, `login`).
- `src/app/app.component.ts`: nav inline template içinde, `<a routerLink="...">` linkleri `|` ile ayrılmış (`Posts | Login`).
- `post-list.component.ts/.html/.scss`: query-param senkronize pagination (`route.queryParamMap.subscribe`, `router.navigate([], { queryParams: { page } })`), abone olduktan sonra `cdr.detectChanges()` çağrısı, loading/error state'i **yok** — bu, mevcut tek referans pattern.

## Uygulama Planı

### 1. Yeni servis: `devblog-ui/src/app/services/comment.service.ts`

Ayrı bir `CommentService` — `PostService`'e eklenmiyor. Gerekçe: yeni endpoint post-scoped değil, comment-domain sorgusu; `PostDetail.comments`'te kullanılan mevcut `Comment` interface'i (`{id, authorName, body, createdAt}`) ile çakışmaması için yeni DTO farklı adla (`CommentListItem`) tanımlanacak.

```typescript
import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CommentListItem {
  id: number;
  authorName: string;
  body: string;
  createdAt: string;
  postId: number;
  postSlug: string;
  postTitle: string;
}

@Injectable({ providedIn: 'root' })
export class CommentService {
  private http = inject(HttpClient);

  getComments(page = 1, pageSize = 20) {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<CommentListItem>>(`${environment.apiUrl}/comments`, { params });
  }
}
```

`PagedResult<T>` burada tekrar tanımlanıyor (import edilmiyor) — mevcut "her servis dosyası kendi kendine yeterli" deseniyle tutarlı, tek dosyadan paylaşılan generic çıkarmak için henüz üçüncü bir sayfalı domain yok.

### 2. Yeni sayfa: `devblog-ui/src/app/pages/comment-list/`

**`comment-list.component.ts`** — `post-list.component.ts` ile yapısal olarak birebir (pagination state, query-param sync, `cdr.detectChanges()`):

```typescript
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { CommentService, CommentListItem } from '../../services/comment.service';

@Component({
  selector: 'app-comment-list',
  standalone: true,
  imports: [RouterLink, CommonModule],
  templateUrl: './comment-list.component.html',
  styleUrl: './comment-list.component.scss'
})
export class CommentListComponent implements OnInit {
  private commentService = inject(CommentService);
  private cdr = inject(ChangeDetectorRef);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  comments: CommentListItem[] = [];
  currentPage = 1;
  pageSize = 20;
  totalPages = 0;
  totalCount = 0;

  ngOnInit() {
    this.route.queryParamMap.subscribe((params) => {
      const page = Number(params.get('page')) || 1;
      this.loadComments(page);
    });
  }

  goToPage(page: number) {
    if (page < 1 || page > this.totalPages || page === this.currentPage) {
      return;
    }
    this.router.navigate([], { queryParams: { page } });
  }

  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  private loadComments(page: number) {
    this.commentService.getComments(page, this.pageSize).subscribe((result) => {
      this.comments = result.items;
      this.currentPage = result.page;
      this.pageSize = result.pageSize;
      this.totalPages = result.totalPages;
      this.totalCount = result.totalCount;
      this.cdr.detectChanges();
    });
  }
}
```

**`comment-list.component.html`**:

```html
<h1>Comments</h1>
<ul>
  @for (comment of comments; track comment.id) {
    <li>
      <p>{{ comment.body }}</p>
      <small>
        {{ comment.authorName }} — {{ comment.createdAt | date:'medium' }}
        on <a [routerLink]="['/posts', comment.postSlug]">{{ comment.postTitle }}</a>
      </small>
    </li>
  }
</ul>

@if (totalPages > 1) {
  <p class="pagination-info">Sayfa {{ currentPage }} / {{ totalPages }} — toplam {{ totalCount }} yorum</p>
  <nav class="pagination">
    @for (p of pageNumbers; track p) {
      <button [class.active]="p === currentPage" [disabled]="p === currentPage" (click)="goToPage(p)">{{ p }}</button>
    }
  </nav>
}
```

- `postSlug` → mevcut `posts/:slug` route'una link (post-detail sayfasına götürür).
- `postTitle` → link metni; `postId` UI'da gösterilmiyor (routing için gerek yok).
- Silme/moderasyon aksiyonu yok — istenmedi, kapsam dışı.

**`comment-list.component.scss`**: `post-list.component.scss`'teki `.pagination-info` / `.pagination button...` kurallarının aynısı — iki liste sayfası arasında görsel tutarlılık için.

**Loading/error state eklenmiyor**: `post-list.component`'te de yok; tutarsızlık yaratmamak için burada da eklenmiyor (istenirse ayrı bir görev olarak her iki sayfaya birden eklenmeli).

### 3. Route: `devblog-ui/src/app/app.routes.ts`

`posts/:slug` ile `login` arasına ekle:

```typescript
{
  path: 'comments',
  loadComponent: () =>
    import('./pages/comment-list/comment-list.component').then(m => m.CommentListComponent)
}
```

### 4. Nav linki: `devblog-ui/src/app/app.component.ts`

```typescript
template: `
  <nav>
    <a routerLink="/posts">Posts</a> |
    <a routerLink="/comments">Comments</a> |
    <a routerLink="/login">Login</a>
  </nav>
  <main>
    <router-outlet />
  </main>
`
```

## Kapsam Dışı (bilinçli olarak)

- Silme/düzenleme/moderasyon aksiyonları.
- Route guard / rol kontrolü (proje genelinde hiç guard yok, ilk guard'ı burada eklemek istenmedi).
- Yeni `models/` klasörü veya paylaşılan pagination component'i — mevcut flat yapı korunuyor.

## Doğrulama

1. Backend'de `GET /comments` endpoint'i çalışır hale geldiğinde (`dotnet run --project src/DevBlog.Api/DevBlog.Api.csproj`), frontend'i başlat (`cd devblog-ui && npm start`).
2. `/comments` route'una git, sayfalı yorum listesinin yüklendiğini doğrula.
3. Bir yorumun `PostTitle` linkine tıklayıp doğru `posts/:slug` sayfasına yönlendiğini doğrula.
4. Sayfalama butonlarıyla (`page=2` vb.) query param'ın URL'e yansıdığını ve listenin güncellendiğini doğrula.
5. Nav'daki "Comments" linkinin `/comments`'e yönlendirdiğini doğrula.
