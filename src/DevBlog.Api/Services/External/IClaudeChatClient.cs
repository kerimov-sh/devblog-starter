namespace DevBlog.Api.Services.External;

public interface IClaudeChatClient
{
    Task<string> GetAnswerAsync(string systemPrompt, string userMessage, CancellationToken ct = default);
}
