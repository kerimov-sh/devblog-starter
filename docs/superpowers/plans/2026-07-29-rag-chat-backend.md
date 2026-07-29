# RAG Chat Backend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Makalelerle (docs/01-12) sınırlı, Claude'un ürettiği cevaplar veren, herkese açık, tek turlu bir `POST /chat` backend endpoint'i kurmak.

**Architecture:** Python (`rag/`) chunk+embedding üretiminin tek sahibi kalır; embed edilmiş veri `.NET` başlangıcında `AppDbContext`'teki yeni `RagChunks` tablosuna tek seferlik import edilir. Chat isteğinde backend, kullanıcı sorusunu Voyage ile embed eder, bellek içi cosine similarity ile en alakalı 5 chunk'ı seçer, Claude'a bağlam olarak verir ve cevabı+kaynakları döner. Endpoint/Service/Repository katmanlaması korunur; endpoint AppDbContext'e doğrudan bağımlı olmaz.

**Tech Stack:** Python 3 + `voyageai` SDK (embedding, mevcut), .NET 10 Minimal API + EF Core (Sqlite) + `Microsoft.Data.Sqlite` (rag.db'yi okumak için) + düz `HttpClient` (Voyage/Anthropic REST çağrıları, ek SDK yok).

## Global Constraints

- Endpoint katmanı AppDbContext'e doğrudan enjekte edilmez; sadece Service arayüzlerini kullanır (CLAUDE.md hedef mimarisi).
- Request/response DTO'ları `*Request`/`*Response` record'ları olarak ilgili `Endpoint` dosyasının altında tanımlanır (bkz. `PostsEndpoint.cs`, `LikesEndpoint.cs`).
- API key'ler asla `appsettings.json`'a düz metin yazılmaz veya commit edilmez; `dotnet user-secrets` (dev) ile sağlanır.
- Chat endpoint'i `[Authorize]` gerektirmez (herkese açık), tek seferlik JSON response döner (streaming yok), tek turlu çalışır (konuşma geçmişi tutulmaz), retrieval her zaman 12 makalenin tamamı üzerinde çalışır.
- Retrieval, sqlite-vec/native extension'a bağımlı olmadan bellek içi brute-force cosine similarity ile yapılır (top-5 chunk).
- Bu round'da yeni xUnit testi yazılmaz (kullanıcı kararı) — doğrulama `dotnet build` + manuel `curl`/SQL sorgusu ile yapılır.

---

## Task 1: Python — embedding pipeline'ı tamamla (`rag/embed.py`)

**Files:**
- Modify: `rag/db.py` (`init_schema` fonksiyonu — `chunks` tablosuna `embedding`/`embedding_model` kolonları ekle)
- Create: `rag/embed.py`

**Interfaces:**
- Consumes: `rag/db.py`'deki `connect(db_path) -> sqlite3.Connection`, `init_schema(conn)`, `EMBEDDING_DIM` (1024).
- Produces: `chunks` tablosunda her satır için dolu `embedding` (BLOB, float32 little-endian), `embedding_model` (TEXT), `embedded_at` (TEXT) kolonları. Bu veri Task 3'teki `RagChunkSeeder` tarafından okunacak — kolon adları (`embedding`, `embedding_model`) ve tip (float32 little-endian ham bayt dizisi) tam bu şekilde kalmalı.

- [ ] **Step 1: `rag/db.py`'ye `embedding`/`embedding_model` kolonlarını idempotent şekilde ekle**

`rag/db.py` içindeki `init_schema` fonksiyonunu şu şekilde değiştir:

```python
def init_schema(conn: sqlite3.Connection) -> None:
    conn.executescript(_SCHEMA)

    existing_columns = {row["name"] for row in conn.execute("PRAGMA table_info(chunks)")}
    if "embedding" not in existing_columns:
        conn.execute("ALTER TABLE chunks ADD COLUMN embedding BLOB")
    if "embedding_model" not in existing_columns:
        conn.execute("ALTER TABLE chunks ADD COLUMN embedding_model TEXT")

    existing = conn.execute(
        "SELECT name FROM sqlite_master WHERE type='table' AND name='vec_chunks'"
    ).fetchone()
    if existing is None:
        conn.execute(
            f"""
            CREATE VIRTUAL TABLE vec_chunks USING vec0(
                chunk_id INTEGER PRIMARY KEY,
                embedding FLOAT[{EMBEDDING_DIM}]
            )
            """
        )

    conn.commit()
```

(`_SCHEMA`, `connect`, `EMBEDDING_DIM` değişmeden kalır.)

- [ ] **Step 2: `rag/embed.py` dosyasını oluştur**

```python
"""chunks tablosundaki embed edilmemiş satırları Voyage API ile embed eder.

chunks.embedding (düz BLOB kolon) asıl tüketici olan .NET backend'in
RagChunkSeeder'ı tarafından okunur — sqlite-vec native extension'ına .NET
tarafında ihtiyaç duyulmaması için bilinçli olarak düz bir kolon kullanılır.
vec_chunks (sqlite-vec sanal tablosu) olası ileride Python tarafı sorgu
araçları için ayrıca doldurulur.

Kullanım:
    python embed.py [--db rag.db]
"""

from __future__ import annotations

import argparse
import array
import os
import sqlite3
from pathlib import Path

import voyageai
from dotenv import load_dotenv

from db import EMBEDDING_DIM, connect, init_schema

MODEL = "voyage-3.5"
BATCH_SIZE = 8


def _pack_embedding(values: list[float]) -> bytes:
    # float32 little-endian ham bayt dizisi: Python'ın array('f') ve .NET'in
    # Buffer.BlockCopy<float[]> yöntemi x86_64'te aynı bellek düzenini
    # kullanır, bu yüzden ek bir serileştirme formatına gerek yok.
    return array.array("f", values).tobytes()


def fetch_unembedded_chunks(conn: sqlite3.Connection) -> list[sqlite3.Row]:
    return conn.execute(
        "SELECT id, content FROM chunks WHERE embedded_at IS NULL ORDER BY id"
    ).fetchall()


def embed_and_store(
    conn: sqlite3.Connection, client: voyageai.Client, rows: list[sqlite3.Row]
) -> int:
    embedded_count = 0

    for start in range(0, len(rows), BATCH_SIZE):
        batch = rows[start : start + BATCH_SIZE]
        texts = [row["content"] for row in batch]

        result = client.embed(
            texts, model=MODEL, input_type="document", output_dimension=EMBEDDING_DIM
        )

        for row, embedding in zip(batch, result.embeddings):
            packed = _pack_embedding(embedding)

            conn.execute(
                """
                UPDATE chunks
                SET embedding = ?, embedding_model = ?, embedded_at = datetime('now')
                WHERE id = ?
                """,
                (packed, MODEL, row["id"]),
            )
            conn.execute(
                "INSERT OR REPLACE INTO vec_chunks (chunk_id, embedding) VALUES (?, ?)",
                (row["id"], packed),
            )
            embedded_count += 1

        conn.commit()
        print(f"  {embedded_count}/{len(rows)} chunk embed edildi")

    return embedded_count


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--db", type=Path, default=Path(__file__).parent / "rag.db")
    args = parser.parse_args()

    load_dotenv(Path(__file__).parent / ".env")
    api_key = os.environ.get("VOYAGE_API_KEY")
    if not api_key:
        raise SystemExit("VOYAGE_API_KEY .env dosyasında bulunamadı.")

    conn = connect(args.db)
    init_schema(conn)

    rows = fetch_unembedded_chunks(conn)
    if not rows:
        print("Embed edilecek chunk yok (hepsi zaten embed edilmiş).")
        conn.close()
        return

    client = voyageai.Client(api_key=api_key)
    total = embed_and_store(conn, client, rows)
    conn.close()

    print(f"\nTamamlandı: {total} chunk embed edildi -> {args.db}")


if __name__ == "__main__":
    main()
```

- [ ] **Step 3: Çalıştır ve doğrula**

Run (rag/ dizininde, venv aktifken):
```bash
./.venv/Scripts/python.exe embed.py
```
Expected: `Tamamlandı: 70 chunk embed edildi -> ...rag.db`

Doğrulama:
```bash
./.venv/Scripts/python.exe -c "
import sqlite3
conn = sqlite3.connect('rag.db')
print('embedded chunks:', conn.execute('SELECT COUNT(*) FROM chunks WHERE embedding IS NOT NULL').fetchone()[0])
print('vec_chunks rows:', conn.execute('SELECT COUNT(*) FROM vec_chunks').fetchone()[0])
print('embedding byte length (should be 4096 = 1024*4):', conn.execute('SELECT LENGTH(embedding) FROM chunks LIMIT 1').fetchone()[0])
"
```
Expected: `embedded chunks: 70`, `vec_chunks rows: 70`, `embedding byte length: 4096`.

- [ ] **Step 4: Commit**

```bash
git add rag/db.py rag/embed.py
git commit -m "feat(rag): add embed.py to populate chunk embeddings via Voyage API"
```

---

## Task 2: `RagChunk` entity + AppDbContext + EF migration

**Files:**
- Create: `src/DevBlog.Api/Models/RagChunk.cs`
- Modify: `src/DevBlog.Api/Data/AppDbContext.cs`
- Create (via `dotnet ef`): `src/DevBlog.Api/Migrations/*_AddRagChunks.cs` (+ Designer.cs, ModelSnapshot güncellemesi)

**Interfaces:**
- Produces: `RagChunk` sınıfı (`Id`, `DocumentFilename`, `DocumentTitle`, `Heading`, `ChunkIndex`, `Content`, `Embedding` (`byte[]`), `CreatedAt`) ve `AppDbContext.RagChunks` (`DbSet<RagChunk>`) — Task 3 (seeder) ve Task 5'teki (`IRagChunkRepository`) tüm sonraki task'lar bu isimleri birebir kullanır.

- [ ] **Step 1: `RagChunk.cs` oluştur**

```csharp
namespace DevBlog.Api.Models;

public class RagChunk
{
    public int Id { get; set; }
    public string DocumentFilename { get; set; } = "";
    public string DocumentTitle { get; set; } = "";
    public string Heading { get; set; } = "";
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = "";
    public byte[] Embedding { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
```

- [ ] **Step 2: `AppDbContext.cs`'e `DbSet` ve unique index ekle**

`src/DevBlog.Api/Data/AppDbContext.cs` içinde, `public DbSet<PostLike> PostLikes => Set<PostLike>();` satırının altına ekle:

```csharp
    public DbSet<RagChunk> RagChunks => Set<RagChunk>();
```

`OnModelCreating` içinde, `PostLike` unique index bloğunun altına ekle:

```csharp
        modelBuilder.Entity<RagChunk>()
            .HasIndex(c => new { c.DocumentFilename, c.ChunkIndex })
            .IsUnique();
```

- [ ] **Step 3: Migration oluştur**

Run:
```bash
dotnet ef migrations add AddRagChunks --project src/DevBlog.Api
```
Expected: `src/DevBlog.Api/Migrations/` altında `<timestamp>_AddRagChunks.cs` ve `.Designer.cs` dosyaları oluşur, `AppDbContextModelSnapshot.cs` güncellenir.

- [ ] **Step 4: Build ile doğrula**

Run: `dotnet build DevBlog.slnx`
Expected: Build succeeded, hata yok.

- [ ] **Step 5: Commit**

```bash
git add src/DevBlog.Api/Models/RagChunk.cs src/DevBlog.Api/Data/AppDbContext.cs src/DevBlog.Api/Migrations/
git commit -m "feat: add RagChunk entity and EF migration"
```

---

## Task 3: `RagChunkSeeder` — rag.db'den import

**Files:**
- Modify: `src/DevBlog.Api/DevBlog.Api.csproj` (explicit `Microsoft.Data.Sqlite` referansı)
- Create: `src/DevBlog.Api/Data/RagChunkSeeder.cs`
- Modify: `src/DevBlog.Api/appsettings.json` (`Rag:DbPath`)
- Modify: `src/DevBlog.Api/Program.cs` (seed çağrısı)

**Interfaces:**
- Consumes: `AppDbContext.RagChunks` (Task 2), `RagChunk` model (Task 2), `rag/rag.db`'deki `documents`/`chunks` tabloları + `chunks.embedding` kolonu (Task 1).
- Produces: `RagChunkSeeder.Seed(AppDbContext db, string ragDbPath)` — Task 5+6'da doğrudan kullanılmaz ama `RagChunks` tablosunun API başlangıcında dolu olmasını garanti eder.

- [ ] **Step 1: `Microsoft.Data.Sqlite` paket referansını ekle**

`src/DevBlog.Api/DevBlog.Api.csproj` içindeki `<ItemGroup>`'a, `Microsoft.EntityFrameworkCore.Sqlite` satırının altına ekle:

```xml
    <PackageReference Include="Microsoft.Data.Sqlite" Version="10.0.0" />
```

- [ ] **Step 2: `RagChunkSeeder.cs` oluştur**

```csharp
using DevBlog.Api.Models;
using Microsoft.Data.Sqlite;

namespace DevBlog.Api.Data;

public static class RagChunkSeeder
{
    public static void Seed(AppDbContext db, string ragDbPath)
    {
        if (db.RagChunks.Any()) return;

        if (!File.Exists(ragDbPath))
        {
            Console.WriteLine($"[RagChunkSeeder] '{ragDbPath}' bulunamadı, RAG chat içeriksiz başlayacak.");
            return;
        }

        using var connection = new SqliteConnection($"Data Source={ragDbPath};Mode=ReadOnly");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT d.filename AS document_filename,
                   d.title AS document_title,
                   c.heading,
                   c.chunk_index,
                   c.content,
                   c.embedding
            FROM chunks c
            JOIN documents d ON d.id = c.document_id
            WHERE c.embedding IS NOT NULL
            ORDER BY d.order_index, c.chunk_index
            """;

        using var reader = command.ExecuteReader();
        var imported = 0;

        while (reader.Read())
        {
            db.RagChunks.Add(new RagChunk
            {
                DocumentFilename = reader.GetString(0),
                DocumentTitle = reader.GetString(1),
                Heading = reader.GetString(2),
                ChunkIndex = reader.GetInt32(3),
                Content = reader.GetString(4),
                Embedding = (byte[])reader.GetValue(5),
                CreatedAt = DateTime.UtcNow
            });
            imported++;
        }

        db.SaveChanges();
        Console.WriteLine($"[RagChunkSeeder] {imported} chunk '{ragDbPath}' üzerinden import edildi.");
    }
}
```

- [ ] **Step 3: `appsettings.json`'a `Rag:DbPath` ekle**

`src/DevBlog.Api/appsettings.json`, `"Cors"` bloğunun altına ekle:

```json
  "Rag": {
    "DbPath": "../../rag/rag.db"
  }
```

- [ ] **Step 4: `Program.cs`'de seed çağrısını ekle**

`src/DevBlog.Api/Program.cs` içindeki migration/seed bloğunu şu şekilde değiştir (7 numaralı yorumun altındaki blok):

```csharp
// 7. Apply migrations and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DataSeeder.Seed(db);

    var ragDbPath = Path.GetFullPath(Path.Combine(
        app.Environment.ContentRootPath,
        builder.Configuration["Rag:DbPath"] ?? "../../rag/rag.db"));
    RagChunkSeeder.Seed(db, ragDbPath);
}
```

- [ ] **Step 5: Çalıştır ve doğrula**

Run: `dotnet run --project src/DevBlog.Api/DevBlog.Api.csproj` (birkaç saniye çalıştıktan sonra Ctrl+C ile durdurulabilir)

Konsol çıktısında şunu doğrula: `[RagChunkSeeder] 70 chunk '...rag.db' üzerinden import edildi.`

SQL ile doğrula (API kapalıyken):
```bash
sqlite3 src/DevBlog.Api/devblog.db "SELECT COUNT(*) FROM RagChunks;"
```
Expected: `70`

- [ ] **Step 6: Commit**

```bash
git add src/DevBlog.Api/DevBlog.Api.csproj src/DevBlog.Api/Data/RagChunkSeeder.cs src/DevBlog.Api/appsettings.json src/DevBlog.Api/Program.cs
git commit -m "feat: import rag.db chunks into AppDbContext on startup"
```

---

## Task 4: Dış API istemcileri (Voyage + Claude) ve secret yönetimi

**Files:**
- Create: `src/DevBlog.Api/Services/External/IVoyageEmbeddingClient.cs`
- Create: `src/DevBlog.Api/Services/External/VoyageEmbeddingClient.cs`
- Create: `src/DevBlog.Api/Services/External/IClaudeChatClient.cs`
- Create: `src/DevBlog.Api/Services/External/ClaudeChatClient.cs`
- Modify: `src/DevBlog.Api/appsettings.json` (`Voyage`, `Anthropic` bölümleri)
- Modify: `src/DevBlog.Api/Program.cs` (typed `HttpClient` DI kaydı)

**Interfaces:**
- Produces: `IVoyageEmbeddingClient.EmbedQueryAsync(string text, CancellationToken ct = default) -> Task<float[]>` ve `IClaudeChatClient.GetAnswerAsync(string systemPrompt, string userMessage, CancellationToken ct = default) -> Task<string>` — Task 5'teki `ChatService` bu iki imzayı birebir kullanır. İkisi de config eksikse `InvalidOperationException`, HTTP hatasında `HttpRequestException` fırlatır (Task 5 bu ikisini yakalar).

- [ ] **Step 1: `IVoyageEmbeddingClient.cs`**

```csharp
namespace DevBlog.Api.Services.External;

public interface IVoyageEmbeddingClient
{
    Task<float[]> EmbedQueryAsync(string text, CancellationToken ct = default);
}
```

- [ ] **Step 2: `VoyageEmbeddingClient.cs`**

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DevBlog.Api.Services.External;

public class VoyageEmbeddingClient(HttpClient httpClient, IConfiguration configuration) : IVoyageEmbeddingClient
{
    public async Task<float[]> EmbedQueryAsync(string text, CancellationToken ct = default)
    {
        var apiKey = configuration["Voyage:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Voyage:ApiKey is not configured.");
        }

        var model = configuration["Voyage:Model"] ?? "voyage-3.5";
        var dimension = configuration.GetValue<int?>("Voyage:EmbeddingDimension") ?? 1024;

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.voyageai.com/v1/embeddings")
        {
            Content = JsonContent.Create(new VoyageEmbeddingRequest(
                Input: [text],
                Model: model,
                InputType: "query",
                OutputDimension: dimension))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<VoyageEmbeddingResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Voyage API returned an empty response.");

        return payload.Data[0].Embedding;
    }
}

file record VoyageEmbeddingRequest(
    [property: JsonPropertyName("input")] string[] Input,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("input_type")] string InputType,
    [property: JsonPropertyName("output_dimension")] int OutputDimension);

