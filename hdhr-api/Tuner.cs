namespace Com.ZachDeibert.MediaTools.Hdhr.Api;

using System.Net.Http.Json;
using Com.ZachDeibert.MediaTools.Hdhr.Api.Dtos;
using Microsoft.Extensions.Logging;

public record class Tuner {
    public required string FriendlyName { get; init; }
    public required string ModelNumber { get; init; }
    public required string FirmwareName { get; init; }
    public required string FirmwareVersion { get; init; }
    public required string DeviceId { get; init; }
    public required string DeviceAuth { get; init; }
    public required string BaseUrl { get; init; }
    public required string LineupUrl { get; init; }
    public required int TunerCount { get; init; }
    public required StorageRef? Storage { get; init; }

    public async Task<IEnumerable<GuideChannel>> GetGuide(HttpClient client, ILogger? logger = null, CancellationToken cancellationToken = default) {
        string url = $"https://api.hdhomerun.com/api/guide.php?DeviceAuth={DeviceAuth}";
        using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead, cancellationToken);
        GuideResponse[]? guides = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<GuideResponse[]>(cancellationToken);
        if (guides == null) {
            logger?.LogWarning("Unable to decode JSON response from {Url}", url);
            return [];
        }
        return guides.Select((guide, i) => {
            if (guide.GuideNumber == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field GuideNumber", i, url);
                return null;
            } else if (guide.GuideName == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field GuideName", i, url);
                return null;
            } else if (guide.ExtraFields?.Count > 0) {
                foreach (string field in guide.ExtraFields.Keys) {
                    logger?.LogWarning("JSON response {Index} from {Url} contains extra field {Field}", i, url, field);
                }
                return null;
            }
            return new GuideChannel() {
                GuideNumber = guide.GuideNumber,
                GuideName = guide.GuideName,
                Affiliate = guide.Affiliate,
                ImageUrl = guide.ImageURL,
                Programs = [.. guide.Guide.Select((program, j) => {
                    if (program.StartTime == 0) {
                        logger?.LogWarning("JSON response {Index1}.{Index2} from {Url} missing required field StartTime", i, j, url);
                        return null;
                    } else if (program.EndTime == 0) {
                        logger?.LogWarning("JSON response {Index1}.{Index2} from {Url} missing required field EndTime", i, j, url);
                        return null;
                    } else if (program.Title == null) {
                        logger?.LogWarning("JSON response {Index1}.{Index2} from {Url} missing required field Title", i, j, url);
                        return null;
                    } else if (program.SeriesID == null) {
                        logger?.LogWarning("JSON response {Index1}.{Index2} from {Url} missing required field SeriesID", i, j, url);
                        return null;
                    } else if (program.ExtraFields?.Count > 0) {
                        foreach (string field in program.ExtraFields.Keys) {
                            logger?.LogWarning("JSON response {Index1}.{Index2} from {Url} contains extra field {Field}", i, j, url, field);
                        }
                        return null;
                    }
                    return new GuideProgram() {
                        StartTime = DateTimeOffset.FromUnixTimeSeconds(program.StartTime),
                        EndTime = DateTimeOffset.FromUnixTimeSeconds(program.EndTime),
                        Title = program.Title,
                        EpisodeNumber = program.EpisodeNumber,
                        EpisodeTitle = program.EpisodeTitle ?? program.Title,
                        Synopsis = program.Synopsis,
                        Teams = [.. new[] { program.Team1, program.Team2 }.Where(t => t != null)!],
                        OriginalAirdate = DateTimeOffset.FromUnixTimeSeconds(program.OriginalAirdate == 0 ? program.StartTime : program.OriginalAirdate),
                        SeriesId = program.SeriesID,
                        ImageUrl = program.ImageURL,
                        PosterUrl = program.PosterURL,
                        Recording = program.RecordingRule == 1,
                        Categories = program.Filter,
                    };
                }).Where(p => p != null)!],
            };
        }).Where(c => c != null)!;
    }

    public async Task<IEnumerable<Channel>> GetLineup(HttpClient client, ILogger? logger = null, CancellationToken cancellationToken = default) {
        using HttpResponseMessage response = await client.GetAsync(LineupUrl, HttpCompletionOption.ResponseContentRead, cancellationToken);
        LineupResponse[]? lineups = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<LineupResponse[]>(cancellationToken);
        if (lineups == null) {
            logger?.LogWarning("Unable to decode JSON response from {Url}", LineupUrl);
            return [];
        }
        return lineups.Select((lineup, i) => {
            if (lineup.GuideNumber == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field GuideNumber", i, LineupUrl);
                return null;
            } else if (lineup.GuideName == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field GuideName", i, LineupUrl);
                return null;
            } else if (lineup.VideoCodec == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field VideoCodec", i, LineupUrl);
                return null;
            } else if (lineup.AudioCodec == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field AudioCodec", i, LineupUrl);
                return null;
            } else if (lineup.URL == null) {
                logger?.LogWarning("JSON response {Index} from {Url} missing required field URL", i, LineupUrl);
                return null;
            } else if (lineup.ExtraFields?.Count > 0) {
                foreach (string field in lineup.ExtraFields.Keys) {
                    logger?.LogWarning("JSON response {Index} from {Url} contains extra field {Field}", i, LineupUrl, field);
                }
                return null;
            }
            return new Channel() {
                GuideNumber = lineup.GuideNumber,
                GuideName = lineup.GuideName,
                VideoCodec = lineup.VideoCodec,
                AudioCodec = lineup.AudioCodec,
                IsHd = lineup.HD == 1,
                SignalStrength = lineup.SignalStrength,
                SignalQuality = lineup.SignalQuality,
                Url = lineup.URL,
            };
        }).Where(c => c != null)!;
    }
}
