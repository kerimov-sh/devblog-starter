using DevBlog.Api.Models;

namespace DevBlog.Api.Repositories;

public interface ICommentRepository
{
    Task<(IReadOnlyList<Comment> Comments, int TotalCount)> GetAllAsync(int page, int pageSize);
}
