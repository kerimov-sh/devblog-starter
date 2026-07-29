# RAG Chat Backend — Design

**Tarih:** 2026-07-29
**Kapsam:** `docs/` altındaki 12 makaleyle sınırlı, LLM (Claude) tarafından üretilen cevaplar veren bir chat özelliğinin backend (.NET) tarafı. Frontend (Angular chat UI) bu tasarımın kapsamı dışındadır — ayrı bir spec/plan konusu.

## Bağlam

`rag/` altında zaten bir Python indeksleme aracı var ([rag/README.md](../../rag/README.md)):
- `chunking.py` — makaleleri `##` başlıklarına göre chunk'lar.
- `db.py` / `ingest.py` — `rag/rag.db` SQLite dosyasında `documents`, `chunks` tablolarını ve (henüz boş) `vec_chunks` sanal vektör tablosunu oluşturur.
- 12 makale, 70 chunk başarıyla ingest edildi. **Embedding üretimi henüz yapılmadı** (`vec_chunks` boş) — bu tasarımın bir ön koşulu.

Backend mevcut hedef mimariye (CLAUDE.md) uyar: Endpoint → Service → Repository → AppDbContext, endpoint'ler AppDbContext'e doğrudan bağımlı olmaz, request/response DTO'ları `*Request`/`*Response` record'larıdır.

## Ön Koşul: `rag/embed.py`

Backend planına başlamadan önce Python tarafında eksik olan embedding adımı tamamlanmalı:
- `chunks` tablosundaki (henüz `embedded_at IS NULL`) satırlar Voyage API (`voyageai` SDK, `input_type="document"`) ile embed edilir.
- Üretilen vektörler `vec_chunks` sanal tablosuna yazılır, `chunks.embedded_at` güncellenir.
- Model/boyut: `db.py`'de tanımlı `EMBEDDING_DIM = 1024` ile uyumlu bir Voyage modeli (örn. `voyage-3.5`, `output_dimension=1024`).

## Mimari Kararlar (tartışılan ve reddedilen alternatiflerle)

| Karar | Seçim | Reddedilen alternatif(ler) ve gerekçe |
|---|---|---|
| Chat modu | LLM (Claude) üretimli cevap | Sadece retrieval/extractive — "chat" hissini karşılamıyor |
| LLM sağlayıcı | Anthropic Claude (API key sonradan sağlanacak) | — |
| Retrieval | Bellek içi brute-force cosine similarity (70 chunk için trivial) | sqlite-vec extension'ını .NET'ten yükleme — native binding karmaşıklığı, veri boyutu için gereksiz |
| Veri konumu | Chunk + embedding'ler `AppDbContext`'e (`RagChunks` tablosu) taşınır | rag.db'yi olduğu yerden salt-okunur okumak — reddedildi, kullanıcı tek veritabanında toplanmasını tercih etti |
| Servis mimarisi | Endpoint/Service/Repository katmanları (hedef mimariye uygun) | ChatEndpoint içinde AppDbContext'i doğrudan kullanmak — CLAUDE.md'deki "Bilinen Borçlar" kalıbını tekrarlar, reddedildi |
| Python mikroservis | Reddedildi | Veri AppDbContext'e taşındığı için ayrı çalışan bir Python servisine gerek kalmadı |
| Auth | Herkese açık (public) | Sadece giriş yapmış kullanıcılar — makaleler zaten public, gerek görülmedi |
| Yanıt teslimi | Tek seferlik JSON response | Streaming (SSE) — daha basit implementasyon tercih edildi |
| Kapsam | Tüm 12 makale üzerinde genel chat | Sayfa bazlı (tek makaleyle sınırlı) chat — reddedildi |
| Konuşma geçmişi | Tek turlu, hafızasız | Çok turlu (frontend'in geçmişi backend'e göndermesi) — bu round'da kapsam dışı bırakıldı |
| Test | Bu round'da kapsam dışı | xUnit unit testleri — sonraki bir işe bırakıldı |

## Veri Modeli

Yeni EF Core entity, `src/DevBlog.Api/Models/RagChunk.cs`:

```csharp
public class RagChunk
{
    public int Id { get; set; }
    public string DocumentFilename { get; set; } = default!;
    public string DocumentTitle { get; set; } = default!;
    public string Heading { get; set; } = default!;
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = default!;   // "Başlık > Alt başlık\n\n..." önekli
    public byte[] Embedding { get; set; } = default!;  // 1024 float32, ham serileştirme
    public DateTime CreatedAt { get; set; }
}
```

`AppDbContext`'e `DbSet<RagChunk> RagChunks` eklenir. Yeni migration: `AddRagChunks`.

## İçe Aktarma (Import/Seed)

`Program.cs`'deki mevcut desen izlenir:

```csharp
db.Database.Migrate();
DataSeeder.Seed(db);
RagChunkSeeder.Seed(db, ragDbPath);  // yeni
```

