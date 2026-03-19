using System;
using System.Collections.Concurrent;
using SwiftGrab.Models;

namespace SwiftGrab.Services;

/// <summary>
/// Tracks per-segment download speeds using a sliding-window EMA.
/// </summary>
public class SegmentPerformanceMonitor
{
    private readonly ConcurrentDictionary<int, double> _speeds = new();
    private const double Alpha = 0.3; // EMA smoothing factor

    public void RecordBytes(int segmentIndex, long bytes, TimeSpan elapsed)
    {
        if (elapsed.TotalSeconds <= 0) return;
        double instantSpeed = bytes / elapsed.TotalSeconds;
        _speeds.AddOrUpdate(segmentIndex,
            instantSpeed,
            (_, prev) => Alpha * instantSpeed + (1 - Alpha) * prev);
    }

    public double GetSpeed(int segmentIndex) =>
        _speeds.TryGetValue(segmentIndex, out var s) ? s : 0;

    public double GetTotalSpeed()
    {
        double total = 0;
        foreach (var s in _speeds.Values) total += s;
        return total;
    }

    public void UpdateDownloadItem(DownloadItem item)
    {
        item.SpeedBytesPerSec = GetTotalSpeed();
        foreach (var seg in item.Segments)
            seg.SpeedBytesPerSec = GetSpeed(seg.Index);
    }

    public void Reset(int segmentIndex) => _speeds.TryRemove(segmentIndex, out _);
    public void ResetAll() => _speeds.Clear();
}
