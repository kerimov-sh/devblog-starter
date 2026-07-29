using DevBlog.Api.Models;

namespace DevBlog.Api.Repositories;

public interface IRagChunkRepository
{
    Task<IReadOnlyList<RagChunk>> GetAllAsync();
}
