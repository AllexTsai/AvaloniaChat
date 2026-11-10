using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace AvaloniaChat.Services;

/// <summary>
/// 呼叫本機 Ollama 的 POST /api/chat（stream: true）。
/// 回應格式依 2026-11-09 以 curl 實測（每行一個 JSON 物件，NDJSON）：
///   {"model":"phi3","created_at":"...","message":{"role":"assistant","content":"It"},"done":false}
///   ...
///   {"model":"phi3",...,"message":{"role":"assistant","content":""},"done":true,"done_reason":"stop",
///    ...,"eval_count":91,"eval_duration":5550319000}
/// 錯誤（例如模型不存在）：HTTP 404，內文 {"error":"model 'no-such-model' not found"}。
/// 2026-11-10 實測 options.num_predict=16：最後一行 "done_reason":"length"、"eval_count":16。
/// </summary>
public sealed class OllamaChatService : IChatService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly int _numCtx;
    private readonly int _numPredict;

    /// <param name="httpClient">BaseAddress 應設為 Ollama 位址，例如 http://localhost:11434/</param>
    /// <param name="numPredict">回覆最多產生的 token 數（Ollama 的 num_predict）。</param>
    public OllamaChatService(HttpClient httpClient, string model = "phi3", int numCtx = 2048, int numPredict = 512)
    {
        _httpClient = httpClient;
        _model = model;
        _numCtx = numCtx;
        _numPredict = numPredict;
    }

    public async IAsyncEnumerable<ChatChunk> StreamChatAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var body = new ChatRequest(
            Model: _model,
            Messages: [new ChatRequestMessage("user", userMessage)],
            Stream: true,
            Options: new ChatRequestOptions(_numCtx, _numPredict));

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/chat")
        {
            Content = JsonContent.Create(body),
        };

        // ResponseHeadersRead：收到標頭就回來，不等整個內文下載完，才能邊收邊顯示。
        using var response = await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new ChatServiceException(
                $"Ollama 回應 HTTP {(int)response.StatusCode}：{TryGetError(errorBody) ?? errorBody}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var chatLine = JsonSerializer.Deserialize<ChatResponseLine>(line)
                           ?? throw new ChatServiceException("無法解析 Ollama 回應：" + line);

            // 串流途中也可能收到 {"error":"..."}
            if (chatLine.Error is not null)
            {
                throw new ChatServiceException("Ollama 錯誤：" + chatLine.Error);
            }

            var text = chatLine.Message?.Content ?? string.Empty;

            if (chatLine.Done)
            {
                yield return new ChatChunk(text, IsDone: true, chatLine.EvalCount, chatLine.EvalDuration, chatLine.DoneReason);
                yield break;
            }

            yield return new ChatChunk(text, IsDone: false);
        }

        // 連線在 done:true 之前就結束了
        throw new ChatServiceException("Ollama 串流在完成前中斷。");
    }

    private static string? TryGetError(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ChatResponseLine>(json)?.Error;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // ---- 請求 ----
    private sealed record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] ChatRequestMessage[] Messages,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("options")] ChatRequestOptions Options);

    private sealed record ChatRequestMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatRequestOptions(
        [property: JsonPropertyName("num_ctx")] int NumCtx,
        [property: JsonPropertyName("num_predict")] int NumPredict);

    // ---- 回應（每一行），只取需要的欄位 ----
    private sealed class ChatResponseLine
    {
        [JsonPropertyName("message")] public ChatResponseMessage? Message { get; set; }
        [JsonPropertyName("done")] public bool Done { get; set; }
        [JsonPropertyName("done_reason")] public string? DoneReason { get; set; }
        [JsonPropertyName("eval_count")] public long? EvalCount { get; set; }
        [JsonPropertyName("eval_duration")] public long? EvalDuration { get; set; }
        [JsonPropertyName("error")] public string? Error { get; set; }
    }

    private sealed class ChatResponseMessage
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }
}