file record VoyageEmbeddingResponse(
    [property: JsonPropertyName("data")] List<VoyageEmbeddingData> Data);

file record VoyageEmbeddingData(
    [property: JsonPropertyName("embedding")] float[] Embedding,
    [property: JsonPropertyName("index")] int Index);
```

- [ ] **Step 3: `IClaudeChatClient.cs`**

```csharp
namespace DevBlog.Api.Services.External;

public interface IClaudeChatClient
{
    Task<string> GetAnswerAsync(string systemPrompt, string userMessage, CancellationToken ct = default);
}
```

- [ ] **Step 4: `ClaudeChatClient.cs`**

```csharp
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DevBlog.Api.Services.External;

public class ClaudeChatClient(HttpClient httpClient, IConfiguration configuration) : IClaudeChatClient
{
    private const string ApiVersion = "2023-06-01";

    public async Task<string> GetAnswerAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
    {
        var apiKey = configuration["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Anthropic:ApiKey is not configured.");
        }

        var model = configuration["Anthropic:Model"] ?? "claude-sonnet-5";

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = JsonContent.Create(new ClaudeMessagesRequest(
                Model: model,
                MaxTokens: 1024,
                System: systemPrompt,
                Messages: [new ClaudeMessage("user", userMessage)]))
        };
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", ApiVersion);

        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ClaudeMessagesResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Claude API returned an empty response.");

        return string.Concat(payload.Content.Select(block => block.Text));
    }
}

