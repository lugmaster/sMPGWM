using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace sMPGWM.Scripts.FileLoadingSystem;

internal static class AtomicTextFile
{
    public static void WriteAllText(string path, string content)
    {
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                writer.Write(content);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }
            // A sibling file keeps replacement on the same filesystem, without truncating the destination.
            using var mutex = Acquire(path);
            try
            {
                if (File.Exists(path)) File.Replace(temporaryPath, path, null);
                else File.Move(temporaryPath, path);
            }
            finally { mutex.ReleaseMutex(); }
        }
        finally { File.Delete(temporaryPath); }
    }

    public static string ReadAllText(string path)
    {
        using var mutex = Acquire(path);
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }
        finally { mutex.ReleaseMutex(); }
    }

    public static bool Exists(string path)
    {
        using var mutex = Acquire(path);
        try { return File.Exists(path); }
        finally { mutex.ReleaseMutex(); }
    }

    private static Mutex Acquire(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (OperatingSystem.IsWindows()) fullPath = fullPath.ToUpperInvariant();
        var name = "sMPGWM.AtomicTextFile." + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fullPath)));
        var mutex = new Mutex(false, name);
        try
        {
            try
            {
                if (!mutex.WaitOne(TimeSpan.FromSeconds(10)))
                    throw new IOException("Timed out waiting to access " + path);
            }
            catch (AbandonedMutexException) { }
            return mutex;
        }
        catch { mutex.Dispose(); throw; }
    }
}
