using Tablet.Core;

namespace Tablet.Mock;

/// <summary>실기 없이 화면을 시험하는 가짜 로봇. 연결은 짧은 지연 후 성공한다.</summary>
public sealed class MockRobotClient : IRobotClient
{
    private readonly TimeSpan _connectDelay;
    private RobotLinkState _linkState = RobotLinkState.Disconnected;

    public MockRobotClient(string robotName = "MobED-01 (Mock)", TimeSpan? connectDelay = null)
    {
        RobotName = robotName;
        _connectDelay = connectDelay ?? TimeSpan.FromMilliseconds(300);
    }

    public string RobotName { get; }

    public RobotConnectionKind Kind => RobotConnectionKind.Mock;

    public RobotLinkState LinkState => _linkState;

    /// <summary>정지 요청을 받은 횟수 (시험 확인용).</summary>
    public int StopRequestCount { get; private set; }

    public event EventHandler<RobotLinkState>? LinkStateChanged;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        SetState(RobotLinkState.Connecting);
        await Task.Delay(_connectDelay, cancellationToken).ConfigureAwait(false);
        SetState(RobotLinkState.Connected);
    }

    public Task RequestStopAsync(CancellationToken cancellationToken = default)
    {
        StopRequestCount++;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        SetState(RobotLinkState.Disconnected);
        return ValueTask.CompletedTask;
    }

    private void SetState(RobotLinkState state)
    {
        if (_linkState == state) return;
        _linkState = state;
        LinkStateChanged?.Invoke(this, state);
    }
}
