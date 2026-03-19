using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MonoTorrent;
using MonoTorrent.Client;
using SwiftGrab.Models;

namespace SwiftGrab.Services;

/// <summary>
/// Manages BitTorrent downloads using MonoTorrent.
/// </summary>
public class TorrentDownloadService : IDisposable
{
    private readonly ClientEngine _engine;
    private readonly Dictionary<TorrentInfo, TorrentManager> _managers = new();
    private readonly string _defaultSavePath;

    public TorrentDownloadService(string? savePath = null)
    {
        _defaultSavePath = savePath
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "SwiftGrab");

        var settings = new EngineSettingsBuilder
        {
            MaximumOpenFiles = 50,
            MaximumConnections = 100,
            ListenEndPoints = new System.Collections.Generic.Dictionary<string, System.Net.IPEndPoint>
            {
                ["ipv4"] = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0)
            }
        }.ToSettings();

        _engine = new ClientEngine(settings);
    }

    public async Task<TorrentManager> AddTorrentAsync(TorrentInfo info, CancellationToken ct = default)
    {
        TorrentManager manager;
        string savePath = string.IsNullOrEmpty(info.SavePath) ? _defaultSavePath : info.SavePath;

        if (info.MagnetOrPath.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
        {
            var magnet = MagnetLink.Parse(info.MagnetOrPath);
            manager = await _engine.AddAsync(magnet, savePath);
        }
        else
        {
            var torrent = await Torrent.LoadAsync(info.MagnetOrPath);
            info.Name = torrent.Name;
            info.TotalBytes = torrent.Size;
            info.Files = new List<string>();
            foreach (var file in torrent.Files)
                info.Files.Add(file.Path);
            manager = await _engine.AddAsync(torrent, savePath);
        }

        manager.TorrentStateChanged += (_, e) => OnStateChanged(info, e);
        manager.PeersFound += (_, _) => { };

        _managers[info] = manager;
        return manager;
    }

    public async Task StartAsync(TorrentInfo info)
    {
        if (_managers.TryGetValue(info, out var mgr))
        {
            await mgr.StartAsync();
            info.Status = TorrentStatus.Downloading;
        }
    }

    public async Task PauseAsync(TorrentInfo info)
    {
        if (_managers.TryGetValue(info, out var mgr))
        {
            await mgr.PauseAsync();
            info.Status = TorrentStatus.Paused;
        }
    }

    public async Task StopAsync(TorrentInfo info)
    {
        if (_managers.TryGetValue(info, out var mgr))
        {
            await mgr.StopAsync();
            info.Status = TorrentStatus.Paused;
        }
    }

    public void UpdateStats(TorrentInfo info)
    {
        if (!_managers.TryGetValue(info, out var mgr)) return;
        info.DownloadedBytes = (long)(mgr.Torrent?.Size * mgr.Progress / 100.0 ?? 0);
        info.SpeedBytesPerSec = mgr.Monitor.DownloadRate;
        info.Seeders = mgr.Peers.Seeds;
        info.Leechers = mgr.Peers.Leechs;
    }

    /// <summary>Restore active torrent sessions from the MonoTorrent fast-resume store.</summary>
    public async Task RestoreAsync()
    {
        await _engine.StartAllAsync();
    }

    private static void OnStateChanged(TorrentInfo info, TorrentStateChangedEventArgs e)
    {
        info.Status = e.NewState switch
        {
            TorrentState.Downloading => TorrentStatus.Downloading,
            TorrentState.Seeding => TorrentStatus.Seeding,
            TorrentState.Paused => TorrentStatus.Paused,
            TorrentState.Stopped => TorrentStatus.Paused,
            TorrentState.Error => TorrentStatus.Error,
            _ => info.Status
        };
    }

    public void Dispose() => _engine.Dispose();
}
