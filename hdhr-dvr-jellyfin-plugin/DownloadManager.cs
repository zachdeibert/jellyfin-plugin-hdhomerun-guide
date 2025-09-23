namespace Com.ZachDeibert.MediaTools.Hdhr.Dvr.Jellyfin;

using Com.ZachDeibert.MediaTools.Hdhr.Api;
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

    private readonly List<(Recording, string, long)> Downloads = [];
    private long TotalSize = 0;

    public async Task Add(Recording recording, string path, CancellationToken cancellationToken = default) {
        long size = await recording.GetFileSize(httpClient, logger, cancellationToken);
        Downloads.Add((recording, path, size));
        TotalSize += size;
    }

    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken = default) {
        long previousDownloaded = 0;
        foreach ((Recording, string, long) download in Downloads) {
            ProgressHelper helper = new() {
                Parent = progress,
                PreviousProgress = previousDownloaded,
                TotalProgress = TotalSize,
            };
            await download.Item1.Download(download.Item2, httpClient, helper, logger, cancellationToken);
            previousDownloaded += helper.Progress;
        }
    }
}
