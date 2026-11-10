using System.Collections.Generic;
using System.Threading;

namespace AvaloniaChat.Services;

/// <summary>
/// 串流回覆的一個片段。
/// </summary>
/// <param name="Text">這次新增的文字（可能是空字串，例如 Ollama 的最後一行）。</param>
/// <param name="IsDone">是否為最後一個片段。</param>
/// <param name="EvalCount">最後一行才有：產生的 token 數（Ollama 的 eval_count）。</param>
/// <param name="EvalDurationNs">最後一行才有：產生 token 花的時間，單位奈秒（Ollama 的 eval_duration）。</param>
/// <param name="DoneReason">最後一行才有：結束原因（Ollama 的 done_reason，實測值 "stop" 或 "length"）。</param>
public sealed record ChatChunk(
    string Text,
    bool IsDone,
    long? EvalCount = null,
    long? EvalDurationNs = null,
    string? DoneReason = null);

public interface IChatService
{
    /// <summary>
    /// 送出一則使用者訊息（不含歷史），以串流方式回傳回覆片段。
    /// 取消時丟出 <see cref="System.OperationCanceledException"/>；
    /// 連線失敗時丟出 <see cref="System.Net.Http.HttpRequestException"/>；
    /// 服務端回報錯誤時丟出 <see cref="ChatServiceException"/>。
    /// </summary>
    IAsyncEnumerable<ChatChunk> StreamChatAsync(string userMessage, CancellationToken cancellationToken = default);
}