file record ClaudeMessagesRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("max_tokens")] int MaxTokens,
    [property: JsonPropertyName("system")] string System,
    [property: JsonPropertyName("messages")] ClaudeMessage[] Messages);

file record ClaudeMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

file record ClaudeMessagesResponse(
    [property: JsonPropertyName("content")] List<ClaudeContentBlock> Content);

file record ClaudeContentBlock(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string Text);
```

- [ ] **Step 5: `appsettings.json`'a config bölümlerini ekle**

`Rag` bloğunun (Task 3) yanına ekle:

```json
  "Voyage": {
    "ApiKey": "",
    "Model": "voyage-3.5",
    "EmbeddingDimension": 1024
  },
  "Anthropic": {
    "ApiKey": "",
    "Model": "claude-sonnet-5"
  }
```

`ApiKey` alanları bilinçli olarak boş bırakılır — gerçek değerler bir sonraki adımda `user-secrets` ile sağlanır, asla commit edilmez.

- [ ] **Step 6: `Program.cs`'de typed `HttpClient` kaydı ekle**

`src/DevBlog.Api/Program.cs` başına ekle:
```csharp
using DevBlog.Api.Services.External;
```

`// 2. Repositories & Services` bloğunun altına (LikeService kaydından sonra) ekle:
```csharp
builder.Services.AddHttpClient<IVoyageEmbeddingClient, VoyageEmbeddingClient>();
builder.Services.AddHttpClient<IClaudeChatClient, ClaudeChatClient>();
```

