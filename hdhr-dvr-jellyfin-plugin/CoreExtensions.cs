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
        string seriesDir = Path.Join(libraryPath, string.Concat(string.Format("{0} ({1})", episode.Series!.Metadata!.Title, episode.Series.Metadata.SeriesId).Split(Path.GetInvalidFileNameChars())));
        if (mkdir) {
            _ = Directory.CreateDirectory(seriesDir);
        }
        return Path.Join(seriesDir, episode.Metadata.Filename);
    }
}
