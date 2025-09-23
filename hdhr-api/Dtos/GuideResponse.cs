namespace Com.ZachDeibert.MediaTools.Hdhr.Api.Dtos;

using System.Text.Json;
using System.Text.Json.Serialization;

internal class GuideResponse {
    public string? GuideNumber { get; set; }
    public string? GuideName { get; set; }
    public string? Affiliate { get; set; }
    public string? ImageURL { get; set; }
    public GuideEntry[] Guide { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; set; }
}
