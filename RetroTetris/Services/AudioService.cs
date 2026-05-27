using System.Diagnostics;
using Plugin.Maui.Audio;
using RetroTetris.Core.Interfaces;

namespace RetroTetris.Services;

/// <summary>
/// Implements IAudioService using Plugin.Maui.Audio.
/// All calls are wrapped in try/catch; on first failure, audio is disabled silently.
/// </summary>
public class AudioService : IAudioService
{
    private readonly IAudioManager _audioManager;
    private bool _audioAvailable = true;
    private bool _muted = false;
    private IAudioPlayer? _musicPlayer;

    public AudioService(IAudioManager audioManager)
    {
        _audioManager = audioManager;
    }

    public Task PlaySfxAsync(SoundEffect effect)
    {
        if (!_audioAvailable || _muted) return Task.CompletedTask;

        try
        {
            var fileName = GetSfxFileName(effect);
            var player = _audioManager.CreatePlayer(fileName);
            player.Play();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioService] PlaySfxAsync({effect}) failed: {ex.Message}");
            _audioAvailable = false;
        }

        return Task.CompletedTask;
    }

    public Task StartMusicAsync()
    {
        if (!_audioAvailable || _muted) return Task.CompletedTask;

        try
        {
            _musicPlayer?.Stop();
            _musicPlayer?.Dispose();

            _musicPlayer = _audioManager.CreatePlayer("bgm.mp3");
            _musicPlayer.Loop = true;
            _musicPlayer.Play();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioService] StartMusicAsync failed: {ex.Message}");
            _audioAvailable = false;
        }

        return Task.CompletedTask;
    }

    public Task StopMusicAsync()
    {
        try
        {
            _musicPlayer?.Stop();
            _musicPlayer?.Dispose();
            _musicPlayer = null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioService] StopMusicAsync failed: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public void SetMuted(bool muted)
    {
        _muted = muted;

        if (muted)
        {
            _musicPlayer?.Stop();
        }
        else if (_audioAvailable)
        {
            // Restart music when unmuting — fire and forget
            _ = StartMusicAsync();
        }
    }

    private static string GetSfxFileName(SoundEffect effect) => effect switch
    {
        SoundEffect.Move      => "move.wav",
        SoundEffect.Rotate    => "rotate.wav",
        SoundEffect.Lock      => "lock.wav",
        SoundEffect.LineClear => "lineclear.wav",
        SoundEffect.Tetris    => "lineclear.wav",
        SoundEffect.GameOver  => "gameover.wav",
        _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, null)
    };
}
