# devblog-data MCP Server

DevBlog Starter projesi için ayrı bir Model Context Protocol (MCP) sunucusu. C# MCP SDK ile geliştirilmiştir, `stdio` transport üzerinden çalışır.

`Data/DatabaseConfig.cs` içinde `DevBlog.Api`'nin kullandığı SQLite dosyasının yolu bir sabit (`SqliteDbPath`) olarak tanımlıdır.

## Tool'lar

- **`get_posts`** — `limit` parametresi kadar (varsayılan 10) yazıyı, yayınlanma tarihine göre en yeniden en eskiye sıralayarak listeler (`id`, `title`, `slug`, `publishedat`).
- **`get_post_by_slug`** — Verilen `slug` değerine sahip yazının tüm alanlarını getirir. Slug bulunamazsa exception fırlatmaz; `tools/call` sonucunu `isError: true` ve açıklayıcı bir metin içerikle döndürür, JSON-RPC çağrısının kendisi protokol seviyesinde başarılı sayılır.

## Kaynaklar (Resources)

- **`devblog://schema`** — SQLite veritabanındaki her tabloyu ve kolonlarını düz metin (`text/plain`) olarak listeler ([Resources/SchemaResource.cs](Resources/SchemaResource.cs)). Veritabanına sorgu göndermez; şema elle yazılmış sabit bir metindir ve kaynak şeması değiştiğinde elle güncellenmelidir.

## Geliştirme

```bash
cd mcp-devblog-data
dotnet run
```

Süreç başlar ve stdin üzerinden JSON-RPC mesajı bekler.

## IDE'de yapılandırma (yerel geliştirme)

```json
{
  "servers": {
    "devblog-data": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "<PROJE DİZİNİNİN YOLU>"
      ]
    }
  }
}
```

## Daha fazla bilgi

- [MCP Resmi Dokümantasyon](https://modelcontextprotocol.io/)
- [MCP C# SDK](https://modelcontextprotocol.github.io/csharp-sdk)
