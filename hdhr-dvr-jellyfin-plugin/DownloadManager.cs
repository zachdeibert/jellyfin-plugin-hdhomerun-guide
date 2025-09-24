namespace Com.ZachDeibert.MediaTools.Hdhr.Dvr.Jellyfin;

using Com.ZachDeibert.MediaTools.Hdhr.Dvr.Core;
using Microsoft.Extensions.Logging;

public class DownloadManager(HttpClient httpClient, ILogger<DownloadManager>? logger = null) {
    private class ProgressHelper : IProgress<long> {
        public required IProgress<double> Parent;
        public long PreviousProgress;
        public long Progress = 0;
        public long TotalProgress;

        public void Report(long value) {
            Progress = value;
            Parent.Report((PreviousProgress + Progress) * 100.0 / TotalProgress);
        }
    }

    private readonly List<(Context, Episode, string)> Downloads = [];
    private long TotalSize = 0;

    public async Task Add(Context context, Episode episode, string path, CancellationToken cancellationToken = default) {
        TotalSize += await episode.Metadata!.GetFileSize(httpClient, logger, cancellationToken);
        Downloads.Add((context, episode, path));
    }

    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken = default) {
        long previousDownloaded = 0;
        foreach ((Context, Episode, string) download in Downloads) {
            ProgressHelper helper = new() {
                Parent = progress,
                PreviousProgress = previousDownloaded,
                TotalProgress = TotalSize,
            };
            await download.Item2.Metadata!.Download(download.Item3, httpClient, helper, logger, cancellationToken);
            download.Item2.DownloadInterrupted = false;
            _ = await download.Item1.SaveChangesAsync(cancellationToken);
            previousDownloaded += helper.Progress;
        }
    }
}
