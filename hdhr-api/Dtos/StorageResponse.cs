namespace Com.ZachDeibert.MediaTools.Hdhr.Api.Dtos;

using System.Text.Json;
using System.Text.Json.Serialization;

internal class StorageResponse {
    public string? SeriesID { get; set; }
    public string? Title { get; set; }
    public string? Category { get; set; }
    public string? ImageURL { get; set; }
    public string? PosterURL { get; set; }
    public int StartTime { get; set; }
    public int New { get; set; }
    public string? EpisodesURL { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; set; }
}
