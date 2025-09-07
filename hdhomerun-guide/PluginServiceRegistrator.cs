namespace Com.ZachDeibert.JellyfinPluginHDHomeRunGuide;

using Com.ZachDeibert.JellyfinPluginHDHomeRunGuide.Configuration;
using Com.ZachDeibert.JellyfinPluginHDHomeRunGuide.Listings;
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
