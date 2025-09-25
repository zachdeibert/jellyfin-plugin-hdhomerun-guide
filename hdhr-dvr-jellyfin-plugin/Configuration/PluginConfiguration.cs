namespace Com.ZachDeibert.MediaTools.Hdhr.Dvr.Jellyfin.Configuration;

using Com.ZachDeibert.MediaTools.Hdhr.Dvr.Core.Configuration;
using MediaBrowser.Model.Plugins;

public class PluginConfiguration : BasePluginConfiguration {
    public string MovieRecordingPath { get; set; } = "";
    public string SeriesRecordingPath { get; set; } = "";
    public DeletePolicy DeletePolicy { get; set; }
    public ReRecordPolicy ReRecordPolicy { get; set; }
}
