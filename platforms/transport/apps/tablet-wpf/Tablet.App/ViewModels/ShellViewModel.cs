using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tablet.App.Settings;
using Tablet.Core;

namespace Tablet.App.ViewModels;

public sealed partial class PageViewModel(string key, string title, string icon) : ObservableObject
{
    public string Key { get; } = key;
    public string Title { get; } = title;
    public string Icon { get; } = icon;
    [ObservableProperty] private bool _isSelected;
}

public sealed record DestinationPreview(string Name, string Description);
public sealed record UiLogEntry(string Time, string Category, string Message);

/// <summary>Approved v10 presentation. Preview selections never dispatch robot motion.</summary>
public sealed partial class ShellViewModel : ObservableObject
{
    private readonly IRobotClient _robot;

    public ShellViewModel(IRobotClient robot)
    {
        _robot = robot;
        _linkState = robot.LinkState;
        Settings = new SettingsViewModel(
            ConnectionSettingsStore.ForCurrentUser(), IsMock, robot as IRobotConnectionDiagnostics);
        _robot.LinkStateChanged += (_, state) =>
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is not null && !dispatcher.HasShutdownStarted)
                dispatcher.InvokeAsync(() => LinkState = state);
        };
        Pages = new()
        {
            new("operate", "운행", "⌁"), new("map", "맵 편집", "▦"),
            new("log", "알람·이력", "◷"), new("settings", "설정", "⚙"),
        };
        Destinations = new()
        {
            new("조립 라인 A", "예시 목적지 · 작업 대기 구역"),
            new("조립 라인 B", "예시 목적지 · 자재 공급 구역"),
            new("충전 스테이션", "예시 목적지 · 충전 구역"),
        };
        Navigate(Pages[0]);
        AddLog("앱", "UI v10 시작 · 지도와 목적지는 배치 확인용 예시입니다.");
    }

    public ObservableCollection<PageViewModel> Pages { get; }
    public ObservableCollection<DestinationPreview> Destinations { get; }
    public ObservableCollection<UiLogEntry> Logs { get; } = new();
    public SettingsViewModel Settings { get; }
    public string RobotName => _robot.RobotName;
    public bool IsMock => _robot.Kind == RobotConnectionKind.Mock;
    public string ModeText => _robot.Kind switch
    {
        RobotConnectionKind.Mock => "Mock · 화면 시험",
        RobotConnectionKind.MobedDirect => "MobED 직접 연결 · 상태 조회",
        RobotConnectionKind.Agent => "Agent 경유",
        _ => "연결 방식 미확인",
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LinkText))]
    [NotifyPropertyChangedFor(nameof(LinkBrush))]
    private RobotLinkState _linkState;
    public string LinkText => LinkState switch
    {
        RobotLinkState.Connected => IsMock ? "Mock 연결" : "연결",
        RobotLinkState.Connecting => "연결 중",
        RobotLinkState.Faulted => "연결 오류",
        _ => "연결 끊김",
    };
    public Brush LinkBrush => LinkState switch
    {
        RobotLinkState.Connected => Brushes.ForestGreen,
        RobotLinkState.Connecting => Brushes.DarkOrange,
        RobotLinkState.Faulted => Brushes.IndianRed,
        _ => Brushes.SlateGray,
    };

    [ObservableProperty] private PageViewModel? _currentPage;
    [ObservableProperty] private string _statusMessage = "화면 구성 시험 · 지도·목적지는 예시이며 실제 주행·상태 수신은 아직 연결되지 않았습니다.";
    [ObservableProperty] private DestinationPreview? _selectedDestination;
    [ObservableProperty] private int _operateTab;
    [ObservableProperty] private int _driveModeIndex;
    [ObservableProperty] private int _speed = 1;

    public int[] SpeedOptions => DriveModeIndex == 0 ? [1, 2, 3] : [1, 2, 3, 4, 5];
    partial void OnDriveModeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(SpeedOptions));
        Speed = Math.Clamp(Speed, 1, value == 0 ? 3 : 5);
    }

    [RelayCommand]
    private void Navigate(PageViewModel page)
    {
        foreach (var item in Pages) item.IsSelected = ReferenceEquals(item, page);
        CurrentPage = page;
    }

    [RelayCommand] private void OpenSettings() => Navigate(Pages.Single(p => p.Key == "settings"));

    private bool CanRequestStop() =>
        _robot.Kind != RobotConnectionKind.MobedDirect || _robot.Navigation is not null;

    [RelayCommand(CanExecute = nameof(CanRequestStop))]
    private async Task StopAllAsync()
    {
        try
        {
            await _robot.RequestStopAsync();
            StatusMessage = IsMock
                ? "Mock 전체 정지 요청을 전달했습니다. 실제 로봇 정지 확인 기능은 아직 연결되지 않았습니다."
                : "전체 정지 요청을 전달했습니다. 로봇의 실제 정지 여부는 확인이 필요합니다.";
            AddLog("정지 요청", StatusMessage);
        }
        catch (Exception)
        {
            StatusMessage = "전체 정지 요청 전달에 실패했습니다. 로봇 상태와 연결을 직접 확인해 주세요.";
            AddLog("오류", StatusMessage);
        }
    }

    private void AddLog(string category, string message) =>
        Logs.Insert(0, new(DateTime.Now.ToString("HH:mm:ss"), category, message));

    public async Task StartAsync()
    {
        try
        {
            await _robot.ConnectAsync();
            AddLog("연결", IsMock ? "Mock 연결 완료 · 실기 통신 없음" : "로봇 연결 완료");
        }
        catch (Exception)
        {
            LinkState = RobotLinkState.Faulted;
            StatusMessage = "로봇 연결에 실패했습니다. 접속 정보와 인증서를 확인해 주세요.";
            AddLog("오류", StatusMessage);
        }
    }
}
