using Avalonia;
using System;

namespace AvaloniaChat;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        // 參數錯誤就在建立任何視窗之前結束（純字串處理，不碰 Avalonia）。
        if (!AppOptions.TryParse(args, out _, out var error))
        {
            Console.Error.WriteLine("錯誤：" + error);
            return 2;
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
