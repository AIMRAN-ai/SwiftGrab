using System.Collections.Generic;
using System.Linq;
using SwiftGrab.Models;

namespace SwiftGrab.Services;

/// <summary>
/// Dynamically rebalances segments: splits large slow segments and merges tiny ones.
/// </summary>
public class DynamicRebalancer
{
    private const long MinSegmentSize = 1024 * 512; // 512 KB minimum
    private const double SlowThresholdRatio = 0.3;  // 30% of average speed = "slow"

    /// <summary>
    /// Returns a set of (segmentIndex, newEndByte) splits to apply.
    /// The caller is responsible for creating new segments from the split point.
    /// </summary>
    public IEnumerable<(int SegmentIndex, long SplitPoint)> ComputeSplits(
        IList<DownloadSegment> segments,
        SegmentPerformanceMonitor monitor)
    {
        var active = segments
            .Where(s => s.Status == DownloadStatus.Downloading && s.RemainingBytes > MinSegmentSize * 2)
            .ToList();

        if (active.Count < 2) yield break;

        double avgSpeed = active.Average(s => monitor.GetSpeed(s.Index));
        if (avgSpeed <= 0) yield break;

        foreach (var seg in active)
        {
            double speed = monitor.GetSpeed(seg.Index);
            if (speed < avgSpeed * SlowThresholdRatio && seg.RemainingBytes > MinSegmentSize * 2)
            {
                // Split the remaining portion in half
                long splitPoint = seg.StartByte + seg.DownloadedBytes + seg.RemainingBytes / 2;
                yield return (seg.Index, splitPoint);
            }
        }
    }
}
