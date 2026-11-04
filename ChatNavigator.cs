using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PawFamily;

public static class ChatNavigator
{
    public static string Open(Chat chat)
    {
        string uri = Records.Link(chat);
        using var running = FindRunning();
        if (running == null)
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
            return "Sent the chat link to the registered app";
        }

        // Target the already running installation. The application's single-instance
        // handler receives the documented protocol argument from a short-lived launcher.
        string path = running.MainModule?.FileName
            ?? throw new InvalidOperationException("Could not locate the open app.");
        AllowSetForegroundWindow(running.Id);
        if (IsIconic(running.MainWindowHandle)) ShowWindowAsync(running.MainWindowHandle, 9);
        SetForegroundWindow(running.MainWindowHandle);
        using var launcher = Process.Start(ForRunningInstallation(path, uri))
            ?? throw new InvalidOperationException("The chat link could not be delivered.");
        return $"Sent chat link to the running installation (PID {running.Id}).";
    }

    public static ProcessStartInfo ForRunningInstallation(string path, string uri)
    {
        var start = new ProcessStartInfo(path) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(path)! };
        start.ArgumentList.Add("--single-argument");
        start.ArgumentList.Add(uri);
        return start;
    }

    static Process? FindRunning()
    {
        foreach (var name in new[] { "ChatGPT", "Codex" })
        foreach (var process in Process.GetProcessesByName(name))
        {
            try
            {
                var path = process.MainModule?.FileName ?? "";
                if (process.MainWindowHandle != IntPtr.Zero &&
                    (name == "Codex" || path.Contains("OpenAI.Codex_", StringComparison.OrdinalIgnoreCase))) return process;
            }
            catch (System.ComponentModel.Win32Exception) { }
            catch (InvalidOperationException) { }
            process.Dispose();
        }
        return null;
    }
    [DllImport("user32.dll")] static extern bool AllowSetForegroundWindow(int processId);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] static extern bool ShowWindowAsync(IntPtr window, int command);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr window);
}
