using Godot;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;
using LogLevel = sMPGWM.Scripts.LoggingSystem.LogLevel;

namespace sMPGWM.Tests.LoggingSystem;

[TestFixture, Category("Godot")]
public class LoggerTests
{
    [Test]
    public void FileLogging_CreatesThenPreservesEarlierMessages()
    {
        var previousPath = Logger.LogFilePath;
        var previousEnabled = Logger.WriteToFile;
        var previousLevel = Logger.MinimumLevel;
        var path = "user://logger-test-" + Guid.NewGuid().ToString("N") + ".log";
        try
        {
            Logger.LogFilePath = path;
            Logger.WriteToFile = true;
            Logger.MinimumLevel = LogLevel.Info;
            Logger.Info("first entry");
            Logger.Debug("filtered entry");
            Logger.Warning("second entry");
            var lines = Godot.FileAccess.GetFileAsString(path).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.That(lines, Has.Length.EqualTo(2));
            Assert.That(lines[0], Does.EndWith("[Info] first entry"));
            Assert.That(lines[1], Does.EndWith("[Warning] second entry"));
        }
        finally
        {
            Logger.LogFilePath = previousPath;
            Logger.WriteToFile = previousEnabled;
            Logger.MinimumLevel = previousLevel;
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
        }
    }
}
