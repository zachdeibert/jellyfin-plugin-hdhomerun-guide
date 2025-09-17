namespace Com.ZachDeibert.JellyfinPluginHDHomeRunDVR;

using System.Collections.Generic;
using Com.ZachDeibert.JellyfinPluginHDHomeRunDVR.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages {
    public override string Description => "Download DVR content recorded on HDHomeRun";
    public override Guid Id => Guid.Parse("EB540661-5435-406D-8A92-F7D8474D247C");
    public static Plugin? Instance { get; private set; }
    public override string Name => "HDHomeRun DVR";

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer) => Instance = this;

    public IEnumerable<PluginPageInfo> GetPages() => [
        new PluginPageInfo {
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html",
            Name = Name,
        }
    ];
}
