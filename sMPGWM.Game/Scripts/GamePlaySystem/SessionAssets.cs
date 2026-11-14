using System;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.VfxSystem;

namespace sMPGWM.Scripts.GamePlaySystem;

public sealed class SessionAssets : IDisposable
{
    public ShipAssetCatalogue Ships { get; }
    public WeaponPresentationProfile[] Profiles { get; private set; } = [];

    public SessionAssets()
    {
        Ships = new ShipAssetCatalogue(ShipRegistry.GetAll());
        try { Profiles = WeaponPresentationProfile.CreateAll(); }
        catch { Ships.Dispose(); throw; }
    }

    public void Dispose()
    {
        Ships.Dispose();
        Profiles = [];
    }
}
