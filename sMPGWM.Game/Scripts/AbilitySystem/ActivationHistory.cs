namespace sMPGWM.Scripts.AbilitySystem;

public sealed class ActivationHistory
{
    private readonly long[] _requests = new long[32];
    private readonly AbilityActivationResult[] _results = new AbilityActivationResult[32];
    private long _highWater;
    private long _lastAcceptedTick = -6;
    public bool Admit(long request, long tick, out AbilityActivationResult result)
    {
        var index = (int)(request % _requests.Length);
        if (request <= _highWater)
        {
            result = request > 0 && _requests[index] == request ? _results[index] : AbilityActivationResult.OldRequest;
            return false;
        }
        _highWater = request;
        result = tick - _lastAcceptedTick < 6 ? AbilityActivationResult.RateLimit : AbilityActivationResult.Pending;
        Complete(request, result);
        if (result != AbilityActivationResult.Pending) return false;
        _lastAcceptedTick = tick;
        return true;
    }
    public void Complete(long request, AbilityActivationResult result)
    {
        var index = (int)(request % _requests.Length);
        if (_requests[index] > request) return;
        _requests[index] = request; _results[index] = result;
    }
}
