namespace Com.ZachDeibert.JellyfinPluginHDHomeRunDVR;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;

public class SyncTask : IScheduledTask {
    public string Category => "Live TV";

    public string Description => "Downloads new recordings from connected HDHomeRun DVRs";

    public string Key => GetType().FullName!;

    public string Name => "Sync HDHomeRun DVR";

    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken) => Task.CompletedTask;

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [
        new TaskTriggerInfo {
            Type = TaskTriggerInfo.TriggerStartup,
        },
        new TaskTriggerInfo {
            IntervalTicks = TimeSpan.FromMinutes(10).Ticks,
            Type = TaskTriggerInfo.TriggerInterval,
        },
    ];
}
