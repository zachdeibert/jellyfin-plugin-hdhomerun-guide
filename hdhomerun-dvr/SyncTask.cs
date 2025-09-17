namespace Com.ZachDeibert.JellyfinPluginHDHomeRunDVR;

using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Com.ZachDeibert.JellyfinPluginHDHomeRunDVR.Dtos;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

public class SyncTask(IConfigurationManager config, IHttpClientFactory httpClientFactory, ILogger<SyncTask> logger) : IScheduledTask {
    public string Category => "Live TV";

    public string Description => "Downloads new recordings from connected HDHomeRun DVRs";

    public string Key => GetType().FullName!;

    public string Name => "Sync HDHomeRun DVR";

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken) {
        using HttpClient httpClient = httpClientFactory.CreateClient(NamedClient.Default);
        foreach ((StorageResponse, EpisodesResponse) episode in await GetEpisodes(httpClient, cancellationToken)) {
            logger.LogInformation("Found {SeriesName} episode {EpisodeNumber} to download from {Url}", episode.Item1.Title, episode.Item2.EpisodeNumber, episode.Item2.PlayURL);
        }
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

    private async Task<IEnumerable<EpisodesResponse>> GetEpisodes(HttpClient client, StorageResponse storage, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await client.GetAsync(storage.EpisodesURL, HttpCompletionOption.ResponseContentRead, cancellationToken);
        IEnumerable<EpisodesResponse>? episodes = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<EpisodesResponse[]>(cancellationToken);
        if (episodes == null || !episodes.Any()) {
            logger.LogWarning("Series {SeriesId} ({SeriesUrl}) has no episodes", storage.SeriesID, storage.EpisodesURL);
            return [];
        }
        foreach (EpisodesResponse episode in episodes) {
            if (episode.ExtraFields != null) {
                foreach (string field in episode.ExtraFields.Keys) {
                    logger.LogError("Series {SeriesId} ({SeriesUrl}) episode {EpisodeNumber} has extra field {FieldName}", storage.SeriesID, storage.EpisodesURL, episode.EpisodeNumber, field);
                }
            }
        }
        return episodes.Where(e => e.ExtraFields == null || e.ExtraFields.Count == 0);
    }

    private async Task<IEnumerable<(StorageResponse, EpisodesResponse)>> GetEpisodes(HttpClient client, CancellationToken cancellationToken) {
        (StorageResponse, Task<IEnumerable<EpisodesResponse>>)[] episodes = [.. (await GetRecordings(client, cancellationToken)).Select(r => (r, GetEpisodes(client, r, cancellationToken)))];
        _ = await Task.WhenAll([.. episodes.Select(e => e.Item2)]);
        return episodes.SelectMany(e => e.Item2.Result.Select(r => (e.Item1, r)));
    }

    private async Task<IEnumerable<StorageResponse>> GetRecordings(HttpClient client, string storageUrl, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await client.GetAsync(storageUrl, HttpCompletionOption.ResponseContentRead, cancellationToken);
        IEnumerable<StorageResponse>? recordings = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<StorageResponse[]>(cancellationToken);
        if (recordings == null || !recordings.Any()) {
            logger.LogInformation("Tuner {StorageUrl} has no recordings to download", storageUrl);
            return [];
        }
        foreach (StorageResponse recording in recordings) {
            if (recording.ExtraFields != null) {
                foreach (string field in recording.ExtraFields.Keys) {
                    logger.LogError("Tuner {StorageUrl} recording {SeriesId} has extra field {FieldName}", storageUrl, recording.SeriesID, field);
                }
            }
        }
        return recordings.Where(r => r.ExtraFields == null || r.ExtraFields.Count == 0);
    }

    private async Task<IEnumerable<StorageResponse>> GetRecordings(HttpClient client, CancellationToken cancellationToken)
        => (await Task.WhenAll([.. (await GetStorageUrls(client, cancellationToken)).Select(u => GetRecordings(client, u, cancellationToken))])).SelectMany(r => r).GroupBy(r => r.EpisodesURL).Select(g => g.First());

    private async Task<string?> GetStorageUrl(HttpClient client, string tuner, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await client.GetAsync(tuner + "/discover.json", HttpCompletionOption.ResponseContentRead, cancellationToken);
        return (await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<DiscoverResponse>(cancellationToken))?.StorageURL;
    }

    private async Task<IEnumerable<string>> GetStorageUrls(HttpClient client, CancellationToken cancellationToken)
        => (await Task.WhenAll([.. GetTuners().Select(t => GetStorageUrl(client, t, cancellationToken))])).Where(u => u != null)!;

    private IEnumerable<string> GetTuners()
        => config.GetConfiguration<LiveTvOptions>("livetv").TunerHosts.Select(t => t.Url.StartsWith("http://") ? t.Url : "http://" + t.Url);
}
