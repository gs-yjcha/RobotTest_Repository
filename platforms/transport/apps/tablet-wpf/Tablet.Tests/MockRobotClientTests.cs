using Tablet.Core;
using Tablet.Mock;

namespace Tablet.Tests;

public class MockRobotClientTests
{
    [Fact]
    public async Task Connect_GoesThroughConnecting_ToConnected()
    {
        var client = new MockRobotClient(connectDelay: TimeSpan.Zero);
        var states = new List<RobotLinkState>();
        client.LinkStateChanged += (_, s) => states.Add(s);

        await client.ConnectAsync();

        Assert.Equal(RobotConnectionKind.Mock, client.Kind);
        Assert.Equal(RobotLinkState.Connected, client.LinkState);
        Assert.Equal(new[] { RobotLinkState.Connecting, RobotLinkState.Connected }, states);
    }

    [Fact]
    public async Task RequestStop_IsCounted()
    {
        var client = new MockRobotClient(connectDelay: TimeSpan.Zero);

        await client.RequestStopAsync();
        await client.RequestStopAsync();

        Assert.Equal(2, client.StopRequestCount);
    }

    [Fact]
    public async Task Dispose_Disconnects()
    {
        var client = new MockRobotClient(connectDelay: TimeSpan.Zero);
        await client.ConnectAsync();

        await client.DisposeAsync();

        Assert.Equal(RobotLinkState.Disconnected, client.LinkState);
    }
}
