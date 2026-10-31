namespace sMPGWM.Scripts.Network;

public sealed class ProtocolRateLimit
{
    private ulong _window;
    private int _count;

    public bool Accept(ulong now)
    {
        if (now < _window || now - _window >= 1000)
        {
            _window = now;
            _count = 0;
        }
        if (_count >= 16) return false;
        _count++;
        return true;
    }
}
