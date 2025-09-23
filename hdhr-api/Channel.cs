namespace Com.ZachDeibert.MediaTools.Hdhr.Api;

public record class Channel {
    public required string GuideNumber { get; init; }
    public required string GuideName { get; init; }
    public required string VideoCodec { get; init; }
    public required string AudioCodec { get; init; }
    public required bool IsHd { get; init; }
    public required int SignalStrength { get; init; }
    public required int SignalQuality { get; init; }
    public required string Url { get; init; }
}
