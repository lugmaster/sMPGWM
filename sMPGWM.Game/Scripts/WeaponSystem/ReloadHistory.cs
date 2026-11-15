namespace sMPGWM.Scripts.WeaponSystem;

public sealed class ReloadHistory
{
    private readonly long[] _requests = new long[32];
    private readonly ReloadResult[] _results = new ReloadResult[32];
    private long _highWater;
    private long _lastAcceptedTick = -6;

    public bool Admit(long request, long tick, out ReloadResult result)
    {
        if (request <= 0 || request == long.MaxValue || tick < -1)
        { result = ReloadResult.OldRequest; return false; }
        var index = (int)(request % _requests.Length);
        if (request <= _highWater)
        {
            result = _requests[index] == request ? _results[index] : ReloadResult.OldRequest;
            return false;
        }
        _highWater = request;
        result = tick - _lastAcceptedTick < 6 ? ReloadResult.RateLimit : ReloadResult.Pending;
        Complete(request, result);
        if (result != ReloadResult.Pending) return false;
        _lastAcceptedTick = tick;
        return true;
    }

    public void Complete(long request, ReloadResult result)
    {
        if (request <= 0 || request == long.MaxValue) return;
        var index = (int)(request % _requests.Length);
        if (_requests[index] > request) return;
        _requests[index] = request;
        _results[index] = result;
    }
}
