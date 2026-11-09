using Avalonia.Controls;

namespace AvaloniaChat.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 自動捲到底：純 View 的行為，放在 code-behind，ViewModel 不需要知道 ScrollViewer。
        // ScrollChanged 在版面配置完成後才觸發，此時 Extent 已包含新訊息的高度；
        // ExtentDelta.Y > 0 代表內容變高了（新增訊息），這時才捲到底。
        MessagesScrollViewer.ScrollChanged += (_, e) =>
        {
            if (e.ExtentDelta.Y > 0)
            {
                MessagesScrollViewer.ScrollToEnd();
            }
        };
    }
}
