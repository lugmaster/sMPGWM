using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.Network;

/// <summary>Monotonic host-time estimate and bounded RTT probes; time is supplied by the session.</summary>
public sealed class NetworkClock(Func<ulong> now)
{
    private readonly Dictionary<long, ulong> _probes = new();
    private double _hostAtReceipt;
    private ulong _receivedAt, _lastProbe;
    public double Rtt { get; private set; }
    public double Jitter { get; private set; }
    public double HostEstimate => _hostAtReceipt + (now() - _receivedAt) * .06;
    public int Lead => Math.Clamp((int)Math.Ceiling((Rtt / 2 + Jitter * 2) * 60) + 3, 3, 24);
    public double PresentationTick => HostEstimate - Rtt * 30 - 4;

    public void Observe(long tick, bool baseline)
    {
        _hostAtReceipt = baseline ? tick : HostEstimate + Math.Clamp(tick + Rtt * 30 - HostEstimate, -1, 1);
        _receivedAt = now();
    }
    public bool TryProbe(out long token)
    {
        token = (long)now();
        if (now() - _lastProbe <= 1000) return false;
        _lastProbe = now();
        _probes.Clear();
        _probes[token] = now();
        return true;
    }
    // False means a valid probe exceeded the supported latency, requiring recovery.
    public bool Reply(long token, long hostTick)
    {
        if (!_probes.Remove(token, out var sent) || hostTick < 0 || hostTick > MovementWire.MaxTick) return true;
        double measured = (now() - sent) / 1000.0;
        if (measured > 2) return false;
        Jitter += (Math.Abs(measured - Rtt) - Jitter) * .2;
        Rtt = Rtt == 0 ? measured : Rtt + (measured - Rtt) * .2;
        _hostAtReceipt = HostEstimate + Math.Clamp(hostTick + measured * 30 - HostEstimate, -1, 1);
        _receivedAt = now();
        return true;
    }
    public void ClearProbes() { _probes.Clear(); _lastProbe = now(); }
}
