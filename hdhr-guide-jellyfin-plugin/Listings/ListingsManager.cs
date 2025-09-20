namespace Com.ZachDeibert.MediaTools.Hdhr.Guide.Jellyfin.Listings;

using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.LiveTv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class ListingsManager(IListingsManager manager, ILogger<ListingsManager> logger, IServiceProvider provider) : IHostedService {
    private const string ProvidersFieldName = "_listingsProviders";
    private FieldInfo? providers;
    private IListingsProvider[]? origValue;

    public Task StartAsync(CancellationToken cancellationToken) {
        Type managerType = manager.GetType();
        providers = managerType.GetField(ProvidersFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        origValue = (IListingsProvider[]?) providers?.GetValue(manager);
        if (origValue == null) {
            logger.LogWarning("Unable to access {Field} on {Type}", ProvidersFieldName, managerType.FullName);
        } else {
            HDHomeRun? service = provider.GetService<HDHomeRun>();
            if (service == null) {
                logger.LogWarning("Unable to find HDHomeRun listings service");
            } else {
                providers?.SetValue(manager, (IListingsProvider[]) [.. origValue, service]);
                logger.LogDebug("Registered listing provider on {Type} at runtime", managerType.FullName);
            }
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        if (origValue != null) {
            providers?.SetValue(manager, origValue);
        }
        return Task.CompletedTask;
    }
}
