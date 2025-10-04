namespace Com.ZachDeibert.MediaTools.Hdhr.Dvr.Core.Metadata;

using System.Xml.Serialization;

public record class UniqueId {
    [XmlAttribute("type")]
    public required string Type { get; init; }

    [XmlText]
    public required string Id { get; init; }
}
