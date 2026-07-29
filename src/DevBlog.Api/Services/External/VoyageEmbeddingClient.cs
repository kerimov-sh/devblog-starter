using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DevBlog.Api.Services.External;

public class VoyageEmbeddingClient(HttpClient httpClient, IConfiguration configuration) : IVoyageEmbeddingClient
{
    public async Task<float[]> EmbedQueryAsync(string text, CancellationToken ct = default)
    {
        var apiKey = configuration["Voyage:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Voyage:ApiKey is not configured.");
        }

        var model = configuration["Voyage:Model"] ?? "voyage-3.5";
        var dimension = configuration.GetValue<int?>("Voyage:EmbeddingDimension") ?? 1024;

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.voyageai.com/v1/embeddings")
        {
            Content = JsonContent.Create(new VoyageEmbeddingRequest(
                Input: [text],
                Model: model,
                InputType: "query",
                OutputDimension: dimension))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<VoyageEmbeddingResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Voyage API returned an empty response.");

        return payload.Data[0].Embedding;
    }
}

file record VoyageEmbeddingRequest(
    [property: JsonPropertyName("input")] string[] Input,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("input_type")] string InputType,
    [property: JsonPropertyName("output_dimension")] int OutputDimension);

file record VoyageEmbeddingResponse(
    [property: JsonPropertyName("data")] List<VoyageEmbeddingData> Data);

file record VoyageEmbeddingData(
    [property: JsonPropertyName("embedding")] float[] Embedding,
    [property: JsonPropertyName("index")] int Index);
