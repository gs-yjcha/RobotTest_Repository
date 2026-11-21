namespace Tablet.Core;

public enum RobotNavigationState
{
    Idle,
    Moving,
    Paused,
    Succeeded,
    Canceled,
    Failed,
}

/// <summary>The caller supplies a new ID for each job, including repeat trips to the same destination.</summary>
public sealed record NavigationJob
{
    public NavigationJob(Guid id, string destinationId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationId);
        Id = id;
        DestinationId = destinationId;
    }

    public Guid Id { get; }
    public string DestinationId { get; }
}

public sealed record RobotNavigationUpdate(Guid JobId, RobotNavigationState State);

public interface IRobotNavigation
{
    /// <summary>Reads ordered navigation updates for a single consumer over the client lifetime.</summary>
    IAsyncEnumerable<RobotNavigationUpdate> WatchAsync(CancellationToken cancellationToken = default);

    Task StartAsync(NavigationJob job, CancellationToken cancellationToken = default);
    Task PauseAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task ResumeAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid jobId, CancellationToken cancellationToken = default);
}
