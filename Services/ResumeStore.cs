using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SwiftGrab.Models;

namespace SwiftGrab.Services;

/// <summary>
/// Persists download state to disk so downloads can be resumed after restart.
/// </summary>
public class ResumeStore
{
    private readonly string _storePath;

    public ResumeStore(string? storePath = null)
    {
        _storePath = storePath
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SwiftGrab",
                "resume.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_storePath)!);
    }

    public async Task SaveAsync(IEnumerable<DownloadItem> items)
    {
        var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_storePath, json);
    }

    public async Task<List<DownloadItem>> LoadAsync()
    {
        if (!File.Exists(_storePath))
            return new List<DownloadItem>();

        try
        {
            var json = await File.ReadAllTextAsync(_storePath);
            return JsonSerializer.Deserialize<List<DownloadItem>>(json)
                   ?? new List<DownloadItem>();
        }
        catch
        {
            return new List<DownloadItem>();
        }
    }

    public void Delete()
    {
        if (File.Exists(_storePath))
            File.Delete(_storePath);
    }
}
