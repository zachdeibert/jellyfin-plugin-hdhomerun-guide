namespace Com.ZachDeibert.JellyfinPluginHDHomeRunDVR;

using Microsoft.Extensions.Logging;

public class DownloadManager(HttpClient httpClient, ILogger<DownloadManager> logger) {
    private readonly List<(string, string, long)> Downloads = [];
    private long TotalSize = 0;

    public async Task Add(string url, string path, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        long size = response.EnsureSuccessStatusCode().Content.Headers.ContentLength ?? 0;
        Downloads.Add((url, path, size));
        TotalSize += size;
    }

    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken) {
        foreach ((string, string, long) download in Downloads) {
            logger.LogInformation("Downloading {Url} to {Path} ({Size} MiB)...", download.Item1, download.Item2, download.Item3 / (1024 * 1024));
            await Task.Delay(100, cancellationToken);
        }
        progress.Report(100);
    }
}
