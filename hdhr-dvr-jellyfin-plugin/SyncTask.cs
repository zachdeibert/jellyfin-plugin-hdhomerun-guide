namespace Com.ZachDeibert.MediaTools.Hdhr.Dvr.Jellyfin;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Com.ZachDeibert.MediaTools.Hdhr.Api;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

public class SyncTask(IConfigurationManager config, IHttpClientFactory httpClientFactory, ILogger<DownloadManager> downloadLogger, ILogger<SyncTask> logger) : IScheduledTask {
    public string Category => "Live TV";

    public string Description => "Downloads new recordings from connected HDHomeRun DVRs";

    public string Key => GetType().FullName!;

    public string Name => "Sync HDHomeRun DVR";

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken) {
        using HttpClient httpClient = httpClientFactory.CreateClient(NamedClient.Default);
        DownloadManager downloadManager = new(httpClient, downloadLogger);
        foreach (TunerHostInfo tunerHost in config.GetConfiguration<LiveTvOptions>("livetv").TunerHosts) {
            Tuner? tuner = await new TunerRef(tunerHost.Url).Discover(httpClient, logger, cancellationToken);
            if (tuner == null) {
                continue;
            }
            if (tuner.Storage == null) {
                logger.LogInformation("Skipping tuner {Url} which has no DVR storage", tuner.BaseUrl);
                continue;
            }
            HashSet<string> storagesScanned = [];
            foreach (RecordingStorage storage in await tuner.Storage.GetRecordings(httpClient, logger, cancellationToken)) {
                if (!storagesScanned.Add(storage.Url)) {
                    continue;
                }
                foreach (Recording recording in await storage.GetRecordings(httpClient, logger, cancellationToken)) {
                    string? dir = recording.Category switch {
                        RecordingCategory.Movie => Plugin.Instance?.Configuration.MovieRecordingPath,
                        RecordingCategory.Series => Plugin.Instance?.Configuration.SeriesRecordingPath,
                        _ => null,
                    };
                    if (dir == null) {
                        logger.LogError("Series {SeriesName} has unknown type {Category}", storage.Title, storage.Category);
                    } else if (dir == "") {
                        logger.LogInformation("Skipping download of {SeriesName} episode {EpisodeNumber} due to not being configured", storage.Title, recording.EpisodeNumber);
                    } else if (recording.RecordEndTime + TimeSpan.FromSeconds(30) > DateTimeOffset.UtcNow) {
                        logger.LogInformation("Not downloading {SeriesName} episode {EpisodeNumber} because it is still recording", storage.Title, recording.EpisodeNumber);
                    } else {
                        string seriesDir = Path.Join(dir, string.Concat(string.Format("{0} ({1})", storage.Title, storage.SeriesId).Split(Path.GetInvalidFileNameChars())));
                        _ = Directory.CreateDirectory(seriesDir);
                        string target = Path.Join(seriesDir, recording.Filename);
                        if (File.Exists(target)) {
                            logger.LogInformation("Skipping download of {SeriesName} episode {EpisodeNumber} because it already exists at {Path}", storage.Title, recording.EpisodeNumber, target);
                        } else {
                            logger.LogInformation("Found {SeriesName} episode {EpisodeNumber} to download from {Url}", storage.Title, recording.EpisodeNumber, recording.PlayUrl);
                            await File.WriteAllTextAsync(target + ".storage.json", JsonSerializer.Serialize(storage), cancellationToken);
                            await File.WriteAllTextAsync(target + ".episode.json", JsonSerializer.Serialize(recording), cancellationToken);
                            await downloadManager.Add(recording, target, cancellationToken);
                        }
                    }
                }
            }
        }
        await downloadManager.Run(progress, cancellationToken);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [
        new TaskTriggerInfo {
            Type = TaskTriggerInfo.TriggerStartup,
        },
        new TaskTriggerInfo {
            IntervalTicks = TimeSpan.FromMinutes(10).Ticks,
            Type = TaskTriggerInfo.TriggerInterval,
        },
    ];
}
