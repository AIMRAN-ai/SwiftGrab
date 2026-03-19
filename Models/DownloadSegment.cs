namespace SwiftGrab.Models;

public class DownloadSegment
{
    public int Index { get; set; }
    public long StartByte { get; set; }
    public long EndByte { get; set; }
    public long DownloadedBytes { get; set; }
    public DownloadStatus Status { get; set; } = DownloadStatus.Queued;
    public double SpeedBytesPerSec { get; set; }

    /// <summary>Returns -1 for streaming segments with unknown length (EndByte == long.MaxValue).</summary>
    public long Length => EndByte == long.MaxValue ? -1 : EndByte - StartByte + 1;
    public long RemainingBytes => Length < 0 ? -1 : Length - DownloadedBytes;
    public double ProgressPercent => Length > 0 ? (double)DownloadedBytes / Length * 100 : 0;
}
