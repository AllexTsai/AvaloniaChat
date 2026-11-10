using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaChat.Services;

/// <summary>
/// 不連網路的假服務：等 500ms 後，把「這是假的回覆：」加上使用者文字，一個字一個字串流回來。
/// 最後一個片段附上模擬的 eval_count / eval_duration，讓畫面上的 tokens/sec 也有數字可看。
/// </summary>
public sealed class FakeChatService : IChatService
{
    private static readonly int CharDelayMs = 40;

    public async IAsyncEnumerable<ChatChunk> StreamChatAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(500, cancellationToken).ConfigureAwait(false);

        var reply = "這是假的回覆：" + userMessage;
        var sw = Stopwatch.StartNew();
        foreach (var ch in reply)
        {
            yield return new ChatChunk(ch.ToString(), IsDone: false);
            await Task.Delay(CharDelayMs, cancellationToken).ConfigureAwait(false);
        }

        // 把每個字當成一個 token；Stopwatch 的時間換成奈秒，跟 Ollama 的單位一致。
        var evalDurationNs = (long)(sw.Elapsed.TotalMilliseconds * 1_000_000);
        // 假服務一定完整說完，結束原因固定是 "stop"。
        yield return new ChatChunk(string.Empty, IsDone: true, EvalCount: reply.Length, EvalDurationNs: evalDurationNs, DoneReason: "stop");
    }
}
