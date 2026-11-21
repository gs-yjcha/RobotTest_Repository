using System.Threading.Channels;
using Tablet.Core;

namespace Tablet.Mock;

/// <summary>Simulates a single robot and exposes deterministic navigation outcomes for tests.</summary>
public sealed class MockRobotClient : IRobotClient, IRobotNavigation
{
    private readonly object _gate = new();
    private readonly TimeSpan _connectDelay;
    private readonly Channel<RobotNavigationUpdate> _updates = Channel.CreateUnbounded<RobotNavigationUpdate>(
        new UnboundedChannelOptions { SingleReader = true });
    private RobotLinkState _linkState = RobotLinkState.Disconnected;
    private RobotNavigationUpdate? _activeNavigation;
    private int _stopRequestCount;
    private bool _disposed;

    public MockRobotClient(string robotName = "MobED-01 (Mock)", TimeSpan? connectDelay = null)
    {
        RobotName = robotName;
        _connectDelay = connectDelay ?? TimeSpan.FromMilliseconds(300);
    }

    public string RobotName { get; }

    public RobotConnectionKind Kind => RobotConnectionKind.Mock;

    public RobotLinkState LinkState
    {
        get { lock (_gate) return _linkState; }
    }

    public IRobotNavigation Navigation => this;

    /// <summary>정지 요청을 받은 횟수 (시험 확인용).</summary>
    public int StopRequestCount => Volatile.Read(ref _stopRequestCount);

    public event EventHandler<RobotLinkState>? LinkStateChanged;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_linkState == RobotLinkState.Connected)
                return;
            if (_linkState == RobotLinkState.Connecting)
                throw new InvalidOperationException("A connection attempt is already in progress.");

            _linkState = RobotLinkState.Connecting;
        }

        try
        {
            LinkStateChanged?.Invoke(this, RobotLinkState.Connecting);
            await Task.Delay(_connectDelay, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            FinishConnection(RobotLinkState.Disconnected);
            throw;
        }

        FinishConnection(RobotLinkState.Connected);
    }

    public IAsyncEnumerable<RobotNavigationUpdate> WatchAsync(CancellationToken cancellationToken = default) =>
        _updates.Reader.ReadAllAsync(cancellationToken);

    public Task StartAsync(NavigationJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        lock (_gate)
        {
            CheckNavigationAvailable(cancellationToken);
            if (_activeNavigation is not null)
                throw new InvalidOperationException("A navigation job is already active.");

            PublishNavigation(job.Id, RobotNavigationState.Moving);
        }

        return Task.CompletedTask;
    }

    public Task PauseAsync(Guid jobId, CancellationToken cancellationToken = default) =>
        ChangeNavigationAsync(jobId, RobotNavigationState.Moving, RobotNavigationState.Paused, cancellationToken);

    public Task ResumeAsync(Guid jobId, CancellationToken cancellationToken = default) =>
        ChangeNavigationAsync(jobId, RobotNavigationState.Paused, RobotNavigationState.Moving, cancellationToken);

    public Task CancelAsync(Guid jobId, CancellationToken cancellationToken = default) =>
        ChangeNavigationAsync(jobId, null, RobotNavigationState.Canceled, cancellationToken);

    public bool TryFinishNavigation(Guid jobId, bool succeeded)
    {
        lock (_gate)
        {
            if (_activeNavigation is null || _activeNavigation.JobId != jobId)
                return false;
            if (succeeded && _activeNavigation.State != RobotNavigationState.Moving)
                return false;

            PublishNavigation(jobId, succeeded ? RobotNavigationState.Succeeded : RobotNavigationState.Failed);
            return true;
        }
    }

    public Task RequestStopAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(_disposed, this);
            Interlocked.Increment(ref _stopRequestCount);
            if (_activeNavigation is not null)
                PublishNavigation(_activeNavigation.JobId, RobotNavigationState.Canceled);
        }

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        bool linkChanged;
        lock (_gate)
        {
            if (_disposed)
                return ValueTask.CompletedTask;

            _disposed = true;
            linkChanged = _linkState != RobotLinkState.Disconnected;
            _linkState = RobotLinkState.Disconnected;
            _activeNavigation = null;
            _updates.Writer.Complete();
        }

        if (linkChanged)
            LinkStateChanged?.Invoke(this, RobotLinkState.Disconnected);

        return ValueTask.CompletedTask;
    }

    private void FinishConnection(RobotLinkState state)
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            _linkState = state;
        }

        LinkStateChanged?.Invoke(this, state);
    }

    private void CheckNavigationAvailable(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_linkState != RobotLinkState.Connected)
            throw new InvalidOperationException("The mock robot is disconnected.");
    }

    private Task ChangeNavigationAsync(
        Guid jobId,
        RobotNavigationState? expectedState,
        RobotNavigationState next,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            CheckNavigationAvailable(cancellationToken);
            if (_activeNavigation is null || _activeNavigation.JobId != jobId ||
                (expectedState.HasValue && _activeNavigation.State != expectedState.Value))
            {
                throw new InvalidOperationException("The job is not in a valid state for this command.");
            }

            PublishNavigation(jobId, next);
        }

        return Task.CompletedTask;
    }

    private void PublishNavigation(Guid jobId, RobotNavigationState state)
    {
        var update = new RobotNavigationUpdate(jobId, state);
        _activeNavigation = state is RobotNavigationState.Moving or RobotNavigationState.Paused ? update : null;
        _updates.Writer.TryWrite(update);
    }
}
