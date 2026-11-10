using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaChat.Models;
using AvaloniaChat.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaChat.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IChatService _chatService;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// 只給 IDE 設計器預覽用（MainWindow.axaml 的 Design.DataContext 需要無參數建構式）。
    /// 執行時由 App.axaml.cs 依啟動參數注入服務。
    /// </summary>
    public MainViewModel() : this(new FakeChatService())
    {
    }

    public MainViewModel(IChatService chatService)
    {
        _chatService = chatService;
    }

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    public partial string InputText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    public partial bool IsStreaming { get; set; }

    private bool CanSend() => !IsStreaming && !string.IsNullOrWhiteSpace(InputText);

    private bool CanCancel() => IsStreaming;

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        var text = InputText.Trim();

        Messages.Add(new ChatMessageViewModel(ChatRole.User, text));
        InputText = string.Empty;

        var assistant = new ChatMessageViewModel(ChatRole.Assistant, string.Empty);
        Messages.Add(assistant);

        using var cts = new CancellationTokenSource();
        _cts = cts;
        IsStreaming = true;

        TimeSpan? ttft = null;
        double? tokensPerSecond = null;
        string? doneReason = null;
        long? evalCount = null;
        var stopwatch = Stopwatch.StartNew(); // 請求在第一次 MoveNextAsync 時才送出，就是下一行

        try
        {
            // 沒有 ConfigureAwait(false)：每個片段都回到 UI 執行緒，才能直接改 assistant.Text。
            await foreach (var chunk in _chatService.StreamChatAsync(text, cts.Token))
            {
                if (ttft is null && !string.IsNullOrEmpty(chunk.Text))
                {
                    ttft = stopwatch.Elapsed;
                }

                assistant.Text += chunk.Text;

                if (chunk.IsDone)
                {
                    doneReason = chunk.DoneReason;
                    evalCount = chunk.EvalCount;
                }

                if (chunk.IsDone && chunk.EvalCount is { } count && chunk.EvalDurationNs is { } durationNs && durationNs > 0)
                {
                    tokensPerSecond = count / (durationNs / 1_000_000_000.0);
                }
            }
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            AppendNote(assistant, "[已取消]");
        }
        catch (HttpRequestException ex) when (ex.HttpRequestError == HttpRequestError.ConnectionError)
        {
            AppendNote(assistant, "[錯誤] 無法連線到 Ollama（http://localhost:11434），請確認 Ollama 已啟動。");
        }
        catch (HttpRequestException ex)
        {
            AppendNote(assistant, "[錯誤] 與 Ollama 通訊失敗：" + ex.Message);
        }
        catch (ChatServiceException ex)
        {
            AppendNote(assistant, "[錯誤] " + ex.Message);
        }
        catch (OperationCanceledException)
        {
            // 不是使用者取消的，通常是 HttpClient.Timeout 到期
            AppendNote(assistant, "[錯誤] 等待 Ollama 回應逾時。");
        }
        catch (Exception ex)
        {
            AppendNote(assistant, $"[錯誤] {ex.GetType().Name}：{ex.Message}");
        }
        finally
        {
            _cts = null;
            IsStreaming = false;
            assistant.Stats = FormatStats(ttft, tokensPerSecond, doneReason, evalCount);
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => _cts?.Cancel();

    private static void AppendNote(ChatMessageViewModel message, string note)
    {
        message.Text = string.IsNullOrEmpty(message.Text) ? note : message.Text + " " + note;
    }

    /// <summary>
    /// 兩行：效能（TTFT、速度）＋ 結束資訊（done_reason、eval_count）。
    /// 沒收到最後一行（取消、錯誤）時，對應欄位顯示 n/a。
    /// </summary>
    private static string FormatStats(TimeSpan? ttft, double? tokensPerSecond, string? doneReason, long? evalCount)
    {
        var ttftText = ttft is { } t
            ? t.TotalMilliseconds.ToString("N0", CultureInfo.InvariantCulture) + " ms"
            : "n/a";
        var tpsText = tokensPerSecond is { } tps
            ? tps.ToString("F1", CultureInfo.InvariantCulture) + " tokens/s"
            : "n/a";
        var reasonText = string.IsNullOrEmpty(doneReason) ? "n/a" : doneReason;
        var countText = evalCount is { } n ? n.ToString(CultureInfo.InvariantCulture) : "n/a";
        return $"TTFT {ttftText} · 速度 {tpsText}\n結束原因：{reasonText}，共 {countText} tokens";
    }
}
