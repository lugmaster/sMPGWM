namespace sMPGWM.Scripts.ShipSystem;
public sealed class ShipResources
{
    internal ResourcePool ShieldPool { get; }
    internal ResourcePool EnergyPool { get; } = new(0, 0);
    public ResourceState Shield => new(ShieldPool.Capacity, ShieldPool.Capacity, ShieldPool.Current);
    public ResourceState Energy => new(EnergyPool.Capacity, EnergyPool.Capacity, EnergyPool.Current);
    internal ShipResources(double capacity, double current) { ShieldPool = new(capacity, current); }
}
