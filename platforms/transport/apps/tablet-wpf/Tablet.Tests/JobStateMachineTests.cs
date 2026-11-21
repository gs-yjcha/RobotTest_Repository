using Tablet.Core;

namespace Tablet.Tests;

public class JobStateMachineTests
{
    private readonly NavigationJob _job = new(Guid.NewGuid(), "destination-1");

    [Fact]
    public void ArrivalRequiresOperatorConfirmationBeforeDone()
    {
        var machine = new JobStateMachine(_job);

        Assert.Equal(JobState.Queued, machine.State);
        Assert.False(machine.TryConfirmCompletion());
        Assert.True(machine.TryApply(new(_job.Id, RobotNavigationState.Moving)));
        Assert.True(machine.TryApply(new(_job.Id, RobotNavigationState.Succeeded)));

        Assert.Equal(JobState.Arrived, machine.State);
        Assert.False(machine.TryConfirmCompletion());
        Assert.True(machine.TryRequestCompletionConfirmation());
        Assert.Equal(JobState.WaitConfirm, machine.State);
        Assert.True(machine.TryConfirmCompletion());
        Assert.Equal(JobState.Done, machine.State);
        Assert.False(machine.TryConfirmCompletion());
        Assert.False(machine.TryApply(new(_job.Id, RobotNavigationState.Failed)));
    }

    [Theory]
    [InlineData(RobotNavigationState.Failed, JobState.Failed)]
    [InlineData(RobotNavigationState.Canceled, JobState.Canceled)]
    public void TerminalUpdateAfterPauseClosesJob(RobotNavigationState outcome, JobState expected)
    {
        var machine = new JobStateMachine(_job);

        Assert.True(machine.TryApply(new(_job.Id, RobotNavigationState.Moving)));
        Assert.True(machine.TryApply(new(_job.Id, RobotNavigationState.Paused)));
        Assert.Equal(JobState.Paused, machine.State);
        Assert.True(machine.TryApply(new(_job.Id, RobotNavigationState.Moving)));
        Assert.Equal(JobState.Moving, machine.State);
        Assert.True(machine.TryApply(new(_job.Id, RobotNavigationState.Paused)));
        Assert.True(machine.TryApply(new(_job.Id, outcome)));
        Assert.Equal(expected, machine.State);
        Assert.False(machine.TryApply(new(_job.Id, RobotNavigationState.Moving)));
        Assert.False(machine.TryRequestCompletionConfirmation());
    }

    [Fact]
    public void UnrelatedIdleAndInvalidUpdatesLeaveStateUnchanged()
    {
        var machine = new JobStateMachine(_job);

        Assert.False(machine.TryApply(new(Guid.NewGuid(), RobotNavigationState.Moving)));
        Assert.False(machine.TryApply(new(_job.Id, RobotNavigationState.Succeeded)));
        Assert.False(machine.TryApply(new(_job.Id, RobotNavigationState.Idle)));
        Assert.Equal(JobState.Queued, machine.State);
        Assert.True(machine.TryApply(new(_job.Id, RobotNavigationState.Moving)));
        Assert.False(machine.TryApply(new(_job.Id, RobotNavigationState.Moving)));
        Assert.False(machine.TryApply(new(_job.Id, RobotNavigationState.Idle)));
        Assert.Equal(JobState.Moving, machine.State);
    }
}
