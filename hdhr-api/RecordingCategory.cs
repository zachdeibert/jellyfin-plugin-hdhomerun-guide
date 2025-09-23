namespace Com.ZachDeibert.MediaTools.Hdhr.Api;

public enum RecordingCategory {
    Movie,
    Series
}

public static class RecordingCategoryExtensions {
    public static RecordingCategory? ToRecordingCategory(this string category) => category switch {
        "movie" => (RecordingCategory?) RecordingCategory.Movie,
        "series" => (RecordingCategory?) RecordingCategory.Series,
        _ => null,
    };
}
