namespace Com.ZachDeibert.JellyfinPluginHDHomeRunGuide.Listings;

internal class GuideResponse {
    public string? GuideNumber { get; set; }
    public string? GuideName { get; set; }
    public string? Affiliate { get; set; }
    public string? ImageURL { get; set; }
    public GuideEntry[] Guide { get; set; } = [];
}
