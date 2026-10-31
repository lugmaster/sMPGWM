using System;

namespace sMPGWM.Scripts.Network;

public sealed class PeerHealthMonitor
{
    public const ulong Interval = 2000;
    public const ulong Timeout = 10000;
    private readonly ulong[] _sent = new ulong[8];
    private readonly long[] _pending = new long[8];
    private long _sequence;
    private long _latestSequence;
    private ulong _nextProbe;
    private ulong _lastResponse;
    public string Token { get; } = Guid.NewGuid().ToString("N");
    public double LatestRtt { get; private set; }
    public double SmoothedRtt { get; private set; }
    public long SampleCount { get; private set; }
    public PeerHealthMonitor(ulong now) { _lastResponse = now; }
    public ulong SampleAge(ulong now) => now - _lastResponse;
    public bool IsExpired(ulong now) => SampleAge(now) >= Timeout;
    public bool IsAvailable(ulong now) => SampleCount > 0 && !IsExpired(now);

    public long Probe(ulong now)
    {
        if (now < _nextProbe || IsExpired(now)) return 0;
        _nextProbe = now + Interval;
        var sequence = ++_sequence;
        var slot = (int)(sequence % _pending.Length);
        _pending[slot] = sequence;
        _sent[slot] = now;
        return sequence;
    }

    public bool Reply(long sequence, string token, ulong now)
    {
        if (sequence <= 0 || token != Token || IsExpired(now)) return false;
        var slot = (int)(sequence % _pending.Length);
        if (_pending[slot] != sequence || now < _sent[slot] || now - _sent[slot] >= Timeout) return false;
        _pending[slot] = 0;
        _lastResponse = now;
        if (sequence <= _latestSequence) return true;
        _latestSequence = sequence;
        LatestRtt = now - _sent[slot];
        SmoothedRtt = SampleCount == 0 ? LatestRtt : SmoothedRtt + 0.125 * (LatestRtt - SmoothedRtt);
        SampleCount++;
        return true;
    }
}
