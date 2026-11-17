using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tablet.Core;

namespace Tablet.App.ViewModels;

/// <summary>왼쪽 메뉴 한 칸. 화면 내용은 단계별로 채운다.</summary>
public sealed partial class PageViewModel : ObservableObject
{
    public PageViewModel(string key, string title, string icon, string plannedStep, string summary)
    {
        Key = key;
        Title = title;
        Icon = icon;
        PlannedStep = plannedStep;
        Summary = summary;
    }

    public string Key { get; }
    public string Title { get; }
    public string Icon { get; }
    public string PlannedStep { get; }
    public string Summary { get; }

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>상단 상태바·왼쪽 메뉴·전체 정지를 가진 앱 껍데기.</summary>
public sealed partial class ShellViewModel : ObservableObject
{
    private readonly IRobotClient _robot;

    public ShellViewModel(IRobotClient robot)
    {
        _robot = robot;
        _robot.LinkStateChanged += (_, state) =>
            Application.Current?.Dispatcher.Invoke(() => LinkState = state);

        // UI 시안 v9 기준. Remote I/O는 1차 범위 제외(기획안 v3)라 메뉴에서 뺀다.
        Pages = new ObservableCollection<PageViewModel>
        {
            new("operate", "운행", "▲", "T5·T6", "작업 지시 · 위치 보정 · 수동 주행 탭"),
            new("map", "맵 편집", "▦", "T7", "목적지 · 영역 · 맵 이미지"),
            new("log", "알람·이력", "◷", "T5", "현재 알람 · 작업·설정 변경 이력"),
            new("settings", "설정", "⚙", "T3", "로봇 연결(접속 주소 · 인증서 폴더 · API Key) · 연결 시험"),
        };
        Navigate(Pages[0]);
    }

    public ObservableCollection<PageViewModel> Pages { get; }

    public string RobotName => _robot.RobotName;

    public string ModeText => _robot.Kind switch
    {
        RobotConnectionKind.Mock => "Mock",
        RobotConnectionKind.MobedDirect => "MobED 직접",
        RobotConnectionKind.Agent => "Agent 경유",
        _ => "알 수 없음",
    };

    public bool IsMock => _robot.Kind == RobotConnectionKind.Mock;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LinkText))]
    private RobotLinkState _linkState;

    public string LinkText => LinkState switch
    {
        RobotLinkState.Connected => "연결",
        RobotLinkState.Connecting => "연결 중",
        RobotLinkState.Faulted => "오류",
        _ => "끊김",
    };

    [ObservableProperty]
    private PageViewModel? _currentPage;

    [ObservableProperty]
    private string? _stopMessage;

    [RelayCommand]
    private void Navigate(PageViewModel page)
    {
        foreach (var p in Pages) p.IsSelected = ReferenceEquals(p, page);
        CurrentPage = page;
    }

    [RelayCommand]
    private async Task StopAllAsync()
    {
        await _robot.RequestStopAsync();
        StopMessage = $"전체 정지 요청 보냄 · {DateTime.Now:HH:mm:ss}" + (IsMock ? " (Mock)" : "");
    }

    public Task StartAsync() => _robot.ConnectAsync();
}
