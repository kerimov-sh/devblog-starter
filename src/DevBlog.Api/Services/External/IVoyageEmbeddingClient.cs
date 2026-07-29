namespace DevBlog.Api.Services.External;

public interface IVoyageEmbeddingClient
{
    Task<float[]> EmbedQueryAsync(string text, CancellationToken ct = default);
}
