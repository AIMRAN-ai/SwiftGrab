using System;
using System.Collections.ObjectModel;

namespace SwiftGrab.Models;

public enum DownloadStatus
{
    Queued,
    Downloading,
    Paused,
    Completed,
    Error,
    Cancelled
}

public enum DownloadType
{
    Http,
    Torrent,
    Video
}

public class DownloadItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string SavePath { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public long DownloadedBytes { get; set; }
    public double SpeedBytesPerSec { get; set; }
    public DownloadStatus Status { get; set; } = DownloadStatus.Queued;
    public DownloadType Type { get; set; } = DownloadType.Http;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int SegmentCount { get; set; } = 8;
    public string? ErrorMessage { get; set; }
    public ObservableCollection<DownloadSegment> Segments { get; set; } = new();

    public double ProgressPercent =>
        TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes * 100 : 0;

    public TimeSpan EstimatedTimeRemaining =>
        SpeedBytesPerSec > 0
            ? TimeSpan.FromSeconds((TotalBytes - DownloadedBytes) / SpeedBytesPerSec)
            : TimeSpan.Zero;
}
