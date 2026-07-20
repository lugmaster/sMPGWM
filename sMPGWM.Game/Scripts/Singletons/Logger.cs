using System;
using Godot;
using sMPGWM.Scripts.Enums;

namespace sMPGWM.Scripts.Singletons;

public static class Logger
{
    public static LogLevel MinimumLevel { get; set; } = LogLevel.Trace;
    public static bool WriteToFile { get; set; }
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
            case LogLevel.Trace:
            case LogLevel.Debug:
            case LogLevel.Info:
            default:
                GD.Print(line);
                break;
        }
    }
}
