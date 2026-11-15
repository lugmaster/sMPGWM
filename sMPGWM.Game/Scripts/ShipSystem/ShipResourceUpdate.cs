namespace sMPGWM.Scripts.ShipSystem;

public readonly record struct ShipResourceUpdate(long Revision, double Shield, double Energy,
    long ConfigurationRevision, long Tick, long ShieldDeadline, bool Depleted, bool Protected,
    bool Afterburner, double Health);
