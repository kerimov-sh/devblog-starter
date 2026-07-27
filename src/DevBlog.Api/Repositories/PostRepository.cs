using DevBlog.Api.Data;
using DevBlog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DevBlog.Api.Repositories;

public class PostRepository(AppDbContext db) : IPostRepository
{
    public async Task<(IReadOnlyList<Post> Posts, int TotalCount)> GetPagedAsync(int page, int pageSize, string? tag)
    {
        var query = db.Posts.AsNoTracking().Include(p => p.Author).AsQueryable();

        if (!string.IsNullOrWhiteSpace(tag))
        {
            query = query.Where(p =>
                p.Tags == tag ||
                p.Tags.StartsWith(tag + ",") ||
                p.Tags.EndsWith("," + tag) ||
                p.Tags.Contains("," + tag + ","));
        }

        var totalCount = await query.CountAsync();

        var posts = await query
            .OrderByDescending(p => p.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (posts, totalCount);
    }

    public Task<bool> SlugExistsAsync(string slug) =>
        db.Posts.AnyAsync(p => p.Slug == slug);

    public async Task AddAsync(Post post)
    {
        db.Posts.Add(post);
        await db.SaveChangesAsync();
    }

    public Task<int?> GetIdBySlugAsync(string slug) =>
        db.Posts.Where(p => p.Slug == slug).Select(p => (int?)p.Id).FirstOrDefaultAsync();
}