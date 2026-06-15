using System;
using Godot;
using sMPGWM.scripts.logging;

namespace sMPGWM.autoload;

public static class GameLogger
{
    public static LogLevel MinimumLevel { get; set; } = LogLevel.Info;
    public static bool WriteToFile { get; set; } = false;
    public static string LogFilePath { get; set; } = "user://game.log";

    public static void Trace(string message) => Log(LogLevel.Trace, message);
    public static void Debug(string message) => Log(LogLevel.Debug, message);
    public static void Info(string message) => Log(LogLevel.Info, message);
    public static void Warning(string message) => Log(LogLevel.Warning, message);
    public static void Error(string message) => Log(LogLevel.Error, message);

    private static void Log(LogLevel level, string message)
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
            default:
                GD.Print(line);
                break;
        }
    }
}