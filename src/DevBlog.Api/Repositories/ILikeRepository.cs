using DevBlog.Api.Models;

namespace DevBlog.Api.Repositories;

public interface ILikeRepository
{
    Task<PostLike?> FindAsync(int userId, int postId);
    Task AddAsync(PostLike like);
    Task RemoveAsync(PostLike like);
    Task<int> CountByPostIdAsync(int postId);
    Task<Dictionary<int, int>> GetLikeCountsAsync(IEnumerable<int> postIds);
    Task<HashSet<int>> GetLikedPostIdsAsync(int userId, IEnumerable<int> postIds);
}
