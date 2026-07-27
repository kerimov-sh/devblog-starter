using DevBlog.Api.Models;

namespace DevBlog.Api.Repositories;

public interface IPostRepository
{
    Task<(IReadOnlyList<Post> Posts, int TotalCount)> GetPagedAsync(int page, int pageSize, string? tag);
    Task<bool> SlugExistsAsync(string slug);
    Task AddAsync(Post post);
    Task<int?> GetIdBySlugAsync(string slug);
}