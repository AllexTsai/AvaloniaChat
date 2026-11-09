using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AvaloniaChat.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaChat.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public ObservableCollection<ChatMessageViewModel> Messages { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    public partial string InputText { get; set; } = string.Empty;

    private bool CanSend() => !string.IsNullOrWhiteSpace(InputText);

    // 產生的屬性名稱是 SendCommand（工具包會去掉 Async 字尾），型別為 IAsyncRelayCommand。
    // AsyncRelayCommand 預設不允許重疊執行：執行中 CanExecute 會是 false。
    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        var text = InputText.Trim();

        Messages.Add(new ChatMessageViewModel(ChatRole.User, text));
        InputText = string.Empty;

        // 非阻塞等待；await 之後會回到 UI 執行緒（沒有 ConfigureAwait(false)），
        // 所以可以直接修改 Messages。
        await Task.Delay(500);

        Messages.Add(new ChatMessageViewModel(ChatRole.Assistant, "這是假的回覆：" + text));
    }
}
