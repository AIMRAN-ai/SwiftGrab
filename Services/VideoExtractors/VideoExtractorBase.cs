using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SwiftGrab.Models;

namespace SwiftGrab.Services.VideoExtractors;

/// <summary>
/// Base class for platform-specific video extractors.
/// Derive and override <see cref="FetchInfoAsync"/> to add new platforms.
/// </summary>
public abstract class VideoExtractorBase
{
    /// <summary>Returns true if this extractor can handle the given URL.</summary>
    public abstract bool CanHandle(string url);

    /// <summary>Fetches video metadata and available formats for the given URL.</summary>
    public abstract Task<VideoInfo> FetchInfoAsync(string url, CancellationToken ct = default);

    /// <summary>
    /// Registry of all known extractors.  Call <see cref="GetExtractor"/> to find one.
    /// </summary>
    private static readonly List<VideoExtractorBase> _registry = new();

    public static void Register(VideoExtractorBase extractor) => _registry.Add(extractor);

    public static VideoExtractorBase? GetExtractor(string url)
    {
        foreach (var e in _registry)
            if (e.CanHandle(url)) return e;
        return null;
    }
}
