# SwiftGrab

A Windows desktop **download manager** built with **WPF (.NET 8)** that brings together:

- **Segmented HTTP/HTTPS downloads** (multi-part, up to 8 parallel segments)
- **BitTorrent downloads** (via `MonoTorrent`)
- **Video extraction/download pipeline** (YouTube via `YoutubeExplode` + `FFmpeg` helpers)
- **Browser integration** (Chrome/Firefox extension + native messaging + custom `swiftgrab://` protocol)

> SwiftGrab focuses on fast, resumable downloads with a clean, dark MVVM UI powered by MahApps.Metro and MaterialDesign.

---

## Features

### Download Manager (HTTP/HTTPS)
| Feature | Details |
|---|---|
| Multi-segment | Up to 8 parallel byte-range segments |
| Resume | Session state persisted via `ResumeStore` (JSON in `%APPDATA%\SwiftGrab`) |
| Speed monitoring | Per-segment EMA speed tracking (`SegmentPerformanceMonitor`) |
| Dynamic rebalancing | Slow segments are split; stolen by faster workers (`DynamicRebalancer`) |
| Work stealing | Custom `WorkStealingScheduler` keeps all CPU cores busy |

### Video Support
| Feature | Details |
|---|---|
| YouTube | Full format list via `YoutubeExplode` (`YouTubeExtractor`) |
| Extensible | Drop in a new `VideoExtractorBase` subclass to add more platforms |
| FFmpeg | Merge video+audio streams, transcode, probe (`FFmpegHelper`) |

### Torrent Support
| Feature | Details |
|---|---|
| Engine | MonoTorrent 3.x |
| Magnet links | Yes |
| `.torrent` files | Yes |
| Session restore | `TorrentDownloadService.RestoreAsync()` on startup |

### Browser Integration
| Feature | Details |
|---|---|
| Extension | Manifest V3 extension (`BrowserExtension/`) – adds context-menu + page buttons |
| Native host | `NativeMessagingHost` reads length-prefixed JSON from `stdin` |
| Protocol | `swiftgrab://download?url=...` custom URL scheme (auto-registered on startup) |
| Mode | Start app with `--native-host` flag to run in headless native-messaging mode |

---

## Architecture

```
SwiftGrab/
├── App.xaml / App.xaml.cs          # Entry point, protocol & native-host routing
├── Models/                          # POCOs: DownloadItem, DownloadSegment, TorrentInfo, VideoInfo
├── Helpers/                         # RelayCommand, HttpHelper
├── Services/
│   ├── ResumeStore.cs               # JSON session persistence
│   ├── SegmentDownloader.cs         # Single segment HTTP downloader
│   ├── SegmentPerformanceMonitor.cs # EMA speed tracker
│   ├── DynamicRebalancer.cs         # Splits slow segments
│   ├── WorkStealingScheduler.cs     # Thread pool scheduler
│   ├── SegmentedDownloadService.cs  # Orchestrator
│   ├── TorrentDownloadService.cs    # MonoTorrent wrapper
│   ├── FFmpegHelper.cs              # FFmpeg CLI wrapper
│   ├── NativeMessagingHost.cs       # Browser extension host
│   ├── ProtocolHandler.cs           # swiftgrab:// registry handler
│   └── VideoExtractors/
│       ├── VideoExtractorBase.cs    # Abstract extractor + registry
│       └── YouTubeExtractor.cs      # YouTube via YoutubeExplode
├── ViewModels/
│   ├── MainViewModel.cs             # Queue, commands, session
│   └── DownloadViewModel.cs         # Per-download UI wrapper
├── Views/
│   └── MainWindow.xaml              # MahApps + MaterialDesign dark UI
└── BrowserExtension/
    ├── manifest.json                # Chrome Manifest V3
    ├── background.js                # Service worker + native messaging
    ├── content.js                   # Page injection
    └── native-host/
        └── com.swiftgrab.host.json  # Chrome native messaging manifest
```

## Requirements

- **Windows 10/11**
- **.NET 8 SDK** (for building)
- **`ffmpeg.exe`** on PATH (only needed for video merge/transcode features)

## Build

```powershell
dotnet restore
dotnet build -c Release
dotnet run
```

## Browser Extension Setup

1. Open `chrome://extensions` → Enable *Developer mode*
2. Click *Load unpacked* → select the `BrowserExtension/` folder
3. Note your extension ID; update the placeholder `YOUR_EXTENSION_ID` in `BrowserExtension/native-host/com.swiftgrab.host.json`
4. Also update the `path` in that file to point to your actual `SwiftGrab.exe` installation path
5. Copy `com.swiftgrab.host.json` to `%LOCALAPPDATA%\Google\Chrome\User Data\NativeMessagingHosts\`
6. Right-click any download link → **Download with SwiftGrab** ⚡

---

## NuGet Packages

| Package | Purpose |
|---|---|
| `CommunityToolkit.Mvvm` | Source-gen MVVM helpers |
| `MahApps.Metro` | Modern WPF window chrome |
| `MaterialDesignThemes` | Material Design controls |
| `MonoTorrent` | BitTorrent engine |
| `YoutubeExplode` | YouTube stream extraction |
