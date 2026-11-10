using System;

namespace sMPGWM.Scripts.GamePlaySystem;

public sealed class ShipGameplayState
{
    public ResourcePool Shield { get; }
    public long Revision { get; private set; } = 1;
    public long BehaviourInstance { get; private set; }
    public long Deadline { get; private set; }
    public event Action? Changed;
    public ShipGameplayState(double capacity, double current) => Shield = new(capacity, current);
    public void Apply(ShieldBoostDefinition definition, long tick, long instance)
    {
        if (BehaviourInstance == 0)
        {
            Shield.SetContribution(instance, definition.Flat, definition.Fraction);
            BehaviourInstance = instance;
        }
        Deadline = checked(tick + definition.DurationTicks);
        Revision++;
        Changed?.Invoke();
    }
    public bool Expire(long tick)
    {
        if (BehaviourInstance == 0 || tick < Deadline) return false;
        Shield.RemoveOwner(BehaviourInstance);
        BehaviourInstance = Deadline = 0;
        Revision++;
        Changed?.Invoke();
        return true;
    }
    public bool Confirm(long revision, double capacity, double current, long instance, long deadline)
    {
        if (revision <= Revision) return false;
        Shield.Confirm(capacity, current);
        Revision = revision; BehaviourInstance = instance; Deadline = deadline;
        Changed?.Invoke();
        return true;
    }
}
