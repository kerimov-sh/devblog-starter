using DevBlog.Api.Endpoints;

namespace DevBlog.Api.Services;

public interface IPostService
{
    Task<PagedPostsResponse> GetPostsAsync(int page, int pageSize, string? tag, int? currentUserId);
    Task<CreatePostResult> CreatePostAsync(CreatePostRequest req, int authorId);
}