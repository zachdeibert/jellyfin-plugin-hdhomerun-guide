namespace Com.ZachDeibert.MediaTools.Hdhr.Api;

using Microsoft.Extensions.Logging;

public record class Recording {
    public required RecordingCategory Category { get; init; }
    public required string? ChannelImageUrl { get; init; }
    public required string ChannelName { get; init; }
    public required string ChannelNumber { get; init; }
    public required DateTimeOffset EndTime { get; init; }
    public required string? EpisodeNumber { get; init; }
    public required string EpisodeTitle { get; init; }
    public required bool FirstAiring { get; init; }
    public required string ImageUrl { get; init; }
    public required string? MovieScore { get; init; }
    public required DateTimeOffset OriginalAirdate { get; init; }
    public required string? PosterUrl { get; init; }
    public required string ProgramId { get; init; }
    public required DateTimeOffset RecordEndTime { get; init; }
    public required string? RecordError { get; init; }
    public required DateTimeOffset RecordStartTime { get; init; }
    public required bool RecordSuccess { get; init; }
    public required string SeriesId { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public required string Synopsis { get; init; }
    public required string Title { get; init; }
    public required string Filename { get; init; }
    public required string PlayUrl { get; init; }
    public required string CmdUrl { get; init; }

    public async Task<long> GetFileSize(HttpClient client, ILogger? logger = null, CancellationToken cancellationToken = default) {
        using HttpResponseMessage response = await client.GetAsync(PlayUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        long? size = response.EnsureSuccessStatusCode().Content.Headers.ContentLength;
        if (size == null) {
            logger?.LogWarning("Unable to determine Content-Length for {Url}", PlayUrl);
            return 0;
        }
        return (long) size;
    }

    public async Task Download(string path, HttpClient client, IProgress<long>? progress = null, ILogger? logger = null, CancellationToken cancellationToken = default) {
        long downloaded = 0;
        byte[] buffer = new byte[16384];
        DateTime lastLog = DateTime.Now;
        using HttpResponseMessage response = await client.GetAsync(PlayUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        using Stream input = await response.EnsureSuccessStatusCode().Content.ReadAsStreamAsync(cancellationToken);
        using FileStream output = File.OpenWrite(path);
        long? size = response.Content.Headers.ContentLength;
        if (size == null) {
            logger?.LogWarning("Unable to determine Content-Length for {Url}", PlayUrl);
            logger?.LogInformation("Downloading {Url} to {Path}...", CmdUrl, path);
        } else {
            logger?.LogInformation("Downloading {Url} to {Path} ({Size} MiB)...", CmdUrl, path, size / (1024 * 1024));
        }
        while (true) {
            int count = await input.ReadAsync(buffer, cancellationToken);
            if (count <= 0) {
                break;
            }
            await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
            downloaded += count;
            progress?.Report(downloaded);
            if (DateTime.Now - lastLog > TimeSpan.FromSeconds(10)) {
                lastLog += TimeSpan.FromSeconds(10);
                if (size == null) {
                    logger?.LogDebug("Downloaded {Done} MiB", downloaded / (1024 * 1024));
                } else {
                    logger?.LogDebug("Downloaded {Done} / {Total} MiB", downloaded / (1024 * 1024), size / (1024 * 1024));
                }
            }
        }
    }
}
