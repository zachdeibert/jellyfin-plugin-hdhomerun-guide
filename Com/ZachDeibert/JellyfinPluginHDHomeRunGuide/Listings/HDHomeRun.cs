namespace Com.ZachDeibert.JellyfinPluginHDHomeRunGuide.Listings;

using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;

public class HDHomeRun(IConfigurationManager config, IHttpClientFactory httpClientFactory, ILogger<HDHomeRun> logger) : IListingsProvider {
    private static readonly DateTime Epoch = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    public string Name => "HDHomeRun";

    public string Type => "hdhomerun";
    private readonly Dictionary<string, ListingInfo> listingCache = [];

    public async Task<List<ChannelInfo>> GetChannels(ListingsProviderInfo info, CancellationToken cancellationToken)
        => [.. (await GetListings(info, cancellationToken)).Select(l => l.Value.Channel)];

    public async Task<List<NameIdPair>> GetLineups(ListingsProviderInfo info, string country, string location)
        => [.. (await GetListings(info, CancellationToken.None)).Select(l => new NameIdPair { Name = l.Value.Channel.Name, Id = l.Value.Channel.Id })];

    public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(ListingsProviderInfo info, string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(channelId)) {
            throw new ArgumentNullException(nameof(channelId));
        }
        if ((await GetListings(info, cancellationToken)).TryGetValue(channelId, out ListingChannel? channel)) {
            return channel.Programs.Where(p => p.StartDate >= startDateUtc && p.EndDate <= endDateUtc);
        } else {
            return [];
        }
    }

    public async Task Validate(ListingsProviderInfo info, bool validateLogin, bool validateListings)
        => await GetListings(info, CancellationToken.None);

    private async Task<Dictionary<string, ListingChannel>> GetListings(ListingsProviderInfo info, CancellationToken cancellationToken) {
        if (!listingCache.TryGetValue(info.Id, out ListingInfo? cache)) {
            cache = new();
            listingCache.Add(info.Id, cache);
        }
        DateTime now = DateTime.Now;
        if (now - cache.LastUpdated > TimeSpan.FromMinutes(1)) {
            logger.LogDebug("Refreshing HDHomeRun listings cache...");
            cache.Channels.Clear();
            LiveTvOptions livetv = config.GetConfiguration<LiveTvOptions>("livetv");
            foreach (string tunerId in info.EnabledTuners) {
                TunerHostInfo? host = livetv.TunerHosts.FirstOrDefault(t => t.Id == tunerId);
                if (host == null) {
                    logger.LogWarning("Unable to find tuner {Id}", tunerId);
                } else {
                    foreach (ListingChannel channel in await LoadListings(info, host.Url, cancellationToken)) {
                        if (cache.Channels.TryGetValue(channel.Channel.Id, out ListingChannel? existing)) {
                            existing.Programs = [.. existing.Programs.Concat(channel.Programs).GroupBy(p => p.StartDate).Select(g => g.First())];
                        } else {
                            cache.Channels.Add(channel.Channel.Id, channel);
                        }
                    }
                }
            }
            if (!cancellationToken.IsCancellationRequested) {
                cache.LastUpdated = now;
            }
        }
        return cache.Channels;
    }

    private async Task<IEnumerable<ListingChannel>> LoadListings(ListingsProviderInfo info, string ip, CancellationToken cancellationToken) {
        using HttpClient httpClient = httpClientFactory.CreateClient(NamedClient.Default);
        DiscoverResponse? discovery;
        using (HttpResponseMessage response = await httpClient.GetAsync("http://" + ip + "/discover.json", HttpCompletionOption.ResponseContentRead, cancellationToken)) {
            discovery = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<DiscoverResponse>(cancellationToken);
            if (discovery == null || discovery.DeviceAuth == null || discovery.LineupURL == null) {
                logger.LogWarning("Unable to connect to HDHomeRun device at {Ip}", ip);
                return [];
            }
        }
        logger.LogTrace("Discovered HDHomeRun with lineup at {Url} and auth token {Token}", discovery.LineupURL, discovery.DeviceAuth);
        LineupResponse[]? lineup;
        using (HttpResponseMessage response = await httpClient.GetAsync(discovery.LineupURL, HttpCompletionOption.ResponseContentRead, cancellationToken)) {
            lineup = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<LineupResponse[]>(cancellationToken);
            if (lineup == null) {
                logger.LogWarning("Unable to get lineup data from HDHomeRun device at {Ip}", ip);
                lineup = [];
            }
        }
        if (logger.IsEnabled(LogLevel.Trace)) {
            foreach (LineupResponse l in lineup) {
                logger.LogTrace("Discovered HDHomeRun lineup {GuideNumber} ({GuideName}) with codec {VideoCodec}/{AudioCodec} (HD={HD})", l.GuideNumber, l.GuideName, l.VideoCodec, l.AudioCodec, l.HD);
            }
        }
        GuideResponse[]? guide;
        using (HttpResponseMessage response = await httpClient.GetAsync("https://api.hdhomerun.com/api/guide.php?DeviceAuth=" + discovery.DeviceAuth, HttpCompletionOption.ResponseContentRead, cancellationToken)) {
            guide = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<GuideResponse[]>(cancellationToken);
            if (guide == null) {
                logger.LogWarning("Unable to connect to HDHomeRun API");
                return [];
            }
        }
        if (logger.IsEnabled(LogLevel.Trace)) {
            foreach (GuideResponse g in guide) {
                logger.LogTrace("Discovered HDHomeRun guide for {GuideNumber} ({GuideName}): {ImageURL}", g.GuideNumber, g.GuideName, g.ImageURL);
                foreach (GuideEntry e in g.Guide) {
                    logger.LogTrace("Guide has entry {StartTime} - {EndTime} (orig {OriginalAirtime}): {SeriesID}.{EpisodeNumber}", e.StartTime, e.EndTime, e.OriginalAirdate, e.SeriesID, e.EpisodeNumber);
                    logger.LogTrace("Show: {Title}: {ImageURL} ({Filter})", e.Title, e.ImageURL, e.Filter);
                    logger.LogTrace("Episode: {EpisodeTitle} ({Synopsis})", e.EpisodeTitle, e.Synopsis);
                }
            }
        }
        return guide.Join(lineup, r => r.GuideNumber, l => l.GuideNumber, (guide, lineup) => (guide, lineup)).Select(d => new ListingChannel {
            Channel = {
                Name = d.guide.GuideName ?? d.lineup?.GuideName,
                Number = d.guide.GuideNumber,
                Id = d.guide.GuideNumber == null ? null : "hdhr_" + d.guide.GuideNumber,
                ChannelType = ChannelType.TV,
                ImageUrl = d.guide.ImageURL,
                IsHD = d.lineup?.HD == 1,
                AudioCodec = d.lineup?.AudioCodec,
                VideoCodec = d.lineup?.VideoCodec
            },
            Programs = [.. d.guide.Guide.Select(entry => new ProgramInfo {
                Id = string.Join("-", new string?[] {entry.SeriesID, entry.EpisodeNumber }.Where(e => e != null)),
                ChannelId = d.guide.GuideNumber == null ? null : "hdhr_" + d.guide.GuideNumber,
                Name = entry.Title,
                Overview = entry.Synopsis,
                StartDate = Epoch.AddSeconds(entry.StartTime),
                EndDate = Epoch.AddSeconds(entry.EndTime),
                Genres = [.. entry.Filter],
                OriginalAirDate = Epoch.AddSeconds(entry.OriginalAirdate),
                IsHD = d.lineup?.HD == 1,
                EpisodeTitle = entry.EpisodeTitle,
                ImageUrl = entry.ImageURL,
                IsMovie = entry.Filter.Any(f => info.MovieCategories.Contains(f.ToLower())),
                IsSports = entry.Filter.Any(f => info.SportsCategories.Contains(f.ToLower())),
                IsNews = entry.Filter.Any(f => info.NewsCategories.Contains(f.ToLower())),
                IsKids = entry.Filter.Any(f => info.KidsCategories.Contains(f.ToLower())),
                SeriesId = entry.SeriesID,
                SeasonNumber = entry.EpisodeNumber == null ? null : int.Parse(new string([.. entry.EpisodeNumber.Skip(1).TakeWhile(c => c != 'E')])),
                EpisodeNumber = entry.EpisodeNumber == null ? null : int.Parse(new string([.. entry.EpisodeNumber.SkipWhile(c => c != 'E').Skip(1)])),
            })],
        });
    }
}
