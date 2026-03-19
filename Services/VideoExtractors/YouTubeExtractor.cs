using System;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;
using SwiftGrab.Models;

namespace SwiftGrab.Services.VideoExtractors;

/// <summary>
/// Extracts video info from YouTube using YoutubeExplode.
/// </summary>
public class YouTubeExtractor : VideoExtractorBase
{
    private readonly YoutubeClient _youtube = new();

    public override bool CanHandle(string url) =>
        url.Contains("youtube.com/watch", StringComparison.OrdinalIgnoreCase)
        || url.Contains("youtu.be/", StringComparison.OrdinalIgnoreCase);

    public override async Task<VideoInfo> FetchInfoAsync(string url, CancellationToken ct = default)
    {
        var video = await _youtube.Videos.GetAsync(url, ct);
        var manifest = await _youtube.Videos.Streams.GetManifestAsync(video.Id, ct);

        var info = new VideoInfo
        {
            OriginalUrl = url,
            Title = video.Title,
            Author = video.Author.ChannelTitle,
            Description = video.Description ?? string.Empty,
            ThumbnailUrl = video.Thumbnails.GetWithHighestResolution()?.Url ?? string.Empty,
            DurationSeconds = (long)(video.Duration?.TotalSeconds ?? 0),
            Platform = "YouTube"
        };

        foreach (var stream in manifest.GetVideoOnlyStreams())
        {
            info.Formats.Add(new VideoFormat
            {
                FormatId = stream.Url,
                Extension = stream.Container.Name,
                Quality = stream.VideoQuality.Label,
                FileSizeBytes = stream.Size.Bytes,
                VideoCodec = stream.VideoCodec,
                Width = stream.VideoResolution.Width,
                Height = stream.VideoResolution.Height,
                Fps = stream.VideoQuality.Framerate,
                DirectUrl = stream.Url
            });
        }

        foreach (var stream in manifest.GetAudioOnlyStreams())
        {
            info.Formats.Add(new VideoFormat
            {
                FormatId = stream.Url,
                Extension = stream.Container.Name,
                Quality = "audio",
                FileSizeBytes = stream.Size.Bytes,
                AudioCodec = stream.AudioCodec,
                Bitrate = (int)stream.Bitrate.BitsPerSecond,
                DirectUrl = stream.Url
            });
        }

        return info;
    }
}
