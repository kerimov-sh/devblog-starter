using DevBlog.Api.Data;
using DevBlog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DevBlog.Api.Repositories;

public class LikeRepository(AppDbContext db) : ILikeRepository
{
    public Task<PostLike?> FindAsync(int userId, int postId) =>
        db.PostLikes.FirstOrDefaultAsync(l => l.UserId == userId && l.PostId == postId);

    public async Task AddAsync(PostLike like)
    {
        db.PostLikes.Add(like);
        await db.SaveChangesAsync();
    }

    public async Task RemoveAsync(PostLike like)
    {
        db.PostLikes.Remove(like);
        await db.SaveChangesAsync();
    }

    public Task<int> CountByPostIdAsync(int postId) =>
        db.PostLikes.CountAsync(l => l.PostId == postId);

    public async Task<Dictionary<int, int>> GetLikeCountsAsync(IEnumerable<int> postIds)
    {
        var ids = postIds.ToList();

        return await db.PostLikes
            .Where(l => ids.Contains(l.PostId))
            .GroupBy(l => l.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count);
    }

    public async Task<HashSet<int>> GetLikedPostIdsAsync(int userId, IEnumerable<int> postIds)
    {
        var ids = postIds.ToList();

        var liked = await db.PostLikes
            .Where(l => l.UserId == userId && ids.Contains(l.PostId))
            .Select(l => l.PostId)
            .ToListAsync();

        return liked.ToHashSet();
    }
}
