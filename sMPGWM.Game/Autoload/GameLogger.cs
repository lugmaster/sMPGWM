using System;
using Godot;
using sMPGWM.Scripts.Logging;

namespace sMPGWM.Autoload;

public partial class GameLogger : AbstractAutoload<GameLogger>
{
    [Export] public LogLevel MinimumLevel { get; set; } = LogLevel.Info;
    [Export] public bool WriteToFile { get; set; }
    [Export] public string LogFilePath { get; set; } = "user://game.log";

    public static void Trace(string message) => Instance.Log(LogLevel.Trace, message);
    public static void Debug(string message) => Instance.Log(LogLevel.Debug, message);
    public static void Info(string message) => Instance.Log(LogLevel.Info, message);
    public static void Warning(string message) => Instance.Log(LogLevel.Warning, message);
    public static void Error(string message) => Instance.Log(LogLevel.Error, message);

    private void Log(LogLevel level, string message)
    {
        if (level < MinimumLevel)
            return;

        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

        if (WriteToFile)
        {
            using var file = FileAccess.Open(LogFilePath, FileAccess.ModeFlags.WriteRead);

            if (file == null)
                throw new InvalidOperationException($"Could not open log file: {LogFilePath}");

            file.SeekEnd();
            file.StoreLine(line);
            return;
        }

        switch (level)
        {
            case LogLevel.Warning:
                GD.PushWarning(line);
                break;
            case LogLevel.Error:
                GD.PushError(line);
                break;
            case LogLevel.Trace:
            case LogLevel.Debug:
            case LogLevel.Info:
            default:
                GD.Print(line);
                break;
        }
    }
}
