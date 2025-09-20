namespace Com.ZachDeibert.MediaTools.Hdhr.Dvr.Jellyfin.Dtos;

using System.Text.Json;
using System.Text.Json.Serialization;

internal class EpisodesResponse {
    public string? Category { get; set; }
    public string? ChannelImageURL { get; set; }
    public string? ChannelName { get; set; }
    public string? ChannelNumber { get; set; }
    public int EndTime { get; set; }
    public string? EpisodeNumber { get; set; }
    public string? EpisodeTitle { get; set; }
    public int FirstAiring { get; set; }
    public string? ImageURL { get; set; }
    public string? MovieScore { get; set; }
    public int OriginalAirdate { get; set; }
    public string? PosterURL { get; set; }
    public string? ProgramID { get; set; }
    public int RecordEndTime { get; set; }
    public string? RecordError { get; set; }
    public int RecordStartTime { get; set; }
    public int RecordSuccess { get; set; }
    public string? SeriesID { get; set; }
    public int StartTime { get; set; }
    public string? Synopsis { get; set; }
    public string? Title { get; set; }
    public string? Filename { get; set; }
    public string? PlayURL { get; set; }
    public string? CmdURL { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; set; }
}
