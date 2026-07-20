using Godot;
using sMPGWM.Scripts.Singletons.Base;

namespace sMPGWM.Scripts.Singletons.Autoload;

public partial class GlobalStateManager : AbstractSingleton<GlobalStateManager>
{
    public static bool IsWindowFocused { get; private set; } = true;
    public static int FastSyncInterval { get; set; } = 2;
    public static int SlowSyncInterval { get; set; } = 13;


    public override void _Notification(int what)
    {
        const int notificationWmFocusIn = 1004;
        const int notificationWmFocusOut = 1005;

        if (what is notificationWmFocusIn or notificationWmFocusOut)
        {
            IsWindowFocused = DisplayServer.WindowIsFocused();
        }
    }
}