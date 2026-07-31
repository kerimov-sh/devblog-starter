# Search UI — Design

## Amaç

Backend'de zaten var olan `GET /search/{arg}?page=&pageSize=` endpoint'ini (`SearchEndpoint` → `IPostService.SearchPostsAsync`) kullanan bir arama deneyimi frontend'e eklemek: navbar'da canlı bir arama kutusu ve sonuçları listeleyen bir `/search` sayfası.

Backend'de değişiklik yapılmayacak; endpoint zaten hedef mimariye uygun (`IPostService`/repository katmanı üzerinden çalışıyor).

## Kapsam

- `PostService`'e `searchPosts(term, page, pageSize)` metodu eklemek.
- `SearchComponent` (route: `/search`) eklemek.
- `app.routes.ts`'e route kaydı eklemek.
- `AppComponent` navbar'ına debounce'lı canlı arama kutusu eklemek.

Kapsam dışı: backend değişikliği, frontend test altyapısı (proje genelinde yok, bu görevde de eklenmiyor).

## Bileşenler

### 1. `PostService.searchPosts` (`devblog-ui/src/app/services/post.service.ts`)

```ts
searchPosts(term: string, page = 1, pageSize = 20) {
  const params = new HttpParams().set('page', page).set('pageSize', pageSize);
  return this.http.get<PagedResult<PostSummary>>(
    `${environment.apiUrl}/search/${encodeURIComponent(term)}`,
    { params }
  );
}
```

Terim `encodeURIComponent` ile encode edilir çünkü backend route'u (`/search/{arg}`) tek bir path segmenti bekliyor; `/` gibi karakterler segment sınırını bozar.

### 2. `SearchComponent` (`devblog-ui/src/app/pages/search/search.component.ts|html|scss`)

`PostListComponent` ile aynı yapı (standalone, `RouterLink` + `CommonModule`, `ActivatedRoute`/`Router` inject edilir):

- `ngOnInit`, `route.queryParamMap` dinler: `q` (arama terimi) ve `page` okunur.
- `q` boş/yalnızca boşluksa: `posts = []`, istek atılmaz, "Arama terimi girin" tarzı boş durum mesajı gösterilir.
- `q` doluysa: `postService.searchPosts(q, page, pageSize)` çağrılır, sonuçlar `PostListComponent`'teki gibi render edilir (başlık, yazar, tarih, tag, like butonu — `toggleLike` de aynı şekilde eklenir).
- Sayfalama: `goToPage(page)` mevcut `q` ile birlikte `router.navigate([], { queryParams: { q, page } })` çağırır (mevcut query param'ları korumak için `queryParamsHandling: 'merge'` kullanılmaz, `q` açıkça verilir).

### 3. Route kaydı (`devblog-ui/src/app/app.routes.ts`)

```ts
{
  path: 'search',
  loadComponent: () =>
    import('./pages/search/search.component').then(m => m.SearchComponent)
}
```

### 4. Navbar arama kutusu (`devblog-ui/src/app/app.component.ts`)

`AppComponent` şu an mantık içermiyor (sadece inline template). Eklenecekler:

- `Router` inject edilir.
- `Subject<string>` (`searchInput$`) üzerinden `debounceTime(300)` + `distinctUntilChanged()` pipe'ı kurulur (constructor'da subscribe edilir).
- Navbar'a bir `<input>` eklenir, `(input)` event'i `searchInput$.next(value)` çağırır.
- Debounce sonrası callback: `term.trim()` boş değilse `router.navigate(['/search'], { queryParams: { q: term, page: 1 } })`; boşsa hiçbir şey yapılmaz (kullanıcı mevcut sayfada kalır).

## Veri Akışı

```
Navbar input → Subject → debounce(300ms) + distinctUntilChanged
  → (term boş değilse) router.navigate(/search?q=term&page=1)
  → SearchComponent.ngOnInit → queryParamMap → PostService.searchPosts
  → sonuçlar render edilir
Sayfalama tıklaması → aynı route'ta page query param günceller → yeniden tetiklenir
```

## Hata Yönetimi

Backend boş/whitespace terimde zaten boş `PagedPostsResponse` döner — ekstra client-side validasyon dışında hata yönetimi gerekmez. HTTP hatalarında mevcut `PostListComponent` deseniyle tutarlı olarak ekstra `error` callback eklenmeyecek (proje genelinde bu seviyede bir hata yönetimi yok).

## Test

Projede frontend test altyapısı yok (CLAUDE.md — bilinen borç). Bu görev kapsamında eklenmeyecek.
