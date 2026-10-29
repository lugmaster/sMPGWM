using NUnit.Framework.Api;
using NUnit.Framework.Internal;

namespace sMPGWM.Tests;

public static class GodotTestRunner
{
    public static int Run(string reportPath)
    {
        var runner = new NUnitTestAssemblyRunner(new DefaultTestAssemblyBuilder());
        runner.Load(typeof(GodotTestRunner).Assembly, new Dictionary<string, object>
        {
            ["NumberOfTestWorkers"] = 0,
            ["RunOnMainThread"] = true
        });
        var result = runner.Run(TestListener.NULL, TestFilter.FromXml("<filter><cat>Godot</cat></filter>"));
        File.WriteAllText(reportPath, result.ToXml(true).OuterXml);
        return result.FailCount == 0 && result.SkipCount == 0 && result.PassCount > 0 ? 0 : 1;
    }
}

