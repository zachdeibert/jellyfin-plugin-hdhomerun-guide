namespace Com.ZachDeibert.MediaTools.Hdhr.Api;

public readonly struct EpisodeNumber(string formatted) {
    public readonly int Season = int.Parse(new string([.. formatted.Skip(1).TakeWhile(c => c != 'E')]));
    public readonly int Episode = int.Parse(new string([.. formatted.SkipWhile(c => c != 'E').Skip(1)]));
}
