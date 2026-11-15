using sMPGWM.Scripts.MovementSystem;

namespace sMPGWM.Tests.MovementSystem;

internal sealed class MovementCodecFixture
{
    private readonly Dictionary<string, int> _indices = new();
    private readonly Dictionary<int, string> _entities = new();

    public int Index(string entity)
    {
        if (_indices.TryGetValue(entity, out var index)) return index;
        index = _indices.Count + 1;
        _indices.Add(entity, index);
        _entities.Add(index, entity);
        return index;
    }

    public string? Resolve(int index) => _entities.GetValueOrDefault(index);

    public byte[] Snapshot(Guid session, long tick, IReadOnlyList<MovementSample> samples)
    {
        var bytes = new byte[MovementWire.SnapshotHeaderBytes + samples.Count * MovementWire.SampleBytes];
        MovementWire.SnapshotInto(session, tick, samples, Index, bytes);
        return bytes;
    }

    public bool ReadSnapshot(byte[] bytes, out Guid session, out long tick, out List<MovementSample> samples)
    {
        samples = new(MovementWire.MaxPlayers);
        return MovementWire.ReadSnapshot(bytes, Resolve, samples, out session, out tick);
    }

    public byte[] Commands(Guid session, string entity, long generation, MovementCommand[] commands)
    {
        var bytes = new byte[MovementWire.CommandHeaderBytes + commands.Length * MovementWire.CommandBytes];
        MovementWire.CommandsInto(session, Index(entity), generation, commands, bytes);
        return bytes;
    }

    public bool ReadCommands(byte[] bytes, out Guid session, out string? entity, out long generation, out MovementCommand[] commands)
    {
        var buffer = new MovementCommand[MovementWire.MaxCommands];
        var valid = MovementWire.ReadCommands(bytes, buffer, out session, out var index, out generation, out var count);
        entity = Resolve(index);
        commands = buffer[..count];
        return valid && entity != null;
    }
}
