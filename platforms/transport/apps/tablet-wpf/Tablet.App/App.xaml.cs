using System.Windows;
using Tablet.App.ViewModels;
using Tablet.Core;
using Tablet.Mock;

namespace Tablet.App;

/// <summary>
/// 시작 인자
///   --windowed : 개발용 창 모드 (기본은 전체화면 키오스크)
///   --mock     : local/Mobed.Client 가 있어도 Mock 로봇으로 실행
/// </summary>
public partial class App : Application
{
    private IRobotClient? _robot;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var args = e.Args.Select(a => a.ToLowerInvariant()).ToHashSet();

        _robot = CreateRobotClient(forceMock: args.Contains("--mock"));
        var shell = new ShellViewModel(_robot);
        var window = new MainWindow(kiosk: !args.Contains("--windowed")) { DataContext = shell };
        MainWindow = window;
        window.Show();

        if (shell.IsMock)
            await shell.StartAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_robot is not null) await _robot.DisposeAsync();
        base.OnExit(e);
    }

    private static IRobotClient CreateRobotClient(bool forceMock)
    {
#if MOBED_DIRECT
        if (!forceMock) return Mobed.Client.MobedClientFactory.Create();
#endif
        return new MockRobotClient();
    }
}