- [ ] **Step 7: User secrets ile Voyage key'i ayarla**

Run:
```bash
dotnet user-secrets init --project src/DevBlog.Api
```
Ardından, `rag/.env` dosyasındaki `VOYAGE_API_KEY` değerini kullanarak (değeri bu plana veya commit'e yapıştırma):
```bash
dotnet user-secrets set "Voyage:ApiKey" "<rag/.env içindeki VOYAGE_API_KEY değeri>" --project src/DevBlog.Api
```
`Anthropic:ApiKey` şimdilik ayarlanmaz — kullanıcı key'i sağladığında aynı şekilde `dotnet user-secrets set "Anthropic:ApiKey" "<değer>" --project src/DevBlog.Api` ile eklenecek.

- [ ] **Step 8: Build ile doğrula**

Run: `dotnet build DevBlog.slnx`
Expected: Build succeeded.

- [ ] **Step 9: Commit**

```bash
git add src/DevBlog.Api/Services/External/ src/DevBlog.Api/appsettings.json src/DevBlog.Api/Program.cs
git commit -m "feat: add Voyage and Claude REST clients"
```
(`dotnet user-secrets` ile ayarlanan key commit edilmez — `%APPDATA%/Microsoft/UserSecrets` altında, repo dışında saklanır.)

---

## Task 5: `RagChunkRepository`, `EmbeddingVector`, `ChatService` ve `ChatEndpoint`

Bu task tek parça tutulur: `ChatService`, kullandığı `ChatResult`/`ChatErrorCode`/`ChatResponse` tiplerini tanımlayan `ChatEndpoint.cs` olmadan derlenmez — ikisi ayrı task'lara bölünürse ara adım build kırık kalır ve bağımsız olarak review edilemez.

**Files:**
- Create: `src/DevBlog.Api/Repositories/IRagChunkRepository.cs`
- Create: `src/DevBlog.Api/Repositories/RagChunkRepository.cs`
- Create: `src/DevBlog.Api/Services/EmbeddingVector.cs`
- Create: `src/DevBlog.Api/Services/IChatService.cs`
- Create: `src/DevBlog.Api/Services/ChatService.cs`
- Create: `src/DevBlog.Api/Endpoints/ChatEndpoint.cs`
- Modify: `src/DevBlog.Api/Program.cs` (DI kaydı + CSRF muafiyeti + endpoint mapping)

**Interfaces:**
- Consumes: `AppDbContext.RagChunks` (Task 2), `IVoyageEmbeddingClient`/`IClaudeChatClient` (Task 4).
- Produces: `POST /chat` — `ChatRequest { string Message }` → `ChatResponse { string Answer, IReadOnlyList<ChatSourceResponse> Sources }` (200), 400/503/502 hata gövdeleri. `EmbeddingVector.ToBytes(float[])`, `EmbeddingVector.FromBytes(byte[])`, `EmbeddingVector.CosineSimilarity(float[], float[])`.

- [ ] **Step 1: `IRagChunkRepository.cs`**

```csharp
using DevBlog.Api.Models;

namespace DevBlog.Api.Repositories;

public interface IRagChunkRepository
{
    Task<IReadOnlyList<RagChunk>> GetAllAsync();
}
```

- [ ] **Step 2: `RagChunkRepository.cs`**

```csharp
using DevBlog.Api.Data;
using DevBlog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DevBlog.Api.Repositories;

public class RagChunkRepository(AppDbContext db) : IRagChunkRepository
{
    public async Task<IReadOnlyList<RagChunk>> GetAllAsync() =>
        await db.RagChunks.AsNoTracking().ToListAsync();
}
```

- [ ] **Step 3: `EmbeddingVector.cs`**

```csharp
namespace DevBlog.Api.Services;

public static class EmbeddingVector
{
    public static byte[] ToBytes(float[] embedding)
    {
        var bytes = new byte[embedding.Length * sizeof(float)];
        Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public static float[] FromBytes(byte[] bytes)
    {
        var embedding = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, embedding, 0, bytes.Length);
        return embedding;
    }

    public static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0) return 0;
        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
```

- [ ] **Step 4: `IChatService.cs`**

```csharp
using DevBlog.Api.Endpoints;

namespace DevBlog.Api.Services;

public interface IChatService
{
    Task<ChatResult> AskAsync(string message);
}
```

- [ ] **Step 5: `ChatService.cs`**

```csharp
using DevBlog.Api.Endpoints;
using DevBlog.Api.Repositories;
using DevBlog.Api.Services.External;

namespace DevBlog.Api.Services;

public class ChatService(
    IRagChunkRepository ragChunkRepository,
    IVoyageEmbeddingClient embeddingClient,
    IClaudeChatClient chatClient) : IChatService
{
    private const int TopK = 5;

    private const string SystemPromptTemplate = """
        Sen ABC Telecom DevBlog'un Claude Code makaleleri üzerine soru
        cevaplayan bir asistansın. Sadece aşağıda verilen makale
        alıntılarını kullanarak cevap ver. Cevap alıntılarda yoksa
        "Bu soruyu makalelerdeki bilgilerle cevaplayamıyorum." de, uydurma.

        Alıntılar:
        {0}
        """;

    public async Task<ChatResult> AskAsync(string message)
    {
        var chunks = await ragChunkRepository.GetAllAsync();
        if (chunks.Count == 0)
        {
            return new ChatResult(false, null, ChatErrorCode.ServiceUnavailable,
                "İçerik indeksi hazır değil.");
        }

        float[] queryEmbedding;
        try
        {
            queryEmbedding = await embeddingClient.EmbedQueryAsync(message);
        }
        catch (InvalidOperationException ex)
        {
            return new ChatResult(false, null, ChatErrorCode.ServiceUnavailable, ex.Message);
        }
        catch (HttpRequestException)
        {
            return new ChatResult(false, null, ChatErrorCode.BadGateway,
                "Embedding servisine ulaşılamadı.");
        }

        var topChunks = chunks
            .Select(c => (
                Chunk: c,
                Score: EmbeddingVector.CosineSimilarity(queryEmbedding, EmbeddingVector.FromBytes(c.Embedding))))
            .OrderByDescending(x => x.Score)
            .Take(TopK)
            .ToList();

        var excerpts = string.Join("\n\n", topChunks.Select(x =>
            $"[{x.Chunk.DocumentTitle} > {x.Chunk.Heading}]\n{x.Chunk.Content}"));
        var systemPrompt = string.Format(SystemPromptTemplate, excerpts);

        string answer;
        try
        {
            answer = await chatClient.GetAnswerAsync(systemPrompt, message);
        }
        catch (InvalidOperationException ex)
        {
            return new ChatResult(false, null, ChatErrorCode.ServiceUnavailable, ex.Message);
        }
        catch (HttpRequestException)
        {
            return new ChatResult(false, null, ChatErrorCode.BadGateway,
                "Claude API'sine ulaşılamadı.");
        }

        var sources = topChunks
            .Select(x => new ChatSourceResponse(x.Chunk.DocumentTitle, x.Chunk.Heading, x.Chunk.DocumentFilename))
            .DistinctBy(s => (s.DocumentTitle, s.Heading))
            .ToList();

        return new ChatResult(true, new ChatResponse(answer, sources), null, null);
    }
}
```

- [ ] **Step 6: `Program.cs`'de DI kaydı ekle**

`// 2. Repositories & Services` bloğuna, Task 4'te eklenen `AddHttpClient` satırlarının üstüne ekle:
```csharp
builder.Services.AddScoped<IRagChunkRepository, RagChunkRepository>();
builder.Services.AddScoped<IChatService, ChatService>();
```

- [ ] **Step 7: `ChatEndpoint.cs` oluştur**

```csharp
using DevBlog.Api.Services;

namespace DevBlog.Api.Endpoints;

public static class ChatEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/chat", async (ChatRequest request, IChatService chatService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest(new { error = "Message boş olamaz." });
            }

            var result = await chatService.AskAsync(request.Message);

            if (result.Success)
            {
                return Results.Ok(result.Response);
            }

            return result.ErrorCode switch
            {
                ChatErrorCode.ServiceUnavailable => Results.Json(
                    new { error = result.ErrorMessage }, statusCode: StatusCodes.Status503ServiceUnavailable),
                ChatErrorCode.BadGateway => Results.Json(
                    new { error = result.ErrorMessage }, statusCode: StatusCodes.Status502BadGateway),
                _ => Results.Problem(result.ErrorMessage)
            };
        });
    }
}

public record ChatRequest(string Message);

public record ChatSourceResponse(string DocumentTitle, string Heading, string DocumentFilename);

public record ChatResponse(string Answer, IReadOnlyList<ChatSourceResponse> Sources);

public enum ChatErrorCode { ServiceUnavailable, BadGateway }

public record ChatResult(bool Success, ChatResponse? Response, ChatErrorCode? ErrorCode, string? ErrorMessage);
```

- [ ] **Step 8: `Program.cs`'de CSRF muafiyeti ekle**

`/auth/login` muafiyetinin bulunduğu satırı değiştir — chat herkese açık (anonim) olduğu için XSRF cookie'si olmayacak, bu yüzden CSRF kontrolünden de muaf tutulmalı:

```csharp
    var isExempt = request.Path.StartsWithSegments("/auth/login")
        || request.Path.StartsWithSegments("/chat");
```

- [ ] **Step 9: `Program.cs`'de endpoint'i map et**

`SearchEndpoint.Map(app);` satırının altına ekle:
```csharp
ChatEndpoint.Map(app);
```

- [ ] **Step 10: Build ile doğrula**

Run: `dotnet build DevBlog.slnx`
Expected: Build succeeded.

- [ ] **Step 11: Boş mesaj için 400 doğrula**

Run: `dotnet run --project src/DevBlog.Api/DevBlog.Api.csproj` (arka planda bırak), sonra başka bir terminalde:
```bash
curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5000/chat \
  -H "Content-Type: application/json" -d "{\"message\": \"\"}"
```
(Port farklıysa `dotnet run` çıktısındaki `Now listening on:` satırından al.)
Expected: `400`

- [ ] **Step 12: Anthropic key eksikken 503 doğrula**

```bash
curl -s -X POST http://localhost:5000/chat \
  -H "Content-Type: application/json" -d "{\"message\": \"Agentic loop nedir?\"}"
```
Expected: HTTP 503, gövde `{"error":"Anthropic:ApiKey is not configured."}`. Bu, Anthropic key'i henüz sağlanmadığı için **beklenen ve doğru** davranıştır — Voyage embedding adımı gerçek API ile çalışır (key zaten `user-secrets`'ta), Claude çağrısına gelindiğinde config eksikliği düzgün 503'e dönüşür.

