using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftGrab.Services;

/// <summary>
/// Wraps FFmpeg CLI calls for merging video/audio streams.
/// Requires ffmpeg.exe to be on PATH.
/// </summary>
public static class FFmpegHelper
{
    public static async Task MergeVideoAudioAsync(
        string videoPath,
        string audioPath,
        string outputPath,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        string args = $"-y -i \"{videoPath}\" -i \"{audioPath}\" -c copy \"{outputPath}\"";
        await RunAsync("ffmpeg", args, progress, ct);
    }

    public static async Task ConvertAsync(
        string inputPath,
        string outputPath,
        string extraArgs = "",
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        string args = $"-y -i \"{inputPath}\" {extraArgs} \"{outputPath}\"";
        await RunAsync("ffmpeg", args, progress, ct);
    }

    public static async Task<string> ProbeAsync(string filePath, CancellationToken ct = default)
    {
        string args = $"-v quiet -print_format json -show_format -show_streams \"{filePath}\"";
        return await RunWithOutputAsync("ffprobe", args, ct);
    }

    private static async Task RunAsync(
        string exe, string args, IProgress<string>? progress, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start {exe}");

        proc.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) progress?.Report(e.Data);
        };
        proc.BeginErrorReadLine();

        await proc.WaitForExitAsync(ct);

        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"{exe} exited with code {proc.ExitCode}");
    }

    private static async Task<string> RunWithOutputAsync(string exe, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start {exe}");

        string output = await proc.StandardOutput.ReadToEndAsync(ct);
        await proc.WaitForExitAsync(ct);
        return output;
    }
}
