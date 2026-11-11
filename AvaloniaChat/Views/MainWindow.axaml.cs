using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace AvaloniaChat.Views;

public partial class MainWindow : Window
{
    /// <summary>
    /// 離底部多少 DIP 以內都算「在底部」。
    /// 吸收排版的小數誤差與 DPI 縮放的捨入，也讓「幾乎到底」的使用者仍會跟著捲。
    /// </summary>
    private const double BottomTolerance = 20;

    /// <summary>
    /// 內容「變高之前」使用者是否在底部。
    /// 必須記住變高前的狀態：內容一變高，以新的 Extent 計算時，任何人都不在底部了。
    /// </summary>
    private bool _wasAtBottom = true;

    public MainWindow()
    {
        InitializeComponent();

        // 自動捲動是純 View 行為，放在 code-behind；ViewModel 不知道 ScrollViewer。
        // ScrollChanged 在版面配置之後觸發，此時 Extent/Offset/Viewport 都是最新值。
        MessagesScrollViewer.ScrollChanged += (_, e) =>
        {
            if (e.ExtentDelta.Y > 0 && _wasAtBottom)
            {
                // 內容變高，而且使用者原本在底部 → 跟著捲到底。
                // ScrollToEnd 會再觸發一次 ScrollChanged（Offset 改變），那時 _wasAtBottom 會被重算為 true。
                MessagesScrollViewer.ScrollToEnd();
                return;
            }

            // 其他情況（使用者捲動、視窗縮放、或內容變高但使用者不在底部）→ 依目前位置重算。
            _wasAtBottom = IsAtBottom(MessagesScrollViewer);
        };

        // 亮/暗切換：讓 ToggleButton 的初始狀態反映目前實際主題（App.axaml 預設跟隨系統），
        // 並在主題被其他來源改變時（例如系統切換）同步按鈕狀態。
        if (Application.Current is { } app)
        {
            SyncThemeToggle(app);
            app.ActualThemeVariantChanged += OnAppActualThemeVariantChanged;
            Closed += (_, _) => app.ActualThemeVariantChanged -= OnAppActualThemeVariantChanged;
        }
    }

    private void OnAppActualThemeVariantChanged(object? sender, EventArgs e)
    {
        if (sender is Application app)
        {
            SyncThemeToggle(app);
        }
    }

    private void SyncThemeToggle(Application app)
        => ThemeToggle.IsChecked = app.ActualThemeVariant == ThemeVariant.Dark;

    /// <summary>
    /// 按下 ToggleButton：IsChecked=true → 暗色，false → 亮色。
    /// 只在目標與目前實際主題不同時才設定，所以 SyncThemeToggle 造成的 IsChecked 變化不會把
    /// 「跟隨系統（Default）」意外改成固定值。
    /// </summary>
    private void OnThemeToggleChanged(object? sender, RoutedEventArgs e)
    {
        if (Application.Current is not { } app)
        {
            return;
        }

        var target = ThemeToggle.IsChecked == true ? ThemeVariant.Dark : ThemeVariant.Light;
        if (app.ActualThemeVariant != target)
        {
            app.RequestedThemeVariant = target;
        }
    }

    private static bool IsAtBottom(ScrollViewer sv)
        => sv.Offset.Y + sv.Viewport.Height >= sv.Extent.Height - BottomTolerance;
}
