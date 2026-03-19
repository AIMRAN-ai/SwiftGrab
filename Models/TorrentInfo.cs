using System;
using System.Collections.Generic;

namespace SwiftGrab.Models;

public class TorrentInfo
{
    public string MagnetOrPath { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public long DownloadedBytes { get; set; }
    public double SpeedBytesPerSec { get; set; }
    public int Seeders { get; set; }
    public int Leechers { get; set; }
    public List<string> Files { get; set; } = new();
    public string SavePath { get; set; } = string.Empty;
    public TorrentStatus Status { get; set; } = TorrentStatus.Queued;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public double ProgressPercent =>
        TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes * 100 : 0;
}

public enum TorrentStatus
{
    Queued,
    Downloading,
    Seeding,
    Paused,
    Completed,
    Error
}
