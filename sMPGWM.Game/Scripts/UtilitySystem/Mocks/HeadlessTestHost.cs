using System;
using System.IO;
using System.Runtime.Loader;
using Godot;

namespace sMPGWM.Scripts.UtilitySystem.Mocks;

/// <summary>Hosts the NUnit integration suite inside the engine, without a test-specific registry.</summary>
public partial class HeadlessTestHost : Node
{
    public override void _Ready()
    {
        try
        {
            var args = OS.GetCmdlineUserArgs();
            var assemblyPath = args[0];
            var directory = Path.GetDirectoryName(assemblyPath)!;
            // Share the engine load context so tests use the initialized Godot bindings and the real registry.
            var context = AssemblyLoadContext.GetLoadContext(typeof(HeadlessTestHost).Assembly)!;
            context.Resolving += (_, name) =>
            {
                var path = Path.Combine(directory, name.Name + ".dll");
                return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
            };
            var tests = context.LoadFromAssemblyPath(assemblyPath);
            var result = tests.GetType("sMPGWM.Tests.GodotTestRunner")!.GetMethod("Run")!
                .Invoke(null, new object[] { args[1] });
            GetTree().Quit((int)result!);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }
}