Kullanıcı Anthropic key'ini sağladığında:
```bash
dotnet user-secrets set "Anthropic:ApiKey" "<değer>" --project src/DevBlog.Api
```
sonrasında aynı `curl` komutu 200 ve gerçek bir Claude cevabı dönmelidir — bu, plan tamamlandıktan sonra kullanıcı tarafından yapılacak ayrı bir doğrulama adımıdır.

- [ ] **Step 13: `dotnet run`'ı durdur, Commit**

```bash
git add src/DevBlog.Api/Repositories/IRagChunkRepository.cs src/DevBlog.Api/Repositories/RagChunkRepository.cs \
        src/DevBlog.Api/Services/EmbeddingVector.cs src/DevBlog.Api/Services/IChatService.cs \
        src/DevBlog.Api/Services/ChatService.cs src/DevBlog.Api/Endpoints/ChatEndpoint.cs \
        src/DevBlog.Api/Program.cs
git commit -m "feat: add POST /chat endpoint with RAG retrieval and Claude generation"
```

---

## Self-Review Notları

- **Spec kapsaması:** Design spec'teki her karar (LLM cevap, Claude, in-memory cosine, RagChunks/AppDbContext, public/no-auth, tek JSON response, tüm makaleler, tek turlu, test kapsam dışı, embed.py ön koşulu) yukarıdaki 5 task'a birebir haritalanmıştır.
- **Placeholder taraması:** Kod bloklarının tamamı çalışır durumda yazıldı; tek "sonradan doldurulacak" alan olan `Anthropic:ApiKey`, kullanıcının kendi kararıyla bilinçli olarak boş bırakıldı ve bunun endpoint davranışına etkisi (503) açıkça test edilip doğrulanıyor — kaçamak bir placeholder değil, tasarımın parçası.
- **Tip tutarlılığı:** `ChatService`/`ChatEndpoint` tek task'ta birlikte tanımlandığı için `ChatResult`/`ChatErrorCode`/`ChatResponse`/`ChatSourceResponse` referansları aynı task içinde tutarlı; `IVoyageEmbeddingClient.EmbedQueryAsync`/`IClaudeChatClient.GetAnswerAsync` imzaları Task 4'te tanımlanıp Task 5'te birebir çağrılıyor; `RagChunk` alan adları Task 2 → Task 3 (seeder) → Task 5 (repository/service) boyunca aynı kalıyor.
