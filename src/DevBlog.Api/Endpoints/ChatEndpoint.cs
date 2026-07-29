using DevBlog.Api.Services;

namespace DevBlog.Api.Endpoints;

public static class ChatEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/chat", async (ChatRequest request, IChatService chatService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest(new { error = "Message boş olamaz." });
            }

            if (request.Message.Length > 2000)
            {
                return Results.BadRequest(new { error = "Message 2000 karakterden uzun olamaz." });
            }

            var result = await chatService.AskAsync(request.Message);

            if (result.Success)
            {
                return Results.Ok(result.Response);
            }

            return result.ErrorCode switch
            {
                ChatErrorCode.ServiceUnavailable => Results.Json(
                    new { error = result.ErrorMessage }, statusCode: StatusCodes.Status503ServiceUnavailable),
                ChatErrorCode.BadGateway => Results.Json(
                    new { error = result.ErrorMessage }, statusCode: StatusCodes.Status502BadGateway),
                _ => Results.Problem(result.ErrorMessage)
            };
        }).RequireRateLimiting("chat");
    }
}

public record ChatRequest(string Message);

public record ChatSourceResponse(string DocumentTitle, string Heading, string DocumentFilename);

public record ChatResponse(string Answer, IReadOnlyList<ChatSourceResponse> Sources);

public enum ChatErrorCode { ServiceUnavailable, BadGateway }

public record ChatResult(bool Success, ChatResponse? Response, ChatErrorCode? ErrorCode, string? ErrorMessage);
