namespace Com.ZachDeibert.MediaTools.Hdhr.Dvr.Jellyfin;

using Com.ZachDeibert.MediaTools.Hdhr.Api;
using Com.ZachDeibert.MediaTools.Hdhr.Dvr.Core;

public static class CoreExtensions {
    public static string? FilePath(this Episode episode, bool mkdir = true) {
        string? seriesDir = episode.Series!.FilePath(mkdir);
        if (seriesDir == null) {
            return null;
        }
        string episodeName;
        if (episode.Metadata!.EpisodeNumber != null) {
            EpisodeNumber episodeNumber = new(episode.Metadata.EpisodeNumber);
            seriesDir = Path.Join(seriesDir, $"Season {episodeNumber.Season:00}");
            episodeName = $"{episode.Series!.Metadata!.Title} {episode.Metadata.EpisodeNumber}";
        } else {
            episodeName = episode.Series!.Metadata!.Title;
        }
        string tags = new([.. episode.Metadata.Filename.SkipWhile(c => c != '[').TakeWhile(c => c != '.')]);
        if (!string.IsNullOrWhiteSpace(tags)) {
            episodeName = $"{episodeName} - {tags}";
        }
        if (mkdir) {
            _ = Directory.CreateDirectory(seriesDir);
        }
        char[] invalidChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*', .. Path.GetInvalidFileNameChars()];
        return Path.Join(seriesDir, string.Concat(episodeName.Split(invalidChars)) + Path.GetExtension(episode.Metadata.Filename));
    }

    public static string? FilePath(this Series series, bool mkdir = true) {
        string? libraryPath = series.Metadata!.Category switch {
            RecordingCategory.Movie => Plugin.Instance?.Configuration.MovieRecordingPath,
            RecordingCategory.Series => Plugin.Instance?.Configuration.SeriesRecordingPath,
            _ => null,
        };
        if (string.IsNullOrWhiteSpace(libraryPath)) {
            return null;
        }
        char[] invalidChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*', .. Path.GetInvalidFileNameChars()];
        string seriesDir = Path.Join(libraryPath, string.Concat($"{series.Metadata.Title} [{series.Metadata.SeriesId}]".Split(invalidChars)));
        if (mkdir) {
            _ = Directory.CreateDirectory(seriesDir);
        }
        return seriesDir;
    }
}
