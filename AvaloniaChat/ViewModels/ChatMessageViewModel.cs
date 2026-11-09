using AvaloniaChat.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaChat.ViewModels;

/// <summary>
/// 聊天清單中的一則訊息。
/// </summary>
public partial class ChatMessageViewModel : ViewModelBase
{
    public ChatMessageViewModel(ChatRole role, string text)
    {
        Role = role;
        Text = text;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUser))]
    public partial ChatRole Role { get; set; }

    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    /// <summary>
    /// 給 View 用來切換 Classes.user / Classes.assistant。
    /// Role 改變時會一併通知（見上方 NotifyPropertyChangedFor）。
    /// </summary>
    public bool IsUser => Role == ChatRole.User;
}
