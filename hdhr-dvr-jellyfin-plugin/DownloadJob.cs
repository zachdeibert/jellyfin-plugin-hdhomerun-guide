namespace Com.ZachDeibert.MediaTools.Hdhr.Dvr.Jellyfin;

using Com.ZachDeibert.MediaTools.Hdhr.Dvr.Core;
using Com.ZachDeibert.MediaTools.Hdhr.Dvr.Core.Configuration;
using Microsoft.Extensions.Logging;

public record class DownloadJob {
    public required Context Db { get; init; }
    public required Episode Episode { get; init; }

    public bool ShouldReRecord {
        get {
            return Plugin.Instance!.Configuration.ReRecordPolicy switch {
                ReRecordPolicy.Always => true,
                ReRecordPolicy.OnRecordingError => !Episode.Metadata!.RecordSuccess || !string.IsNullOrWhiteSpace(Episode.Metadata.RecordError),
                ReRecordPolicy.IfDeleted => !File.Exists(Episode.FilePath(false)),
                ReRecordPolicy.Never => false,
                _ => throw new ArgumentOutOfRangeException(nameof(Plugin.Instance.Configuration.ReRecordPolicy)),
            };
        }
    }

    public async Task Delete(DeleteReason reason, HttpClient client, ILogger<DownloadJob>? logger = null, CancellationToken cancellationToken = default) {
        logger?.LogInformation("Deleting {SeriesName} episode {EpisodeNumber}", Episode.Series!.Metadata!.Title, Episode.Metadata!.EpisodeNumber);
        Episode.DeleteReason = reason;
        Episode.ReRecordable = ShouldReRecord;
        if (!await Episode.Metadata!.Delete(Episode.ReRecordable, client, cancellationToken)) {
            Episode.DeleteReason = DeleteReason.RemoteDeleted;
        }
        _ = await Db.SaveChangesAsync(cancellationToken);
    }

    public async Task Download(HttpClient client, IProgress<long>? progress, ILogger<DownloadJob>? logger = null, CancellationToken cancellationToken = default) {
        await Episode.Metadata!.Download(Episode.FilePath()!, client, progress, logger, cancellationToken);
        Episode.DownloadInterrupted = false;
        _ = await Db.SaveChangesAsync(cancellationToken);
        if (Plugin.Instance?.Configuration.DeletePolicy == DeletePolicy.AfterDownload) {
            await Delete(DeleteReason.Downloaded, client, logger, cancellationToken);
        }
    }

    public Task<long> GetFileSize(HttpClient client, ILogger<DownloadJob>? logger = null, CancellationToken cancellationToken = default) {
        return Episode.Metadata!.GetFileSize(client, logger, cancellationToken);
    }
}
