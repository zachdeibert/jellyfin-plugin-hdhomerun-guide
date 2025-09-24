namespace Com.ZachDeibert.MediaTools.Hdhr.Dvr.Core;

using Com.ZachDeibert.MediaTools.Hdhr.Api;

public record class Series {
    public int Id { get; set; }
    public RecordingStorage? Metadata { get; set; }
    public List<Episode> Episodes { get; set; } = [];
}
