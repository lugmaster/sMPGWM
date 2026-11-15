using sMPGWM.Scripts.NetworkSystem;

namespace sMPGWM.Tests.NetworkSystem;

[Category("Godot")]
public sealed class HeartbeatMonitorTests
{
    [Test]
    public void IdleHostMonitoringDoesNotAllocatePerFrame()
    {
        var monitor = new HeartbeatMonitor { RequestIntervalSeconds = 3600, SilenceTimeoutSeconds = 7200 };
        try
        {
            monitor.Initialize(true);
            for (var peer = 2; peer <= 10; peer++) monitor.Track(peer);
            for (var i = 0; i < 100; i++) monitor._Process(1.0 / 60);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++) monitor._Process(1.0 / 60);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
        }
        finally { monitor.Stop(); monitor.Free(); }
    }
}
