using System;

namespace sMPGWM.Scripts.GamePlaySystem;

internal sealed class SessionInitialization(Action complete, Action<string> fail)
{
    public SessionInitializationStage Stage { get; private set; }
    public (int Percent, string Text) Progress => Stage switch
    {
        SessionInitializationStage.PreparingDefinitions => (10, "Preparing definitions and compatibility..."),
        SessionInitializationStage.DefinitionsReady => (30, "Loading world resources..."),
        SessionInitializationStage.Constructing => (40, "Constructing session and bindings..."),
        SessionInitializationStage.AwaitingSynchronization => (60, "Synchronizing world and player state..."),
        SessionInitializationStage.Binding => (90, "Checking local gameplay bindings..."),
        SessionInitializationStage.Ready => (100, "Ready"),
        _ => (0, "Loading session...")
    };

    public void PrepareDefinitions(Action prepare)
    {
        if (Stage != SessionInitializationStage.Created) return;
        Stage = SessionInitializationStage.PreparingDefinitions;
        try
        {
            prepare();
            if (Stage == SessionInitializationStage.PreparingDefinitions)
                Stage = SessionInitializationStage.DefinitionsReady;
        }
        catch (Exception error) { fail($"Definition preparation failed: {error.Message}"); }
    }

    public void Construct(Action construct)
    {
        if (Stage != SessionInitializationStage.DefinitionsReady)
            throw new InvalidOperationException("Session definitions have not been prepared.");
        Stage = SessionInitializationStage.Constructing;
        construct();
        if (Stage == SessionInitializationStage.Constructing)
            Stage = SessionInitializationStage.AwaitingSynchronization;
    }

    public void Synchronize(Action bindAndActivate)
    {
        if (Stage != SessionInitializationStage.AwaitingSynchronization) return;
        Stage = SessionInitializationStage.Binding;
        try
        {
            bindAndActivate();
            if (Stage != SessionInitializationStage.Binding) return;
            Stage = SessionInitializationStage.Ready;
            complete();
        }
        catch (Exception error) { fail($"Session readiness failed: {error.Message}"); }
    }

    public void Stop() => Stage = SessionInitializationStage.Stopped;
}
