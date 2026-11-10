using System;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaloniaChat.Services;
using AvaloniaChat.ViewModels;
using AvaloniaChat.Views;

namespace AvaloniaChat;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(CreateChatService(desktop.Args ?? [])),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 啟動參數 --fake 用假服務；預設連本機 Ollama。--max-tokens 見 <see cref="AppOptions"/>。
    /// desktop.Args 就是 Program.Main 傳給 StartWithClassicDesktopLifetime 的 args，
    /// Program.Main 已經驗證過，這裡解析一定成功。
    /// </summary>
    private static IChatService CreateChatService(string[] args)
    {
        AppOptions.TryParse(args, out var options, out _);

        if (options.UseFake)
        {
            return new FakeChatService();
        }

        // 整個應用程式共用一個 HttpClient（不要每次請求都 new）。
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:11434/") };
        return new OllamaChatService(httpClient, model: "phi3", numCtx: 2048, numPredict: options.MaxTokens);
    }
}
