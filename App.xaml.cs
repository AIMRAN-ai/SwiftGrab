using System;
using System.Linq;
using System.Windows;
using SwiftGrab.Services;

namespace SwiftGrab;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Run as native messaging host if invoked by a browser extension
        if (e.Args.Contains("--native-host", StringComparer.OrdinalIgnoreCase))
        {
            RunNativeHost();
            Shutdown();
            return;
        }

        // Register the swiftgrab:// protocol
        try
        {
            string? exePath = Environment.ProcessPath;
            if (exePath != null)
                ProtocolHandler.Register(exePath);
        }
        catch { /* non-critical */ }

        // Handle swiftgrab:// invocation
        if (e.Args.Length > 0 && e.Args[0].StartsWith("swiftgrab://", StringComparison.OrdinalIgnoreCase))
        {
            string? url = ProtocolHandler.ParseDownloadUri(e.Args[0]);
            if (url != null)
                SetStartupUrl(url);
        }
    }

    private static void RunNativeHost()
    {
        var host = new NativeMessagingHost();
        host.MessageReceived += (_, msg) =>
        {
            // Echo back an ack — real implementation would enqueue a download
            NativeMessagingHost.SendMessage(new { status = "ok", received = msg });
        };
        host.RunAsync().GetAwaiter().GetResult();
    }

    private void SetStartupUrl(string url)
    {
        // Stored in a property for the MainWindow to pick up after it opens
        Properties["StartupUrl"] = url;
    }
}
