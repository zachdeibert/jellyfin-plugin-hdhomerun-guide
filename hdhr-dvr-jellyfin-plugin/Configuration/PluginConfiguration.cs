namespace Com.ZachDeibert.MediaTools.Hdhr.Dvr.Jellyfin.Configuration;

using MediaBrowser.Model.Plugins;

public class PluginConfiguration : BasePluginConfiguration {
    public string MovieRecordingPath { get; set; } = "";
    public string SeriesRecordingPath { get; set; } = "";
}
