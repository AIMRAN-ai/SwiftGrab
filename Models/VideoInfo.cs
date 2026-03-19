using System.Collections.Generic;

namespace SwiftGrab.Models;

public class VideoFormat
{
    public string FormatId { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string Quality { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string VideoCodec { get; set; } = string.Empty;
    public string AudioCodec { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public double Fps { get; set; }
    public int Bitrate { get; set; }
    public string DirectUrl { get; set; } = string.Empty;
}

public class VideoInfo
{
    public string OriginalUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public long DurationSeconds { get; set; }
    public List<VideoFormat> Formats { get; set; } = new();
    public string Platform { get; set; } = string.Empty;
}
