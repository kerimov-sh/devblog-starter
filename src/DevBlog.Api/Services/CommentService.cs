using DevBlog.Api.Endpoints;
using DevBlog.Api.Repositories;

namespace DevBlog.Api.Services;

public class CommentService(ICommentRepository commentRepository) : ICommentService
{
    public async Task<PagedCommentsResponse> GetAllCommentsAsync(int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (comments, totalCount) = await commentRepository.GetAllAsync(page, pageSize);

        var items = comments.Select(c => new CommentResponse(
            c.Id,
            c.AuthorName,
            c.Body,
            c.CreatedAt,
            c.PostId,
            c.Post.Slug,
            c.Post.Title
        )).ToList();

        return new PagedCommentsResponse(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize)
        );
    }
}
