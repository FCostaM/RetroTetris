namespace RetroTetris.Core.Interfaces;

public interface IAudioService
{
    Task PlaySfxAsync(SoundEffect effect);
    Task StartMusicAsync();
    Task StopMusicAsync();
    void SetMuted(bool muted);
}

public enum SoundEffect { Move, Rotate, Lock, LineClear, Tetris, GameOver }
