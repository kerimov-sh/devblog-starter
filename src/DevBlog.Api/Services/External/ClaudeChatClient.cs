using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DevBlog.Api.Services.External;

public class ClaudeChatClient(HttpClient httpClient, IConfiguration configuration) : IClaudeChatClient
{
    private const string ApiVersion = "2023-06-01";

    public async Task<string> GetAnswerAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
    {
        var apiKey = configuration["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Anthropic:ApiKey is not configured.");
        }

        var model = configuration["Anthropic:Model"] ?? "claude-sonnet-5";

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = JsonContent.Create(new ClaudeMessagesRequest(
                Model: model,
                MaxTokens: 1024,
                System: systemPrompt,
                Messages: [new ClaudeMessage("user", userMessage)]))
        };
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", ApiVersion);

        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ClaudeMessagesResponse>(cancellationToken: ct)
            ?? throw new HttpRequestException("Claude API returned an empty response.");

        return string.Concat(payload.Content.Select(block => block.Text));
    }
}

file record ClaudeMessagesRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("max_tokens")] int MaxTokens,
    [property: JsonPropertyName("system")] string System,
    [property: JsonPropertyName("messages")] ClaudeMessage[] Messages);

file record ClaudeMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

file record ClaudeMessagesResponse(
    [property: JsonPropertyName("content")] List<ClaudeContentBlock> Content);

file record ClaudeContentBlock(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string Text);
