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
        long downloaded = 0;
        byte[] buffer = new byte[16384];
        DateTime lastLog = DateTime.Now;
        foreach ((string, string, long) download in Downloads) {
            logger.LogInformation("Downloading {Url} to {Path} ({Size} MiB)...", download.Item1, download.Item2, download.Item3 / (1024 * 1024));
            using HttpResponseMessage response = await httpClient.GetAsync(download.Item1, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            using Stream input = await response.Content.ReadAsStreamAsync(cancellationToken);
            using FileStream output = File.OpenWrite(download.Item2);
            while (true) {
                int count = await input.ReadAsync(buffer, cancellationToken);
                if (count <= 0) {
                    break;
                }
                await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                downloaded += count;
                progress.Report(downloaded * 100.0 / TotalSize);
                if (DateTime.Now - lastLog > TimeSpan.FromSeconds(10)) {
                    lastLog += TimeSpan.FromSeconds(10);
                    logger.LogDebug("Downloaded {Done} / {Total} MiB", downloaded / (1024 * 1024), TotalSize / (1024 * 1024));
                }
            }
        }
    }
}
