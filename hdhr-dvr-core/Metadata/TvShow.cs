namespace Com.ZachDeibert.MediaTools.Hdhr.Dvr.Core.Metadata;

using System.Xml.Serialization;

[XmlRoot("tvshow")]
public class TvShow(Series series) {
    [XmlElement("title")]
    public string Title { get; init; } = series.Metadata!.Title;

    [XmlElement("dateadded")]
    public string DateAdded { get; init; } = series.Metadata!.StartTime.ToString("yyyy-MM-dd HH:mm:ss");

    [XmlElement("uniqueid")]
    public UniqueId[] UniqueIds { get; init; } = [
        new() { Type = "hdhomerun-series", Id = series.Metadata.SeriesId },
        new() { Type = typeof(Context).Namespace!, Id = series.Id.ToString() },
    ];

    public TvShow() : this(null!) { }
}
