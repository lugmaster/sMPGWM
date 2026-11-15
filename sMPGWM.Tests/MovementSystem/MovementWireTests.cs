using System.Buffers.Binary;
using Godot;
using sMPGWM.Scripts.MovementSystem;

namespace sMPGWM.Tests.MovementSystem;

public sealed class MovementWireTests
{
    [Test]
    public void NumericIdentityRoundTripDoesNotAllocateAfterBuffersArePrepared()
    {
        var fixture = new MovementCodecFixture();
        var session = Guid.NewGuid();
        var samples = new MovementSample[MovementWire.MaxPlayers];
        for (var i = 0; i < samples.Length; i++)
            samples[i] = new(i + 1, Guid.NewGuid().ToString("N"), 1, 0, Vector2.Zero, 0, Vector2.Zero, default, 20);
        var bytes = fixture.Snapshot(session, 20, samples);
        var decoded = new List<MovementSample>(MovementWire.MaxPlayers);
        Func<string, int> index = fixture.Index;
        Func<int, string?> resolve = fixture.Resolve;
        var commands = new MovementCommand[] { new(20, default), new(21, default) };
        var commandBytes = new byte[MovementWire.CommandHeaderBytes + commands.Length * MovementWire.CommandBytes];
        var decodedCommands = new MovementCommand[MovementWire.MaxCommands];
        var valid = true;
        for (var iteration = 0; iteration < 100; iteration++) Run();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < 1000; iteration++) Run();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(valid, Is.True);
        Assert.That(decoded, Is.EqualTo(samples));
        Assert.That(allocated, Is.Zero);

        void Run()
        {
            MovementWire.SnapshotInto(session, 20, samples, index, bytes);
            valid &= MovementWire.ReadSnapshot(bytes, resolve, decoded, out _, out _);
            MovementWire.CommandsInto(session, 1, 1, commands, commandBytes);
            valid &= MovementWire.ReadCommands(commandBytes, decodedCommands, out _, out _, out _, out _);
        }
    }

    [Test]
    public void UnknownDuplicateAndMalformedIndicesRejectTheWholeSnapshot()
    {
        var fixture = new MovementCodecFixture();
        var samples = new MovementSample[]
        {
            new(1, Guid.NewGuid().ToString("N"), 1, 0, Vector2.Zero, 0, Vector2.Zero, default, 20),
            new(2, Guid.NewGuid().ToString("N"), 1, 0, Vector2.Zero, 0, Vector2.Zero, default, 20)
        };
        foreach (var index in new[] { 0, -1, 1, int.MaxValue })
        {
            var bytes = fixture.Snapshot(Guid.NewGuid(), 20, samples);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(MovementWire.SnapshotHeaderBytes + MovementWire.SampleBytes + 4), index);
            var decoded = new List<MovementSample>(MovementWire.MaxPlayers);
            Assert.That(MovementWire.ReadSnapshot(bytes, fixture.Resolve, decoded, out _, out _), Is.False);
            Assert.That(decoded, Is.Empty);
        }
    }

    [Test]
    public void CommandDecoderRejectsTruncationExcessRecordsAndShortDestination()
    {
        var fixture = new MovementCodecFixture();
        var commands = new MovementCommand[] { new(1, default), new(2, default) };
        var bytes = fixture.Commands(Guid.NewGuid(), Guid.NewGuid().ToString("N"), 1, commands);
        var destination = new MovementCommand[MovementWire.MaxCommands];
        for (var size = 0; size < bytes.Length; size++)
            Assert.That(MovementWire.ReadCommands(bytes.AsSpan(0, size), destination, out _, out _, out _, out _), Is.False);
        Assert.That(MovementWire.ReadCommands(bytes, destination.AsSpan(0, 1), out _, out _, out _, out _), Is.False);
        bytes[MovementWire.CommandHeaderBytes - 1] = MovementWire.MaxCommands + 1;
        Assert.That(MovementWire.ReadCommands(bytes, destination, out _, out _, out _, out _), Is.False);
    }
}
