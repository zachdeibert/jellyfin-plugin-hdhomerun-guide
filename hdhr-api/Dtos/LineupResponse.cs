namespace Com.ZachDeibert.MediaTools.Hdhr.Api.Dtos;

using System.Text.Json;
using System.Text.Json.Serialization;

internal class LineupResponse {
    public string? GuideNumber { get; set; }
    public string? GuideName { get; set; }
    public string? VideoCodec { get; set; }
    public string? AudioCodec { get; set; }
    public int HD { get; set; }
    public int SignalStrength { get; set; }
    public int SignalQuality { get; set; }
    public string? URL { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; set; }
}
