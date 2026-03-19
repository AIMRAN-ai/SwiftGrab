using System;
using Microsoft.Win32;

namespace SwiftGrab.Services;

/// <summary>
/// Registers/unregisters the <c>swiftgrab://</c> custom URL protocol in the Windows registry.
/// </summary>
public static class ProtocolHandler
{
    private const string Protocol = "swiftgrab";

    public static void Register(string exePath)
    {
        using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{Protocol}");
        key.SetValue("", $"URL:{Protocol} Protocol");
        key.SetValue("URL Protocol", "");

        using var iconKey = key.CreateSubKey("DefaultIcon");
        iconKey.SetValue("", $"\"{exePath}\",1");

        using var cmdKey = key.CreateSubKey(@"shell\open\command");
        cmdKey.SetValue("", $"\"{exePath}\" \"%1\"");
    }

    public static void Unregister()
    {
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{Protocol}", throwOnMissingSubKey: false);
    }

    /// <summary>
    /// Parses a <c>swiftgrab://download?url=...</c> URI and returns the embedded URL.
    /// </summary>
    public static string? ParseDownloadUri(string uri)
    {
        if (!uri.StartsWith($"{Protocol}://", StringComparison.OrdinalIgnoreCase))
            return null;

        var parsed = new Uri(uri);
        // Parse query string without System.Web dependency
        foreach (var pair in parsed.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');
            if (idx < 0) continue;
            string key = Uri.UnescapeDataString(pair[..idx]);
            string value = Uri.UnescapeDataString(pair[(idx + 1)..]);
            if (key.Equals("url", StringComparison.OrdinalIgnoreCase))
                return value;
        }
        return null;
    }
}
