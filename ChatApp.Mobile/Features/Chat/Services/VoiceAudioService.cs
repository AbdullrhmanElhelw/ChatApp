namespace ChatApp.Mobile.Features.Chat.Services;

public class VoiceAudioService : IVoiceAudioService
{
#if ANDROID
    private Android.Media.MediaRecorder? _recorder;
    private Android.Media.MediaPlayer? _player;
#endif
    private static readonly HttpClient _downloadClient = new();
    private string? _currentRecordingPath;
    private DateTime _recordingStartedAt;

    public bool IsRecording { get; private set; }
    public bool IsPlaying { get; private set; }

    public TimeSpan CurrentPosition
    {
        get
        {
#if ANDROID
            if (_player != null && IsPlaying)
            {
                try { return TimeSpan.FromMilliseconds(_player.CurrentPosition); }
                catch { }
            }
#endif
            return TimeSpan.Zero;
        }
    }

    public async Task<bool> StartRecordingAsync()
    {
        if (IsRecording) return false;

        var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Microphone>();
        }

        if (status != PermissionStatus.Granted)
        {
            return false;
        }

        await StopPlaybackAsync();

#if ANDROID
        var fileName = $"voice_{DateTime.UtcNow:yyyyMMddHHmmssfff}.m4a";
        _currentRecordingPath = Path.Combine(FileSystem.CacheDirectory, fileName);

        var context = Android.App.Application.Context;
#pragma warning disable CA1422
        _recorder = OperatingSystem.IsAndroidVersionAtLeast(31)
            ? new Android.Media.MediaRecorder(context)
            : new Android.Media.MediaRecorder();
#pragma warning restore CA1422

        _recorder.SetAudioSource(Android.Media.AudioSource.Mic);
        _recorder.SetOutputFormat(Android.Media.OutputFormat.Mpeg4);
        _recorder.SetAudioEncoder(Android.Media.AudioEncoder.Aac);
        _recorder.SetAudioSamplingRate(44100);
        _recorder.SetAudioEncodingBitRate(96000);
        _recorder.SetOutputFile(_currentRecordingPath);
        _recorder.Prepare();
        _recorder.Start();
#endif

        _recordingStartedAt = DateTime.UtcNow;
        IsRecording = true;
        return true;
    }

    public Task<(string? FilePath, TimeSpan Duration)> StopRecordingAsync(bool cancel = false)
    {
        if (!IsRecording)
        {
            return Task.FromResult<(string?, TimeSpan)>((null, TimeSpan.Zero));
        }

        var duration = DateTime.UtcNow - _recordingStartedAt;
        IsRecording = false;

#if ANDROID
        try
        {
            _recorder?.Stop();
        }
        catch { }
        finally
        {
            _recorder?.Reset();
            _recorder?.Release();
            _recorder = null;
        }
#endif

        var path = _currentRecordingPath;
        _currentRecordingPath = null;

        if (cancel && !string.IsNullOrEmpty(path) && File.Exists(path))
        {
            try { File.Delete(path); } catch { }
            return Task.FromResult<(string?, TimeSpan)>((null, TimeSpan.Zero));
        }

        return Task.FromResult<(string?, TimeSpan)>((path, duration));
    }

    public async Task PlayAsync(string urlOrPath, Action? onCompleted = null)
    {
        await StopPlaybackAsync();

        if (string.IsNullOrWhiteSpace(urlOrPath))
        {
            onCompleted?.Invoke();
            return;
        }

#if ANDROID
        var playablePath = urlOrPath;
        if (urlOrPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var cacheFile = Path.Combine(FileSystem.CacheDirectory, $"play_{Math.Abs(urlOrPath.GetHashCode()):X}.m4a");
            if (!File.Exists(cacheFile) || new FileInfo(cacheFile).Length == 0)
            {
                var bytes = await _downloadClient.GetByteArrayAsync(urlOrPath);
                await File.WriteAllBytesAsync(cacheFile, bytes);
            }
            playablePath = cacheFile;
        }

        _player = new Android.Media.MediaPlayer();
        if (OperatingSystem.IsAndroidVersionAtLeast(21))
        {
            var attrs = new Android.Media.AudioAttributes.Builder()
                .SetUsage(Android.Media.AudioUsageKind.Media)!
                .SetContentType(Android.Media.AudioContentType.Speech)!
                .Build();
            if (attrs != null)
            {
                _player.SetAudioAttributes(attrs);
            }
        }

        _player.Completion += (_, _) =>
        {
            IsPlaying = false;
            MainThread.BeginInvokeOnMainThread(() => onCompleted?.Invoke());
        };
        _player.Error += (_, _) =>
        {
            IsPlaying = false;
            MainThread.BeginInvokeOnMainThread(() => onCompleted?.Invoke());
        };

        await _player.SetDataSourceAsync(playablePath);
        _player.Prepare();
        _player.Start();
        IsPlaying = true;
#else
        IsPlaying = false;
        await Task.CompletedTask;
        onCompleted?.Invoke();
#endif
    }

    public Task StopPlaybackAsync()
    {
#if ANDROID
        if (_player != null)
        {
            try
            {
                if (_player.IsPlaying) _player.Stop();
            }
            catch { }
            finally
            {
                _player.Reset();
                _player.Release();
                _player = null;
            }
        }
#endif
        IsPlaying = false;
        return Task.CompletedTask;
    }
}
