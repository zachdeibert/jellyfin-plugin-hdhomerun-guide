namespace Com.ZachDeibert.MediaTools.Hdhr.Guide.Jellyfin.Listings;

using MediaBrowser.Controller.LiveTv;

internal class ListingChannel {
    public ChannelInfo Channel = new();
    public ProgramInfo[] Programs = [];
}
