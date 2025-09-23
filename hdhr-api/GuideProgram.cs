namespace Com.ZachDeibert.MediaTools.Hdhr.Api;

public record class GuideProgram {
    public required DateTimeOffset StartTime { get; init; }
    public required DateTimeOffset EndTime { get; init; }
    public required string Title { get; init; }
    public required string? EpisodeNumber { get; init; }
    public required string EpisodeTitle { get; init; }
    public required string? Synopsis { get; init; }
    public required IEnumerable<string> Teams { get; init; }
    public required DateTimeOffset OriginalAirdate { get; init; }
    public required string SeriesId { get; init; }
    public required string? ImageUrl { get; init; }
    public required string? PosterUrl { get; init; }
    public required bool Recording { get; init; }
    public required IEnumerable<string> Categories { get; init; }
}
