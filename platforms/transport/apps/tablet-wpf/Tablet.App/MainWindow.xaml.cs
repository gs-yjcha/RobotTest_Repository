using System.Windows;
using System.Windows.Input;

namespace Tablet.App;

public partial class MainWindow : Window
{
    public MainWindow() : this(kiosk: false) { }

    public MainWindow(bool kiosk)
    {
        InitializeComponent();
        if (kiosk)
        {
            // 태블릿 전체화면 (작업 표시줄 가림)
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
        }
    }

    // 개발 편의: F11로 전체화면 전환. 현장 키오스크 잠금은 이후 단계에서 정한다.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            var full = WindowStyle != WindowStyle.None;
            WindowState = WindowState.Normal;
            WindowStyle = full ? WindowStyle.None : WindowStyle.SingleBorderWindow;
            ResizeMode = full ? ResizeMode.NoResize : ResizeMode.CanResize;
            WindowState = full ? WindowState.Maximized : WindowState.Normal;
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }
}
