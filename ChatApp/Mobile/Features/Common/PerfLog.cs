using System.Diagnostics;

namespace ChatApp.Mobile.Features.Common;

/// <summary>
/// Lightweight timing helper. On Android it writes to logcat under the "BU-PERF" tag so slow
/// screens can be measured on a real device with: adb logcat -s BU-PERF
/// </summary>
public static class PerfLog
{
    public static long Start() => Stopwatch.GetTimestamp();

    public static long ElapsedMs(long startTimestamp) =>
        (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

    public static void Since(string label, long startTimestamp, string? extra = null) =>
        Write(label, ElapsedMs(startTimestamp), extra);

    public static void Write(string label, long elapsedMs, string? extra = null)
    {
#if ANDROID
        Android.Util.Log.Info("BU-PERF", extra == null ? $"{label}: {elapsedMs}ms" : $"{label}: {elapsedMs}ms ({extra})");
#endif
    }
}
