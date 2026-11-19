namespace ChatApp.Mobile.Features.Chat.Services;

public interface IVoiceAudioService
{
    bool IsRecording { get; }
    bool IsPlaying { get; }
    TimeSpan CurrentPosition { get; }

    Task<bool> StartRecordingAsync();
    Task<(string? FilePath, TimeSpan Duration)> StopRecordingAsync(bool cancel = false);
    Task PlayAsync(string urlOrPath, Action? onCompleted = null);
    Task StopPlaybackAsync();
}
