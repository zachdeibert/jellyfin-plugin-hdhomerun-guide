namespace Com.ZachDeibert.MediaTools.Hdhr.Api;

using System.Net.Http.Json;
using Com.ZachDeibert.MediaTools.Hdhr.Api.Dtos;
using Microsoft.Extensions.Logging;

public record class RecordingStorage {
    public required string SeriesId { get; init; }
    public required string Title { get; init; }
    public required RecordingCategory Category { get; init; }
    public required string ImageUrl { get; init; }
    public required string? PosterUrl { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public required bool IsNew { get; init; }
    public required string Url { get; init; }

    public async Task<IEnumerable<Recording>> GetRecordings(HttpClient client, ILogger? logger, CancellationToken cancellationToken = default) {
        using HttpResponseMessage response = await client.GetAsync(Url, HttpCompletionOption.ResponseContentRead, cancellationToken);
        EpisodesResponse[]? recordings = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<EpisodesResponse[]>(cancellationToken);
        if (recordings == null) {
            logger?.LogWarning("Unable to decode JSON response from {Url}", Url);
            return [];
        }
        return recordings.Select((recording, i) => {
            if (recording.Category == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field Category", i, Url);
                return null;
            } else if (recording.ChannelName == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field ChannelName", i, Url);
                return null;
            } else if (recording.ChannelNumber == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field ChannelNumber", i, Url);
                return null;
            } else if (recording.EndTime == 0) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field EndTime", i, Url);
                return null;
            } else if (recording.ImageURL == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field ImageURL", i, Url);
                return null;
            } else if (recording.ProgramID == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field ProgramID", i, Url);
                return null;
            } else if (recording.RecordEndTime == 0) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field RecordEndTime", i, Url);
                return null;
            } else if (recording.RecordStartTime == 0) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field RecordStartTime", i, Url);
                return null;
            } else if (recording.SeriesID == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field SeriesID", i, Url);
                return null;
            } else if (recording.StartTime == 0) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field StartTime", i, Url);
                return null;
            } else if (recording.Synopsis == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field Synopsis", i, Url);
                return null;
            } else if (recording.Title == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field Title", i, Url);
                return null;
            } else if (recording.Filename == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field Filename", i, Url);
                return null;
            } else if (recording.PlayURL == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field PlayURL", i, Url);
                return null;
            } else if (recording.CmdURL == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field CmdURL", i, Url);
                return null;
            } else if (recording.ExtraFields?.Count > 0) {
                foreach (string field in recording.ExtraFields.Keys) {
                    logger?.LogWarning("JSON response {Index} from {Url} contains extra field {Field}", i, Url, field);
                }
                return null;
            }
            RecordingCategory? category = recording.Category.ToRecordingCategory();
            if (category == null) {
                logger?.LogWarning("JSON response {Index} from {Url} has unknown category \"{Category}\"", i, Url, recording.Category);
                return null;
            }
            return new Recording() {
                Category = (RecordingCategory) category,
                ChannelImageUrl = recording.ChannelImageURL,
                ChannelName = recording.ChannelName,
                ChannelNumber = recording.ChannelNumber,
                EndTime = DateTimeOffset.FromUnixTimeSeconds(recording.EndTime),
                EpisodeNumber = recording.EpisodeNumber,
                EpisodeTitle = recording.EpisodeTitle ?? recording.Title,
                FirstAiring = recording.FirstAiring == 1,
                ImageUrl = recording.ImageURL,
                MovieScore = recording.MovieScore,
                OriginalAirdate = DateTimeOffset.FromUnixTimeSeconds(recording.OriginalAirdate == 0 ? recording.StartTime : recording.OriginalAirdate),
                PosterUrl = recording.PosterURL,
                ProgramId = recording.ProgramID,
                RecordEndTime = DateTimeOffset.FromUnixTimeSeconds(recording.RecordEndTime),
                RecordError = recording.RecordError,
                RecordStartTime = DateTimeOffset.FromUnixTimeSeconds(recording.RecordStartTime),
                RecordSuccess = recording.RecordSuccess != 0,
                SeriesId = recording.SeriesID,
                StartTime = DateTimeOffset.FromUnixTimeSeconds(recording.StartTime),
                Synopsis = recording.Synopsis,
                Title = recording.Title,
                Filename = recording.Filename,
                PlayUrl = recording.PlayURL,
                CmdUrl = recording.CmdURL,
            };
        }).Where(r => r != null)!;
    }
}
