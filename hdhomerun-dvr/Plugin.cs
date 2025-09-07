namespace Com.ZachDeibert.JellyfinPluginHDHomeRunDVR;

using Com.ZachDeibert.JellyfinPluginHDHomeRunDVR.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

public class Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : BasePlugin<PluginConfiguration>(applicationPaths, xmlSerializer) {
    public override string Description => "Download DVR content recorded on HDHomeRun";
    public override Guid Id => Guid.Parse("EB540661-5435-406D-8A92-F7D8474D247C");
    public override string Name => "HDHomeRun DVR";
}
