namespace sMPGWM.Scripts.GamePlaySystem;

public sealed class ActivationHistory
{
    private readonly long[] _requests = new long[32];
    private readonly string?[] _results = new string?[32];
    private long _highWater;
    private long _lastAcceptedTick = -6;
    public bool Admit(long request, long tick, out string result)
    {
        var index = (int)(request % _requests.Length);
        if (request <= _highWater)
        {
            result = request > 0 && _requests[index] == request ? _results[index]! : "Rejected: old request";
            return false;
        }
        _highWater = request;
        result = tick - _lastAcceptedTick < 6 ? "Rejected: rate limit" : "Pending";
        Complete(request, result);
        if (result != "Pending") return false;
        _lastAcceptedTick = tick;
        return true;
    }
    public void Complete(long request, string result)
    {
        var index = (int)(request % _requests.Length);
        if (_requests[index] > request) return;
        _requests[index] = request; _results[index] = result;
    }
}
