namespace SwiftGrab.Models;

public class DownloadSegment
{
    public int Index { get; set; }
    public long StartByte { get; set; }
    public long EndByte { get; set; }
    public long DownloadedBytes { get; set; }
    public DownloadStatus Status { get; set; } = DownloadStatus.Queued;
    public double SpeedBytesPerSec { get; set; }

    public long Length => EndByte - StartByte + 1;
    public long RemainingBytes => Length - DownloadedBytes;
    public double ProgressPercent => Length > 0 ? (double)DownloadedBytes / Length * 100 : 0;
}
