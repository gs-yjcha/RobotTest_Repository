namespace Tablet.Core;

public enum JobState
{
    Queued,
    Moving,
    Paused,
    Arrived,
    WaitConfirm,
    Done,
    Failed,
    Canceled,
}

/// <summary>Tracks a single job. Robot arrival and operator confirmation are separate transitions.</summary>
public sealed class JobStateMachine(NavigationJob job)
{
    private readonly object _gate = new();
    private JobState _state = JobState.Queued;

    public NavigationJob Job { get; } = job ?? throw new ArgumentNullException(nameof(job));

    public JobState State
    {
        get { lock (_gate) return _state; }
    }

    public bool TryApply(RobotNavigationUpdate update)
    {
        if (update.JobId != Job.Id)
            return false;

        lock (_gate)
        {
            JobState next = (_state, update.State) switch
            {
                (JobState.Queued or JobState.Paused, RobotNavigationState.Moving) => JobState.Moving,
                (JobState.Moving, RobotNavigationState.Paused) => JobState.Paused,
                (JobState.Moving, RobotNavigationState.Succeeded) => JobState.Arrived,
                (JobState.Queued or JobState.Moving or JobState.Paused, RobotNavigationState.Failed) => JobState.Failed,
                (JobState.Queued or JobState.Moving or JobState.Paused, RobotNavigationState.Canceled) => JobState.Canceled,
                _ => _state,
            };

            if (next == _state)
                return false;

            _state = next;
            return true;
        }
    }

    public bool TryRequestCompletionConfirmation() => TryTransition(JobState.Arrived, JobState.WaitConfirm);

    public bool TryConfirmCompletion() => TryTransition(JobState.WaitConfirm, JobState.Done);

    private bool TryTransition(JobState expected, JobState next)
    {
        lock (_gate)
        {
            if (_state != expected)
                return false;

            _state = next;
            return true;
        }
    }
}
