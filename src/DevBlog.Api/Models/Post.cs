namespace DevBlog.Api.Models;

public class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Tags { get; set; } = "";
    public DateTime PublishedAt { get; set; }
    public int ReadingTimeMinutes { get; set; }
    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public ICollection<Comment> Comments { get; set; } = [];
}
