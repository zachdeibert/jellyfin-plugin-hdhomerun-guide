namespace Com.ZachDeibert.MediaTools.Hdhr.Dvr.Core.Metadata;

using System.Xml.Serialization;
using Com.ZachDeibert.MediaTools.Hdhr.Api;

[XmlRoot("episodedetails")]
public class EpisodeDetails(Episode episode) {
    [XmlElement("title")]
    public string Title { get; init; } = episode.Metadata!.EpisodeTitle;

    [XmlElement("dateadded")]
    public string DateAdded { get; init; } = episode.Metadata!.RecordEndTime.ToString("yyyy-MM-dd HH:mm:ss");

    [XmlElement("plot")]
    public string Plot { get; init; } = episode.Metadata!.Synopsis;

    [XmlElement("year")]
    public int Year { get; init; } = episode.Metadata!.OriginalAirdate.Year;

    [XmlElement("rating")]
    public string? Rating { get; init; } = episode.Metadata!.MovieScore;

    [XmlElement("aired")]
    public string Aired { get; init; } = episode.Metadata!.StartTime.ToString("yyyy-MM-dd");

    [XmlElement("premiered")]
    public string Premiered { get; init; } = episode.Metadata!.OriginalAirdate.ToString("yyyy-MM-dd");

    [XmlElement("uniqueid")]
    public UniqueId[] UniqueIds { get; init; } = [
        new() { Type = "hdhomerun", Id = episode.Metadata!.ProgramId },
        new() { Type = "hdhomerun-series", Id = episode.Metadata.SeriesId },
        new() { Type = typeof(Context).Namespace!, Id = episode.Id.ToString() },
    ];

    [XmlElement("showtitle")]
    public string ShowTitle { get; init; } = episode.Metadata!.Title;

    [XmlElement("episode")]
    public int? Episode { get; init; } = episode.Metadata!.EpisodeNumber == null ? null : new EpisodeNumber(episode.Metadata.EpisodeNumber).Episode;

    [XmlElement("season")]
    public int? Season { get; init; } = episode.Metadata!.EpisodeNumber == null ? null : new EpisodeNumber(episode.Metadata.EpisodeNumber).Season;

    public EpisodeDetails() : this(null!) { }
}
