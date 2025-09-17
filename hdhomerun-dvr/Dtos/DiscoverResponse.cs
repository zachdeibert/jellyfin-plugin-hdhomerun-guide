namespace Com.ZachDeibert.JellyfinPluginHDHomeRunDVR.Dtos;

internal class DiscoverResponse {
    public string? FriendlyName { get; set; }
    public string? ModelNumber { get; set; }
    public string? FirmwareName { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? DeviceID { get; set; }
    public string? DeviceAuth { get; set; }
    public string? BaseURL { get; set; }
    public string? LineupURL { get; set; }
    public int TunerCount { get; set; }
    public string? StorageID { get; set; }
    public string? StorageURL { get; set; }
    public long TotalSpace { get; set; }
    public long FreeSpace { get; set; }
}
