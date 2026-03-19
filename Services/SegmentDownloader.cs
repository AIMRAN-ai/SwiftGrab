using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SwiftGrab.Helpers;
using SwiftGrab.Models;

namespace SwiftGrab.Services;

/// <summary>
/// Downloads a single byte-range segment and writes it to the target file.
/// </summary>
public class SegmentDownloader
{
    private const int BufferSize = 81920; // 80 KiB

    public async Task DownloadSegmentAsync(
        string url,
        string filePath,
        DownloadSegment segment,
        IProgress<long>? progress = null,
        CancellationToken ct = default)
    {
        segment.Status = DownloadStatus.Downloading;

        try
        {
            long resumeFrom = segment.StartByte + segment.DownloadedBytes;
            using var resp = await HttpHelper.GetRangeAsync(url, resumeFrom, segment.EndByte, ct);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            await using var fileStream = new FileStream(
                filePath,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.ReadWrite,
                BufferSize,
                useAsync: true);

            fileStream.Seek(resumeFrom, SeekOrigin.Begin);

            var buffer = new byte[BufferSize];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                segment.DownloadedBytes += bytesRead;
                progress?.Report(bytesRead);
            }

            segment.Status = DownloadStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            segment.Status = DownloadStatus.Paused;
            throw;
        }
        catch (Exception ex)
        {
            segment.Status = DownloadStatus.Error;
            throw new InvalidOperationException($"Segment {segment.Index} failed: {ex.Message}", ex);
        }
    }
}
