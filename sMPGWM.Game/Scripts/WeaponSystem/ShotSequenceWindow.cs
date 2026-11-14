namespace sMPGWM.Scripts.WeaponSystem;

public struct ShotSequenceWindow
{
    private ulong _highest;
    private ulong _seen;

    public bool TryAccept(ulong sequence)
    {
        if (sequence == 0) return false;
        if (sequence > _highest)
        {
            var distance = sequence - _highest;
            _seen = distance >= 64 ? 1UL : (_seen << (int)distance) | 1UL;
            _highest = sequence;
            return true;
        }

        var age = _highest - sequence;
        if (age >= 64 || (_seen & (1UL << (int)age)) != 0) return false;
        _seen |= 1UL << (int)age;
        return true;
    }
}
