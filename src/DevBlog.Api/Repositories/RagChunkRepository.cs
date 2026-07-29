using DevBlog.Api.Data;
using DevBlog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DevBlog.Api.Repositories;

public class RagChunkRepository(AppDbContext db) : IRagChunkRepository
{
    public async Task<IReadOnlyList<RagChunk>> GetAllAsync() =>
        await db.RagChunks.AsNoTracking().ToListAsync();
}
