namespace Com.ZachDeibert.MediaTools.Hdhr.Api;

using System.Net.Http.Json;
using Com.ZachDeibert.MediaTools.Hdhr.Api.Dtos;
using Microsoft.Extensions.Logging;

public record class StorageRef {
    public required string Id { get; init; }
    public required string Url { get; init; }
    public required long TotalSpace { get; init; }
    public required long FreeSpace { get; init; }

    public async Task<IEnumerable<RecordingStorage>> GetRecordings(HttpClient client, ILogger? logger = null, CancellationToken cancellationToken = default) {
        using HttpResponseMessage response = await client.GetAsync(Url, HttpCompletionOption.ResponseContentRead, cancellationToken);
        StorageResponse[]? storages = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<StorageResponse[]>(cancellationToken);
        if (storages == null) {
            logger?.LogWarning("Unable to decode JSON response from {Url}", Url);
            return [];
        }
        return storages.Select((storage, i) => {
            if (storage.SeriesID == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field SeriesID", i, Url);
                return null;
            } else if (storage.Title == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field Title", i, Url);
                return null;
            } else if (storage.Category == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field Category", i, Url);
                return null;
            } else if (storage.ImageURL == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field ImageURL", i, Url);
                return null;
            } else if (storage.StartTime == 0) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field StartTime", i, Url);
                return null;
            } else if (storage.EpisodesURL == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field EpisodesURL", i, Url);
                return null;
            } else if (storage.ExtraFields?.Count > 0) {
                foreach (string field in storage.ExtraFields.Keys) {
                    logger?.LogWarning("JSON response {Index} from {Url} contains extra field {Field}", i, Url, field);
                }
                return null;
            }
            RecordingCategory? category = storage.Category.ToRecordingCategory();
            if (category == null) {
                logger?.LogWarning("JSON response {Index} from {Url} has unknown category \"{Category}\"", i, Url, storage.Category);
                return null;
            }
            return new RecordingStorage() {
                SeriesId = storage.SeriesID,
                Title = storage.Title,
                Category = (RecordingCategory) category,
                ImageUrl = storage.ImageURL,
                PosterUrl = storage.PosterURL,
                StartTime = DateTimeOffset.FromUnixTimeSeconds(storage.StartTime),
                IsNew = storage.New == 1,
                Url = storage.EpisodesURL,
            };
        }).Where(s => s != null)!;
    }
}
