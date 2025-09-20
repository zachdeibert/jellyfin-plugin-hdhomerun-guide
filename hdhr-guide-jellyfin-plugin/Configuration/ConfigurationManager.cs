namespace Com.ZachDeibert.MediaTools.Hdhr.Guide.Jellyfin.Configuration;

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class ConfigurationManager(IConfigurationManager config, ILogger<ConfigurationManager> logger) : IHostedService {
    private const string HDHomeRunType = "hdhomerun";
    private const string LiveTVConfig = "livetv";

    public Task StartAsync(CancellationToken cancellationToken) {
        config.NamedConfigurationUpdating += NamedConfigurationUpdating;
        config.SaveConfiguration(LiveTVConfig, config.GetConfiguration(LiveTVConfig));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        config.NamedConfigurationUpdating -= NamedConfigurationUpdating;
        return Task.CompletedTask;
    }

    private void NamedConfigurationUpdating(object? sender, ConfigurationUpdateEventArgs ev) {
        if (ev.Key == LiveTVConfig) {
            LiveTvOptions livetv = (LiveTvOptions) ev.NewConfiguration;
            string[] tuners = [.. livetv.TunerHosts.Where(t => t.Type == HDHomeRunType).Select(t => t.Id)];
            logger.LogDebug("Configuring {Length} HDHomeRun tuners for listings data", tuners.Length);
            if (tuners.Length == 0) {
                livetv.ListingProviders = [.. livetv.ListingProviders.Where(p => p.Type != HDHomeRunType)];
            } else {
                ListingsProviderInfo? provider = livetv.ListingProviders.FirstOrDefault(p => p.Type == HDHomeRunType);
                if (provider == null) {
                    provider = new() {
                        Id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                        Type = HDHomeRunType,
                        EnableAllTuners = false,
                    };
                    livetv.ListingProviders = [.. livetv.ListingProviders, provider];
                }
                provider.EnabledTuners = tuners;
            }
        }
    }
}
