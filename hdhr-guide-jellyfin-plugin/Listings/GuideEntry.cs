namespace Com.ZachDeibert.MediaTools.Hdhr.Guide.Jellyfin.Listings;

internal class GuideEntry {
    public int StartTime { get; set; }
    public int EndTime { get; set; }
    public string? Title { get; set; }
    public string? EpisodeNumber { get; set; }
    public string? EpisodeTitle { get; set; }
    public string? Synopsis { get; set; }
    public int OriginalAirdate { get; set; }
    public string? SeriesID { get; set; }
    public string? ImageURL { get; set; }
    public string[] Filter { get; set; } = [];
}
