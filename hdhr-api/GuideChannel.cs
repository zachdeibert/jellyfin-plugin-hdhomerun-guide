namespace Com.ZachDeibert.MediaTools.Hdhr.Api;

public record class GuideChannel {
    public required string GuideNumber { get; init; }
    public required string GuideName { get; init; }
    public required string? Affiliate { get; init; }
    public required string? ImageUrl { get; init; }
    public required IEnumerable<GuideProgram> Programs { get; init; }
}
