using System;
using Godot;
using sMPGWM.Scripts.GameFlowSystem;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.GameplayDataSystem;

public partial class DefinitionStartup : Node
{
    public override void _Ready()
    {
        try
        {
            GameplayDefinitions.Initialize();
        }
        catch (Exception error)
        {
            var message = $"Failed to prepare definitions at startup: {error.Message}";
            if (GameFlowManager.Instance != null) GameFlowManager.Instance.QuitWithOsError(message);
            else
            {
                Logger.Error(message);
                GetTree().Quit(1);
            }
        }
    }
}
