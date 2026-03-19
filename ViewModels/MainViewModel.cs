using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using SwiftGrab.Helpers;
using SwiftGrab.Models;
using SwiftGrab.Services;
using SwiftGrab.Services.VideoExtractors;

namespace SwiftGrab.ViewModels;

public class MainViewModel : System.ComponentModel.INotifyPropertyChanged
{
    // ── Services ────────────────────────────────────────────────────────────
    private readonly SegmentedDownloadService _httpService;
    private readonly TorrentDownloadService _torrentService;
    private readonly ResumeStore _resumeStore;

    // ── UI State ─────────────────────────────────────────────────────────────
    private string _newUrl = string.Empty;
    private string _savePath;
    private DownloadViewModel? _selectedDownload;
    private string _statusMessage = "Ready";

    public ObservableCollection<DownloadViewModel> Downloads { get; } = new();

    public string NewUrl
    {
        get => _newUrl;
        set { _newUrl = value; OnPropertyChanged(); AddCommand.RaiseCanExecuteChanged(); }
    }

    public string SavePath
    {
        get => _savePath;
        set { _savePath = value; OnPropertyChanged(); }
    }

    public DownloadViewModel? SelectedDownload
    {
        get => _selectedDownload;
        set { _selectedDownload = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    // ── Commands ─────────────────────────────────────────────────────────────
    public RelayCommand AddCommand { get; }
    public RelayCommand PauseCommand { get; }
    public RelayCommand ResumeCommand { get; }
    public RelayCommand RemoveCommand { get; }
    public RelayCommand BrowseSavePathCommand { get; }

    public MainViewModel()
    {
        _savePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads", "SwiftGrab");

        _resumeStore = new ResumeStore();
        _httpService = new SegmentedDownloadService(_resumeStore);
        _torrentService = new TorrentDownloadService(_savePath);

        // Register extractors
        VideoExtractorBase.Register(new YouTubeExtractor());

        AddCommand = new RelayCommand(
            _ => _ = AddDownloadAsync(),
            _ => !string.IsNullOrWhiteSpace(NewUrl));

        PauseCommand = new RelayCommand(
            _ => PauseSelected(),
            _ => SelectedDownload?.Item.Status == DownloadStatus.Downloading);

        ResumeCommand = new RelayCommand(
            _ => _ = ResumeSelectedAsync(),
            _ => SelectedDownload?.Item.Status == DownloadStatus.Paused);

        RemoveCommand = new RelayCommand(
            _ => RemoveSelected(),
            _ => SelectedDownload != null);

        BrowseSavePathCommand = new RelayCommand(_ => BrowseSavePath());

        // Restore prior session
        _ = RestoreSessionAsync();
    }

    // ── Add ──────────────────────────────────────────────────────────────────
    private async Task AddDownloadAsync()
    {
        string url = NewUrl.Trim();
        if (string.IsNullOrEmpty(url)) return;
        NewUrl = string.Empty;

        if (url.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase)
            || url.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase))
        {
            await AddTorrentAsync(url);
            return;
        }

        // Check if it's a video URL
        var extractor = VideoExtractorBase.GetExtractor(url);
        if (extractor != null)
        {
            await AddVideoDownloadAsync(url, extractor);
            return;
        }

        await AddHttpDownloadAsync(url);
    }

    private async Task AddHttpDownloadAsync(string url)
    {
        var item = new DownloadItem
        {
            Url = url,
            FileName = GetFileNameFromUrl(url),
            SavePath = SavePath,
            SegmentCount = 8
        };

        var vm = new DownloadViewModel(item);
        Downloads.Add(vm);
        StatusMessage = $"Starting: {item.FileName}";

        try
        {
            await _httpService.StartAsync(item);
            StatusMessage = $"Completed: {item.FileName}";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Paused.";
        }
        catch (Exception ex)
        {
            item.Status = DownloadStatus.Error;
            item.ErrorMessage = ex.Message;
            StatusMessage = $"Error: {ex.Message}";
        }

        vm.Refresh();
        await _resumeStore.SaveAsync(Downloads.Select(d => d.Item));
    }

    private async Task AddTorrentAsync(string magnetOrPath)
    {
        var info = new TorrentInfo
        {
            MagnetOrPath = magnetOrPath,
            SavePath = SavePath,
            Name = magnetOrPath.Length > 40 ? magnetOrPath[..40] + "…" : magnetOrPath
        };

        StatusMessage = $"Adding torrent: {info.Name}";
        try
        {
            await _torrentService.AddTorrentAsync(info);
            await _torrentService.StartAsync(info);
            StatusMessage = $"Torrent started: {info.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Torrent error: {ex.Message}";
        }
    }

    private async Task AddVideoDownloadAsync(string url, VideoExtractorBase extractor)
    {
        StatusMessage = "Fetching video info…";
        try
        {
            var videoInfo = await extractor.FetchInfoAsync(url);
            var best = videoInfo.Formats
                .OrderByDescending(f => f.Height)
                .FirstOrDefault(f => !string.IsNullOrEmpty(f.DirectUrl));

            if (best == null) { StatusMessage = "No downloadable format found."; return; }

            var item = new DownloadItem
            {
                Url = best.DirectUrl,
                FileName = $"{SanitizeFileName(videoInfo.Title)}.{best.Extension}",
                SavePath = SavePath,
                Type = DownloadType.Video,
                SegmentCount = 4
            };

            var vm = new DownloadViewModel(item);
            Downloads.Add(vm);
            StatusMessage = $"Downloading: {videoInfo.Title}";

            await _httpService.StartAsync(item);
            vm.Refresh();
            StatusMessage = $"Completed: {videoInfo.Title}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Video error: {ex.Message}";
        }
    }

    // ── Pause / Resume ───────────────────────────────────────────────────────
    private void PauseSelected()
    {
        if (SelectedDownload != null)
        {
            SelectedDownload.Item.Status = DownloadStatus.Paused;
            SelectedDownload.Refresh();
        }
    }

    private Task ResumeSelectedAsync()
    {
        if (SelectedDownload == null) return Task.CompletedTask;
        var item = SelectedDownload.Item;
        item.Status = DownloadStatus.Queued;
        SelectedDownload.Refresh();
        return _httpService.StartAsync(item);
    }

    // ── Remove ───────────────────────────────────────────────────────────────
    private void RemoveSelected()
    {
        if (SelectedDownload != null)
        {
            Downloads.Remove(SelectedDownload);
            SelectedDownload = null;
        }
    }

    // ── Browse ───────────────────────────────────────────────────────────────
    private void BrowseSavePath()
    {
        // Use WinForms FolderBrowserDialog for broad SDK compatibility
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select Save Folder",
            UseDescriptionForTitle = true,
            SelectedPath = SavePath
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            SavePath = dialog.SelectedPath;
    }

    // ── Session restore ──────────────────────────────────────────────────────
    private async Task RestoreSessionAsync()
    {
        var saved = await _resumeStore.LoadAsync();
        foreach (var item in saved)
        {
            if (item.Status == DownloadStatus.Completed) continue;
            item.Status = DownloadStatus.Paused;
            Downloads.Add(new DownloadViewModel(item));
        }

        await _torrentService.RestoreAsync();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static string GetFileNameFromUrl(string url)
    {
        try
        {
            string path = new Uri(url).LocalPath;
            string name = Path.GetFileName(path);
            return string.IsNullOrEmpty(name) ? "download" : name;
        }
        catch
        {
            return "download";
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}
