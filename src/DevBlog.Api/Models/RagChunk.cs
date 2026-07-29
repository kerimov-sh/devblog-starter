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
