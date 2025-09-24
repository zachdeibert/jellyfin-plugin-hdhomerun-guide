namespace Com.ZachDeibert.MediaTools.Hdhr.Dvr.Jellyfin;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Com.ZachDeibert.MediaTools.Hdhr.Api;
using Com.ZachDeibert.MediaTools.Hdhr.Dvr.Core;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class SyncTask(IConfigurationManager config, IHttpClientFactory httpClientFactory, ILogger<DownloadManager> downloadLogger, ILogger<SyncTask> logger) : IScheduledTask {
    public string Category => "Live TV";

    public string Description => "Downloads new recordings from connected HDHomeRun DVRs";

    public string Key => GetType().FullName!;

    public string Name => "Sync HDHomeRun DVR";

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken) {
        Dictionary<RecordingCategory, (string, Context)> libraries = new[] {
            (RecordingCategory.Movie, Plugin.Instance?.Configuration.MovieRecordingPath),
            (RecordingCategory.Series, Plugin.Instance?.Configuration.SeriesRecordingPath)
        }.Where(l => !string.IsNullOrWhiteSpace(l.Item2)).ToDictionary(l => l.Item1, l => (l.Item2, new Context() { DbPath = Path.Join(l.Item2!, $"{typeof(Plugin).Namespace}.db") }))!;
        foreach ((string, Context) library in libraries.Values) {
            await library.Item2.Database.MigrateAsync(cancellationToken);
        }
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
                Dictionary<RecordingCategory, Series> categorySeries = [];
                foreach (Recording recording in await storage.GetRecordings(httpClient, logger, cancellationToken)) {
                    if (!libraries.TryGetValue(recording.Category, out (string, Context) library)) {
                        logger.LogError("Series {SeriesName} has unknown type {Category}", storage.Title, storage.Category);
                    } else if (recording.RecordEndTime + TimeSpan.FromSeconds(30) > DateTimeOffset.UtcNow) {
                        logger.LogInformation("Not downloading {SeriesName} episode {EpisodeNumber} because it is still recording", storage.Title, recording.EpisodeNumber);
                    } else {
                        if (!categorySeries.TryGetValue(recording.Category, out Series? series)) {
                            series = await library.Item2.Series.FirstOrDefaultAsync(
                                s => s.Metadata!.SeriesId == storage.SeriesId
                                && s.Metadata!.Title == storage.Title
                                && s.Metadata!.Category == storage.Category
                                && s.Metadata!.ImageUrl == storage.ImageUrl
                                && s.Metadata!.PosterUrl == storage.PosterUrl
                                && s.Metadata!.IsNew == storage.IsNew
                                && s.Metadata!.Url == storage.Url,
                                cancellationToken
                            ) ?? (await library.Item2.Series.AddAsync(new() {
                                Metadata = storage,
                            }, cancellationToken)).Entity;
                            categorySeries.Add(recording.Category, series);
                        }
                        Episode? episode = await library.Item2.Episodes.FirstOrDefaultAsync(e => e.Series == series && e.Metadata!.Filename == recording.Filename && e.DeleteReason == DeleteReason.NotDeleted, cancellationToken);
                        if (episode?.DownloadInterrupted == false) {
                            logger.LogInformation("Skipping download of {SeriesName} episode {EpisodeNumber} because it already exists", storage.Title, recording.EpisodeNumber);
                        } else {
                            string seriesDir = Path.Join(library.Item1, string.Concat(string.Format("{0} ({1})", storage.Title, storage.SeriesId).Split(Path.GetInvalidFileNameChars())));
                            _ = Directory.CreateDirectory(seriesDir);
                            string target = Path.Join(seriesDir, recording.Filename);
                            if (episode == null && File.Exists(target)) {
                                logger.LogWarning("Skipping download of {SeriesName} episode {EpisodeNumber} because it already exists at {Path}", storage.Title, recording.EpisodeNumber, target);
                            } else {
                                if (episode == null) {
                                    logger.LogInformation("Found {SeriesName} episode {EpisodeNumber} to download from {Url}", storage.Title, recording.EpisodeNumber, recording.PlayUrl);
                                } else {
                                    episode.DeleteReason = DeleteReason.ReDownloaded;
                                    logger.LogInformation("Retrying download of {SeriesName} episode {EpisodeNumber} from {Url}", storage.Title, recording.EpisodeNumber, recording.PlayUrl);
                                }
                                episode = (await library.Item2.Episodes.AddAsync(new() {
                                    Series = series,
                                    SeriesStartTime = storage.StartTime,
                                    Metadata = recording,
                                    DownloadInterrupted = true,
                                    DownloadStarted = DateTime.Now,
                                    DownloadReason = episode == null ? DownloadReason.New : DownloadReason.DownloadInterrupted,
                                    DeleteReason = DeleteReason.NotDeleted,
                                    ReRecordable = false,
                                }, cancellationToken)).Entity;
                                await downloadManager.Add(library.Item2, episode, target, cancellationToken);
                            }
                        }
                    }
                }
            }
        }
        foreach ((string, Context) library in libraries.Values) {
            _ = await library.Item2.SaveChangesAsync(cancellationToken);
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
