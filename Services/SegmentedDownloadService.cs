using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SwiftGrab.Helpers;
using SwiftGrab.Models;

namespace SwiftGrab.Services;

/// <summary>
/// Orchestrates a multi-segment download: allocates segments, runs them in parallel,
/// monitors performance and dynamically rebalances.
/// </summary>
public class SegmentedDownloadService
{
    private readonly ResumeStore _resumeStore;
    private readonly SegmentPerformanceMonitor _monitor = new();
    private readonly DynamicRebalancer _rebalancer = new();
    private readonly SegmentDownloader _downloader = new();

    public SegmentedDownloadService(ResumeStore? resumeStore = null)
    {
        _resumeStore = resumeStore ?? new ResumeStore();
    }

    public async Task StartAsync(DownloadItem item, CancellationToken ct = default)
    {
        item.Status = DownloadStatus.Downloading;

        // Step 1: probe the URL
        var (supportsRanges, contentLength) = await HttpHelper.GetHeaderInfoAsync(item.Url, ct);
        item.TotalBytes = contentLength;

        // Step 2: allocate segments (or restore from resume)
        if (item.Segments.Count == 0)
            AllocateSegments(item, supportsRanges ? item.SegmentCount : 1);

        // Ensure the output file exists with the right size
        string filePath = Path.Combine(item.SavePath, item.FileName);
        Directory.CreateDirectory(item.SavePath);
        if (contentLength > 0)
            await PreAllocateFileAsync(filePath, contentLength);

        // Step 3: download segments in parallel
        var tasks = item.Segments
            .Where(s => s.Status != DownloadStatus.Completed)
            .Select(seg => DownloadSegmentWithMonitoring(item, seg, filePath, ct))
            .ToList();

        await Task.WhenAll(tasks);

        if (item.Segments.All(s => s.Status == DownloadStatus.Completed))
        {
            item.Status = DownloadStatus.Completed;
            item.CompletedAt = DateTime.UtcNow;
            item.DownloadedBytes = item.TotalBytes;
        }
        else if (!ct.IsCancellationRequested)
        {
            item.Status = DownloadStatus.Error;
        }
    }

    private async Task DownloadSegmentWithMonitoring(
        DownloadItem item, DownloadSegment seg, string filePath, CancellationToken ct)
    {
        var progress = new Progress<long>(bytes =>
        {
            item.DownloadedBytes += bytes;
            _monitor.RecordBytes(seg.Index, bytes, TimeSpan.FromMilliseconds(100));
            _monitor.UpdateDownloadItem(item);
        });

        await _downloader.DownloadSegmentAsync(item.Url, filePath, seg, progress, ct);
    }

    private static void AllocateSegments(DownloadItem item, int count)
    {
        item.Segments.Clear();
        if (item.TotalBytes <= 0)
        {
            item.Segments.Add(new DownloadSegment { Index = 0, StartByte = 0, EndByte = long.MaxValue });
            return;
        }

        long segSize = item.TotalBytes / count;
        for (int i = 0; i < count; i++)
        {
            long start = i * segSize;
            long end = (i == count - 1) ? item.TotalBytes - 1 : start + segSize - 1;
            item.Segments.Add(new DownloadSegment { Index = i, StartByte = start, EndByte = end });
        }
    }

    private static async Task PreAllocateFileAsync(string filePath, long size)
    {
        if (File.Exists(filePath) && new FileInfo(filePath).Length == size) return;
        await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        fs.SetLength(size);
    }
}
