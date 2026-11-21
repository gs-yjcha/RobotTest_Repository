namespace Tablet.Core;

/// <summary>로봇 연결 방식. 화면은 이 값으로 Mock/실기를 구분해 표시한다.</summary>
public enum RobotConnectionKind
{
    /// <summary>실기 없이 화면을 시험하는 가짜 로봇.</summary>
    Mock,

    /// <summary>태블릿이 MobED에 직접 연결 (1단계).</summary>
    MobedDirect,

    /// <summary>차상 PC Robot Agent 경유 (이후 단계).</summary>
    Agent,
}

/// <summary>로봇 연결 상태.</summary>
public enum RobotLinkState
{
    Disconnected,
    Connecting,
    Connected,
    Faulted,
}

/// <summary>
/// 화면이 로봇과 말하는 유일한 통로. 구현은 Mock → MobED 직접 → Agent 순서로 바꿔 끼운다.
/// 주행을 지원하는 구현은 Navigation으로 작업 명령과 상태 스트림을 제공한다.
/// </summary>
public interface IRobotClient : IAsyncDisposable
{
    /// <summary>상단 상태바에 보일 로봇 이름.</summary>
    string RobotName { get; }

    RobotConnectionKind Kind { get; }

    RobotLinkState LinkState { get; }

    /// <summary>주행 명령과 상태를 제공한다. 연결 전용 어댑터는 null을 반환한다.</summary>
    IRobotNavigation? Navigation => null;

    event EventHandler<RobotLinkState>? LinkStateChanged;

    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>전체 정지 요청. 화면 어디서든 호출할 수 있어야 한다.</summary>
    Task RequestStopAsync(CancellationToken cancellationToken = default);
}
