using DevBlog.Api.Endpoints;
using DevBlog.Api.Models;
using DevBlog.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DevBlog.Api.Services;

public class PostService(IPostRepository postRepository, ILikeRepository likeRepository) : IPostService
{
    public async Task<PagedPostsResponse> GetPostsAsync(int page, int pageSize, string? tag, int? currentUserId)
    {
        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (posts, totalCount) = await postRepository.GetPagedAsync(page, pageSize, tag);

        var postIds = posts.Select(p => p.Id).ToList();
        var likeCounts = await likeRepository.GetLikeCountsAsync(postIds);
        HashSet<int> likedPostIds = currentUserId is null
            ? []
            : await likeRepository.GetLikedPostIdsAsync(currentUserId.Value, postIds);

        var items = posts.Select(p => new PostSummaryResponse(
            p.Id, p.Title, p.Slug, p.Tags, p.PublishedAt, p.Author.Username,
            likeCounts.GetValueOrDefault(p.Id), likedPostIds.Contains(p.Id)
        )).ToList();

        return new PagedPostsResponse(
            items, page, pageSize, totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<CreatePostResult> CreatePostAsync(CreatePostRequest req, int authorId)
    {
        if (await postRepository.SlugExistsAsync(req.Slug))
        {
            return new CreatePostResult(false, null, $"Slug '{req.Slug}' already exists.");
        }

        var post = new Post
        {
            Title = req.Title,
            Content = req.Content,
            Slug = req.Slug,
            Tags = req.Tags,
            PublishedAt = DateTime.UtcNow,
            AuthorId = authorId
        };

        try
        {
            await postRepository.AddAsync(post);
        }
        catch (DbUpdateException)
        {
            return new CreatePostResult(false, null, $"Slug '{req.Slug}' already exists.");
        }

        return new CreatePostResult(true, new CreatePostResponse(post.Id, post.Slug), null);
    }
}