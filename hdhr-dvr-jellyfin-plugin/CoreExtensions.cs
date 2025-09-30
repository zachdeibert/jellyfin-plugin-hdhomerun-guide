namespace Com.ZachDeibert.MediaTools.Hdhr.Dvr.Jellyfin;

using Com.ZachDeibert.MediaTools.Hdhr.Api;
using Com.ZachDeibert.MediaTools.Hdhr.Dvr.Core;

public static class CoreExtensions {
    public static string? FilePath(this Episode episode, bool mkdir = true) {
        string? libraryPath = episode.Metadata!.Category switch {
            RecordingCategory.Movie => Plugin.Instance?.Configuration.MovieRecordingPath,
            RecordingCategory.Series => Plugin.Instance?.Configuration.SeriesRecordingPath,
            _ => null,
        };
        if (string.IsNullOrWhiteSpace(libraryPath)) {
            return null;
        }
        char[] invalidChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*', .. Path.GetInvalidFileNameChars()];
        string seriesDir = Path.Join(libraryPath, string.Concat($"{episode.Series!.Metadata!.Title} [{episode.Series.Metadata.SeriesId}]".Split(invalidChars)));
        string episodeName;
        if (episode.Metadata.EpisodeNumber != null) {
            EpisodeNumber episodeNumber = new(episode.Metadata.EpisodeNumber);
            seriesDir = Path.Join(seriesDir, $"Season {episodeNumber.Season:00}");
            episodeName = $"{episode.Series.Metadata.Title} {episode.Metadata.EpisodeNumber}";
        } else {
            episodeName = episode.Series.Metadata.Title;
        }
        string tags = new([.. episode.Metadata.Filename.SkipWhile(c => c != '[').TakeWhile(c => c != '.')]);
        if (!string.IsNullOrWhiteSpace(tags)) {
            episodeName = $"{episodeName} - {tags}";
        }
        if (mkdir) {
            _ = Directory.CreateDirectory(seriesDir);
        }
        return Path.Join(seriesDir, string.Concat(episodeName.Split(invalidChars)) + Path.GetExtension(episode.Metadata.Filename));
    }
}
