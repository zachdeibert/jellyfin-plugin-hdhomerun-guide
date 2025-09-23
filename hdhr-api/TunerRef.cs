namespace Com.ZachDeibert.MediaTools.Hdhr.Api;

using System.Net.Http.Json;
using Com.ZachDeibert.MediaTools.Hdhr.Api.Dtos;
using Microsoft.Extensions.Logging;

public class TunerRef(string ip) {
    public readonly string BaseUrl = ip.StartsWith("http") ? ip : $"http://{ip}";

    public async Task<Tuner?> Discover(HttpClient client, ILogger? logger = null, CancellationToken cancellationToken = default) {
        if (!ip.StartsWith("http://")) {
            ip = $"http://{ip}";
        }
        string url = $"{ip}/discover.json";
        using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead, cancellationToken);
        DiscoverResponse? discovery = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<DiscoverResponse>(cancellationToken);
        if (discovery == null) {
            logger?.LogWarning("Unable to decode JSON response from {Url}", url);
            return null;
        } else if (discovery.DeviceID == null) {
            logger?.LogWarning("JSON response from {Url} missing required field DeviceID", url);
            return null;
        } else if (discovery.DeviceAuth == null) {
            logger?.LogWarning("JSON response from {Url} missing required field DeviceAuth", url);
            return null;
        } else if (discovery.LineupURL == null) {
            logger?.LogWarning("JSON response from {Url} missing required field LineupURL", url);
            return null;
        } else if (discovery.ExtraFields?.Count > 0) {
            foreach (string field in discovery.ExtraFields.Keys) {
                logger?.LogWarning("JSON response from {Url} contains extra field {Field}", url, field);
            }
            return null;
        }
        return new() {
            FriendlyName = discovery.FriendlyName ?? "HDHomeRun",
            ModelNumber = discovery.ModelNumber ?? "unknown",
            FirmwareName = discovery.FirmwareName ?? "hdhomerun",
            FirmwareVersion = discovery.FirmwareVersion ?? "unknown",
            DeviceId = discovery.DeviceID,
            DeviceAuth = discovery.DeviceAuth,
            BaseUrl = discovery.BaseURL ?? ip,
            LineupUrl = discovery.LineupURL,
            TunerCount = discovery.TunerCount,
            Storage = discovery.StorageID != null && discovery.StorageURL != null ? new() {
                Id = discovery.StorageID,
                Url = discovery.StorageURL,
                TotalSpace = discovery.TotalSpace,
                FreeSpace = discovery.FreeSpace,
            } : null,
        };
    }
}
