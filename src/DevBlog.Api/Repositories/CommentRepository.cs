using DevBlog.Api.Data;
using DevBlog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DevBlog.Api.Repositories;

public class CommentRepository(AppDbContext db) : ICommentRepository
{
    public async Task<(IReadOnlyList<Comment> Comments, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        var query = db.Comments.AsNoTracking().Include(c => c.Post);

        var totalCount = await query.CountAsync();

        var comments = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (comments, totalCount);
    }

    public async Task<Dictionary<int, int>> GetCommentCountsAsync(IEnumerable<int> postIds)
    {
        var ids = postIds.ToList();

        return await db.Comments
            .Where(c => ids.Contains(c.PostId))
            .GroupBy(c => c.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count);
    }
}
