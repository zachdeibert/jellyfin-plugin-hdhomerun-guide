namespace Com.ZachDeibert.MediaTools.Hdhr.Dvr.Core.Metadata;

using System.Xml.Serialization;

[XmlRoot("season")]
public class Season(int season) {
    [XmlElement("seasonnumber")]
    public int SeasonNumber { get; init; } = season;

    public Season() : this(0) { }
}
