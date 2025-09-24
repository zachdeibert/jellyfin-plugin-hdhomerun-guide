namespace Com.ZachDeibert.MediaTools.Hdhr.Dvr.Core;

using Com.ZachDeibert.MediaTools.Hdhr.Api;
using Microsoft.EntityFrameworkCore;

[PrimaryKey(nameof(Id))]
public record class Episode {
    public int Id;
    public Series? Series { get; set; }
    public DateTimeOffset SeriesStartTime { get; set; }
    public Recording? Metadata { get; set; }
    public bool DownloadInterrupted { get; set; }
    public DateTime DownloadStarted { get; set; }
    public DownloadReason DownloadReason { get; set; }
    public DeleteReason DeleteReason { get; set; }
    public bool ReRecordable { get; set; }
}
