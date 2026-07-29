using DevBlog.Api.Endpoints;
using DevBlog.Api.Repositories;
using DevBlog.Api.Services.External;
using Microsoft.Extensions.Logging;

namespace DevBlog.Api.Services;

public class ChatService(
    IRagChunkRepository ragChunkRepository,
    IVoyageEmbeddingClient embeddingClient,
    IClaudeChatClient chatClient,
    ILogger<ChatService> logger) : IChatService
{
    private const int TopK = 5;

    private const string SystemPromptTemplate = """
        Sen ABC Telecom DevBlog'un Claude Code makaleleri üzerine soru
        cevaplayan bir asistansın. Sadece aşağıda verilen makale
        alıntılarını kullanarak cevap ver. Cevap alıntılarda yoksa
        "Bu soruyu makalelerdeki bilgilerle cevaplayamıyorum." de, uydurma.

        Alıntılar:
        {0}
        """;

    public async Task<ChatResult> AskAsync(string message)
    {
        var chunks = await ragChunkRepository.GetAllAsync();
        if (chunks.Count == 0)
        {
            return new ChatResult(false, null, ChatErrorCode.ServiceUnavailable,
                "İçerik indeksi hazır değil.");
        }

        float[] queryEmbedding;
        try
        {
            queryEmbedding = await embeddingClient.EmbedQueryAsync(message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Embedding servisi yapılandırma hatası.");
            return new ChatResult(false, null, ChatErrorCode.ServiceUnavailable, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Embedding servisine ulaşılamadı.");
            return new ChatResult(false, null, ChatErrorCode.BadGateway,
                "Embedding servisine ulaşılamadı.");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "Embedding servisine zaman aşımı.");
            return new ChatResult(false, null, ChatErrorCode.BadGateway,
                "Embedding servisine zaman aşımı.");
        }

        var topChunks = chunks
            .Select(c => (
                Chunk: c,
                Score: EmbeddingVector.CosineSimilarity(queryEmbedding, EmbeddingVector.FromBytes(c.Embedding))))
            .OrderByDescending(x => x.Score)
            .Take(TopK)
            .ToList();

        var excerpts = string.Join("\n\n", topChunks.Select(x =>
            $"[{x.Chunk.DocumentTitle} > {x.Chunk.Heading}]\n{x.Chunk.Content}"));
        var systemPrompt = string.Format(SystemPromptTemplate, excerpts);

        string answer;
        try
        {
            answer = await chatClient.GetAnswerAsync(systemPrompt, message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Claude API yapılandırma hatası.");
            return new ChatResult(false, null, ChatErrorCode.ServiceUnavailable, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Claude API'sine ulaşılamadı.");
            return new ChatResult(false, null, ChatErrorCode.BadGateway,
                "Claude API'sine ulaşılamadı.");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "Claude API'sine zaman aşımı.");
            return new ChatResult(false, null, ChatErrorCode.BadGateway,
                "Claude API'sine zaman aşımı.");
        }

        var sources = topChunks
            .Select(x => new ChatSourceResponse(x.Chunk.DocumentTitle, x.Chunk.Heading, x.Chunk.DocumentFilename))
            .DistinctBy(s => (s.DocumentTitle, s.Heading))
            .ToList();

        return new ChatResult(true, new ChatResponse(answer, sources), null, null);
    }
}