`RagChunkSeeder.Seed`:
- `RagChunks` tablosu boşsa VE configlenebilir `ragDbPath` (varsayılan: `../../rag/rag.db`, `appsettings.json`'da override edilebilir) dosyası mevcutsa çalışır.
- `Microsoft.Data.Sqlite` ile `rag.db`'ye ayrı, salt-okunur bir bağlantı açar.
- `chunks` tablosunu `vec_chunks` ile `chunk_id` üzerinden join edip `documents` ile filename/title alır, `embedding` FLOAT[1024] vektörünü byte dizisine çevirip `RagChunks`'a yazar.
- Idempotent: tablo doluysa hiçbir şey yapmaz (yeniden indexleme gerektiğinde tablo manuel temizlenip API yeniden başlatılır — bu starter proje için yeterli, otomatik senkronizasyon kapsam dışı).
- `rag.db` bulunamazsa veya embed edilmemiş chunk'lar varsa, uygulama başlangıcını **engellemez**; sadece log uyarısı yazar (chat endpoint'i bu durumda boş context ile çalışır, aşağıdaki hata yönetimine bakın).

## Dış API İstemcileri

`src/DevBlog.Api/Services/External/`:

- `IVoyageEmbeddingClient` / `VoyageEmbeddingClient` — `HttpClient` ile Voyage `/v1/embeddings`'e POST. Kullanıcı sorgusu `input_type="query"` ile embed edilir.
- `IClaudeChatClient` / `ClaudeChatClient` — `HttpClient` ile Anthropic `/v1/messages`'a POST.
- Her ikisi de `Program.cs`'de `AddHttpClient<TInterface, TImplementation>` ile named/typed client olarak kayıt edilir.
- API key'ler `appsettings.json`'da düz metin **tutulmaz**; `dotnet user-secrets` (dev) veya ortam değişkenleri (`Voyage__ApiKey`, `Anthropic__ApiKey`) ile sağlanır. Bu, mevcut JWT secret hardcoded anti-pattern'inin (CLAUDE.md "Bilinen Borçlar") tekrarlanmaması için bilinçli bir karardır.
- Anthropic API key kullanıcı tarafından **henüz sağlanmadı**; config bu key'i bekleyen bir placeholder olarak bırakılır (aşağıya bakın, "Hata Yönetimi").

## Servis Akışı — `IChatService` / `ChatService`

`POST /chat` çağrıldığında:

1. `IRagChunkRepository`, tüm `RagChunks` satırlarını döner; sonuç `ChatService` içinde (veya ayrı bir `IRagChunkCache` singleton'ında) process ömrü boyunca bellekte cache'lenir — 70 satır için önemsiz bellek maliyeti, her istekte DB round-trip'i önler.
2. Kullanıcı mesajı `IVoyageEmbeddingClient.EmbedQueryAsync` ile embed edilir.
3. Cache'deki embedding'lerle bellek içi cosine similarity hesaplanır, en yüksek skorlu **top-5 chunk** seçilir.
4. Seçilen chunk'ların `Content`'i + kullanıcı sorusu, aşağıdaki gibi bir system prompt ile `IClaudeChatClient.GetAnswerAsync`'e gönderilir:
   > "Sadece aşağıda verilen makale alıntılarını kullanarak cevap ver. Cevap alıntılarda yoksa, bilmediğini söyle, uydurma."
5. Claude'un cevabı + kullanılan chunk'ların kaynak bilgisi (`DocumentTitle`, `Heading`, `DocumentFilename`) `ChatResponse` olarak döner.

## Endpoint

`src/DevBlog.Api/Endpoints/ChatEndpoint.cs`:

```
POST /chat
Request:  ChatRequest { string Message }
Response: ChatResponse {
    string Answer,
    List<ChatSourceResponse> Sources   // { DocumentTitle, Heading, DocumentFilename }
}
```

- Auth gerektirmez (`[Authorize]` yok) — kararlaştırıldığı gibi herkese açık.
- Naming convention: `ChatRequest`/`ChatResponse`/`ChatSourceResponse`, `ChatEndpoint.Map(WebApplication app)`.

## Hata Yönetimi

| Durum | Davranış |
|---|---|
| Boş/whitespace `Message` | `400 Bad Request` |
| Anthropic veya Voyage API key config'de yok | `503 Service Unavailable` + "Chat şu anda yapılandırılmamış" mesajı (geliştirme sırasında anlaşılır olması için — key kullanıcı tarafından sonradan eklenecek) |
| `RagChunks` tablosu boş (embed/import henüz yapılmamış) | `503 Service Unavailable` + "İçerik indeksi hazır değil" mesajı |
| Voyage/Claude API çağrısı başarısız (timeout, 4xx/5xx) | `502 Bad Gateway`; ham hata detayı client'a sızdırılmaz, sunucu tarafında loglanır |
| Retrieval sonucu düşük benzerlikli | Yine de Claude'a gönderilir; system prompt zaten "bilmiyorsan söyle" talimatı içerir |

## Kapsam Dışı (bu round için)

- Frontend chat UI (ayrı spec/plan).
- Çok turlu konuşma geçmişi.
- Streaming yanıt (SSE).
- xUnit testleri (ChatService/repository/client'lar için — hedef test stratejisine sonraki bir işte adım atılabilir).
- rag.db → RagChunks otomatik yeniden senkronizasyon (şu an tek seferlik, tablo boşsa import).
