namespace Com.ZachDeibert.MediaTools.Hdhr.Dvr.Jellyfin;

using Microsoft.Extensions.Logging;

public class DownloadManager(HttpClient httpClient, ILogger<DownloadJob>? logger = null) {
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

    private readonly List<DownloadJob> Downloads = [];
    private long TotalSize = 0;

    public async Task Add(DownloadJob job, CancellationToken cancellationToken = default) {
        TotalSize += await job.GetFileSize(httpClient, logger, cancellationToken);
        Downloads.Add(job);
    }

    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken = default) {
        long previousDownloaded = 0;
        foreach (DownloadJob job in Downloads) {
            ProgressHelper helper = new() {
                Parent = progress,
                PreviousProgress = previousDownloaded,
                TotalProgress = TotalSize,
            };
            await job.Download(httpClient, helper, logger, cancellationToken);
            previousDownloaded += helper.Progress;
        }
    }
}
