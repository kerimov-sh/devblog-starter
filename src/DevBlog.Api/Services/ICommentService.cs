using DevBlog.Api.Endpoints;

namespace DevBlog.Api.Services;

public interface ICommentService
{
    Task<PagedCommentsResponse> GetAllCommentsAsync(int page, int pageSize);
}
