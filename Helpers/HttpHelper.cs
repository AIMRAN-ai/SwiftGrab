using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftGrab.Helpers;

public static class HttpHelper
{
    private static readonly HttpClient _client = new(new HttpClientHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 10
    })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public static HttpClient Client => _client;

    /// <summary>Returns (supportsRanges, contentLength). contentLength may be -1 if unknown.</summary>
    public static async Task<(bool SupportsRanges, long ContentLength)> GetHeaderInfoAsync(
        string url, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Head, url);
        using var resp = await _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        bool supportsRanges = resp.Headers.AcceptRanges.Contains("bytes");
        long contentLength = resp.Content.Headers.ContentLength ?? -1;
        return (supportsRanges, contentLength);
    }

    public static async Task<HttpResponseMessage> GetRangeAsync(
        string url, long from, long to, CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(from, to);
        return await _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }
}
