namespace sMPGWM.Scripts.VfxSystem;

public static class WeaponPresentationProfiles
{
    private static readonly string[] Ids = ["smg_standard", "laser_standard"];
    public static int Count => Ids.Length;
    public static string GetId(int index) => Ids[index];

    public static int IndexOf(string id)
    {
        for (var i = 0; i < Ids.Length; i++) if (Ids[i] == id) return i;
        return -1;
    }
}
