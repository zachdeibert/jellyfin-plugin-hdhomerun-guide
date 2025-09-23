namespace Com.ZachDeibert.MediaTools.Hdhr.Api.Dtos;

using System.Text.Json;
using System.Text.Json.Serialization;

internal class GuideEntry {
    public int StartTime { get; set; }
    public int EndTime { get; set; }
    public string? Title { get; set; }
    public string? EpisodeNumber { get; set; }
    public string? EpisodeTitle { get; set; }
    public string? Synopsis { get; set; }
    public string? Team1 { get; set; }
    public string? Team2 { get; set; }
    public int OriginalAirdate { get; set; }
    public string? SeriesID { get; set; }
    public string? ImageURL { get; set; }
    public string? PosterURL { get; set; }
    public int RecordingRule { get; set; }
    public string[] Filter { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; set; }
}
