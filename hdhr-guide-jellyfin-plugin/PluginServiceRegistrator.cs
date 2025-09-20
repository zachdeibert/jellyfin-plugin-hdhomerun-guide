namespace Com.ZachDeibert.MediaTools.Hdhr.Guide.Jellyfin;

using Com.ZachDeibert.MediaTools.Hdhr.Guide.Jellyfin.Configuration;
using Com.ZachDeibert.MediaTools.Hdhr.Guide.Jellyfin.Listings;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

public class PluginServiceRegistrator : IPluginServiceRegistrator {
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost) {
        _ = serviceCollection.AddHostedService<ConfigurationManager>();
        _ = serviceCollection.AddHostedService<ListingsManager>();
        _ = serviceCollection.AddSingleton<HDHomeRun>();
    }
}
