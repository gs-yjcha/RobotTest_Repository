using Tablet.Core;
using Tablet.Mock;

namespace Tablet.Tests;

public class MockNavigationTests
{
    [Fact]
    public async Task MockUpdatesDriveAPauseResumeAndArrival()
    {
        var client = new MockRobotClient(connectDelay: TimeSpan.Zero);
        await using var cleanup = client;
        IRobotClient robot = client;
        IRobotNavigation navigation = Assert.IsAssignableFrom<IRobotNavigation>(robot.Navigation);
        var job = new NavigationJob(Guid.NewGuid(), "destination-1");

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var updates = navigation.WatchAsync(timeout.Token).GetAsyncEnumerator();
        var machine = new JobStateMachine(job);
        var firstUpdate = updates.MoveNextAsync();

        await robot.ConnectAsync();
        await navigation.StartAsync(job);
        Assert.True(await firstUpdate);
        Assert.True(machine.TryApply(updates.Current));
        Assert.Equal(JobState.Moving, machine.State);

        await navigation.PauseAsync(job.Id);
        await navigation.ResumeAsync(job.Id);
        Assert.True(client.TryFinishNavigation(job.Id, succeeded: true));
        await client.DisposeAsync();

        var states = new List<JobState>();
        while (await updates.MoveNextAsync())
        {
            Assert.True(machine.TryApply(updates.Current));
            states.Add(machine.State);
        }

        Assert.Equal(new[] { JobState.Paused, JobState.Moving, JobState.Arrived }, states);
    }

    [Fact]
    public async Task MockCanReportFailureDuringPause()
    {
        await using var client = new MockRobotClient(connectDelay: TimeSpan.Zero);
        var job = new NavigationJob(Guid.NewGuid(), "destination-1");
        await client.ConnectAsync();
        await client.StartAsync(job);
        await client.PauseAsync(job.Id);
        Assert.False(client.TryFinishNavigation(job.Id, succeeded: true));
        Assert.True(client.TryFinishNavigation(job.Id, succeeded: false));
        await client.DisposeAsync();

        var machine = new JobStateMachine(job);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await foreach (var update in client.WatchAsync(timeout.Token))
            Assert.True(machine.TryApply(update));

        Assert.Equal(JobState.Failed, machine.State);
    }

    [Fact]
    public async Task CommandsProtectActiveJobAndStopClosesIt()
    {
        await using var client = new MockRobotClient(connectDelay: TimeSpan.Zero);
        var previous = new NavigationJob(Guid.NewGuid(), "destination-1");
        var current = new NavigationJob(Guid.NewGuid(), "destination-1");

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.StartAsync(previous));
        await client.ConnectAsync();
        await client.StartAsync(previous);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.StartAsync(current));
        await client.CancelAsync(previous.Id);
        await client.StartAsync(current);
        Assert.False(client.TryFinishNavigation(previous.Id, succeeded: true));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.CancelAsync(previous.Id));
        await client.RequestStopAsync();
        Assert.False(client.TryFinishNavigation(current.Id, succeeded: true));
        await client.DisposeAsync();

        var updates = new List<RobotNavigationUpdate>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await foreach (var update in client.WatchAsync(timeout.Token))
            updates.Add(update);

        Assert.Equal(
            new RobotNavigationUpdate[]
            {
                new(previous.Id, RobotNavigationState.Moving),
                new(previous.Id, RobotNavigationState.Canceled),
                new(current.Id, RobotNavigationState.Moving),
                new(current.Id, RobotNavigationState.Canceled),
            },
            updates);
        Assert.Equal(1, client.StopRequestCount);
    }
}
