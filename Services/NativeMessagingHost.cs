using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftGrab.Services;

/// <summary>
/// Implements the Chrome/Firefox native messaging protocol.
/// Reads 4-byte length-prefixed JSON messages from stdin and writes responses to stdout.
/// </summary>
public class NativeMessagingHost
{
    public event EventHandler<string>? MessageReceived;

    public async Task RunAsync(CancellationToken ct = default)
    {
        using var stdin = Console.OpenStandardInput();
        while (!ct.IsCancellationRequested)
        {
            // Read 4-byte little-endian message length
            var lenBuf = new byte[4];
            int read = await stdin.ReadAsync(lenBuf.AsMemory(0, 4), ct);
            if (read < 4) break;

            int msgLen = BitConverter.ToInt32(lenBuf, 0);
            if (msgLen <= 0 || msgLen > 1024 * 1024) break;

            var msgBuf = new byte[msgLen];
            int totalRead = 0;
            while (totalRead < msgLen)
            {
                int r = await stdin.ReadAsync(msgBuf.AsMemory(totalRead, msgLen - totalRead), ct);
                if (r == 0) return;
                totalRead += r;
            }

            string message = Encoding.UTF8.GetString(msgBuf);
            MessageReceived?.Invoke(this, message);
        }
    }

    public static void SendMessage(object payload)
    {
        string json = JsonSerializer.Serialize(payload);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        byte[] lenBytes = BitConverter.GetBytes(bytes.Length);

        using var stdout = Console.OpenStandardOutput();
        stdout.Write(lenBytes, 0, 4);
        stdout.Write(bytes, 0, bytes.Length);
        stdout.Flush();
    }
}
