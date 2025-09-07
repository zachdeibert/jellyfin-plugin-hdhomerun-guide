namespace Com.ZachDeibert.JellyfinPluginHDHomeRunGuide;

using Com.ZachDeibert.JellyfinPluginHDHomeRunGuide.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

public class Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : BasePlugin<PluginConfiguration>(applicationPaths, xmlSerializer) {
    public override string Description => "Pull guide data from HDHomeRun";
    public override Guid Id => Guid.Parse("0B4BBA13-E571-435C-8319-84D55E93A5DA");
    public override string Name => "HDHomeRun Guide";
}
