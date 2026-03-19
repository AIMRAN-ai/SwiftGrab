using System.ComponentModel;
using System.Runtime.CompilerServices;
using SwiftGrab.Models;

namespace SwiftGrab.ViewModels;

/// <summary>
/// ViewModel wrapping a single <see cref="DownloadItem"/> for WPF binding.
/// </summary>
public class DownloadViewModel : INotifyPropertyChanged
{
    public DownloadItem Item { get; }

    public DownloadViewModel(DownloadItem item) => Item = item;

    public string DisplayName => string.IsNullOrEmpty(Item.FileName) ? Item.Url : Item.FileName;

    public string StatusText => Item.Status.ToString();

    public string SpeedText => Item.SpeedBytesPerSec switch
    {
        >= 1_000_000 => $"{Item.SpeedBytesPerSec / 1_000_000:F1} MB/s",
        >= 1_000 => $"{Item.SpeedBytesPerSec / 1_000:F0} KB/s",
        _ => $"{Item.SpeedBytesPerSec:F0} B/s"
    };

    public string ProgressText => $"{Item.ProgressPercent:F1}%";

    public string EtaText
    {
        get
        {
            var eta = Item.EstimatedTimeRemaining;
            if (eta == System.TimeSpan.Zero) return "--";
            if (eta.TotalHours >= 1) return $"{eta.Hours}h {eta.Minutes}m";
            if (eta.TotalMinutes >= 1) return $"{eta.Minutes}m {eta.Seconds}s";
            return $"{eta.Seconds}s";
        }
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(SpeedText));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(EtaText));
        OnPropertyChanged(nameof(DisplayName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
