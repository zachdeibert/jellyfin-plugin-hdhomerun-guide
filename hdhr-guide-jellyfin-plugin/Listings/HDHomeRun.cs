namespace Com.ZachDeibert.MediaTools.Hdhr.Guide.Jellyfin.Listings;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Com.ZachDeibert.MediaTools.Hdhr.Api;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;

public class HDHomeRun(IConfigurationManager config, IHttpClientFactory httpClientFactory, ILogger<HDHomeRun> logger) : IListingsProvider {
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
        Tuner? tuner = await new TunerRef(ip).Discover(httpClient, logger, cancellationToken);
        if (tuner == null) {
            logger.LogWarning("Unable to connect to HDHomeRun device at {Ip}", ip);
            return [];
        }
        logger.LogTrace("Discovered HDHomeRun with lineup at {Url} and auth token {Token}", tuner.LineupUrl, tuner.DeviceAuth);
        Channel[] lineup = [.. await tuner.GetLineup(httpClient, logger, cancellationToken)];
        if (logger.IsEnabled(LogLevel.Trace)) {
            foreach (Channel c in lineup) {
                logger.LogTrace("Discovered HDHomeRun lineup {GuideNumber} ({GuideName}) with codec {VideoCodec}/{AudioCodec} (HD={IsHd})", c.GuideNumber, c.GuideName, c.VideoCodec, c.AudioCodec, c.IsHd);
            }
        }
        GuideChannel[] guide = [.. await tuner.GetGuide(httpClient, logger, cancellationToken)];
        if (logger.IsEnabled(LogLevel.Trace)) {
            foreach (GuideChannel c in guide) {
                logger.LogTrace("Discovered HDHomeRun guide for {GuideNumber} ({GuideName}): {ImageUrl}", c.GuideNumber, c.GuideName, c.ImageUrl);
                foreach (GuideProgram p in c.Programs) {
                    logger.LogTrace("Guide has entry {StartTime} - {EndTime} (orig {OriginalAirtime}): {SeriesId}.{EpisodeNumber}", p.StartTime, p.EndTime, p.OriginalAirdate, p.SeriesId, p.EpisodeNumber);
                    logger.LogTrace("Show: {Title}: {ImageUrl} ({Categories})", p.Title, p.ImageUrl, p.Categories);
                    logger.LogTrace("Episode: {EpisodeTitle} ({Synopsis})", p.EpisodeTitle, p.Synopsis);
                }
            }
        }
        return guide.Join(lineup, c => c.GuideNumber, c => c.GuideNumber, (guide, lineup) => (guide, lineup)).Select(d => new ListingChannel {
            Channel = {
                Name = d.guide.GuideName,
                Number = d.guide.GuideNumber,
                Id = $"hdhr_{d.guide.GuideNumber}",
                ChannelType = ChannelType.TV,
                ImageUrl = d.guide.ImageUrl,
                IsHD = d.lineup.IsHd,
                AudioCodec = d.lineup.AudioCodec,
                VideoCodec = d.lineup.VideoCodec,
            },
            Programs = [.. d.guide.Programs.Select(entry => new ProgramInfo {
                Id = string.Join("-", new string?[] {entry.SeriesId, entry.EpisodeNumber}.Where(e => e != null)),
                ChannelId = $"hdhr_{d.guide.GuideNumber}",
                Name = entry.Title,
                Overview = entry.Synopsis,
                StartDate = entry.StartTime.DateTime,
                EndDate = entry.EndTime.DateTime,
                Genres = [.. entry.Categories],
                OriginalAirDate = entry.OriginalAirdate.DateTime,
                IsHD = d.lineup.IsHd,
                EpisodeTitle = entry.EpisodeTitle,
                ImageUrl = entry.ImageUrl,
                IsMovie = entry.Categories.Any(c => info.MovieCategories.Contains(c.ToLower())),
                IsSports = entry.Categories.Any(c => info.SportsCategories.Contains(c.ToLower())),
                IsNews = entry.Categories.Any(c => info.NewsCategories.Contains(c.ToLower())),
                IsKids = entry.Categories.Any(c => info.KidsCategories.Contains(c.ToLower())),
                SeriesId = entry.SeriesId,
                SeasonNumber = entry.EpisodeNumber == null ? null : int.Parse(new string([.. entry.EpisodeNumber.Skip(1).TakeWhile(c => c != 'E')])),
                EpisodeNumber = entry.EpisodeNumber == null ? null : int.Parse(new string([.. entry.EpisodeNumber.SkipWhile(c => c != 'E').Skip(1)])),
            })],
        });
    }
}
