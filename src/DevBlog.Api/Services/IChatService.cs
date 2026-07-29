using DevBlog.Api.Endpoints;

namespace DevBlog.Api.Services;

public interface IChatService
{
    Task<ChatResult> AskAsync(string message);
}
