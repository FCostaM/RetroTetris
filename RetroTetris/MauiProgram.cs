using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using RetroTetris.Core.Commands;
using RetroTetris.Core.Engine;
using RetroTetris.Core.Interfaces;
using RetroTetris.Pages;
using RetroTetris.Presentation;
using RetroTetris.Services;

namespace RetroTetris;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("PressStart2P-Regular.ttf", "PressStart2P");
            });

        #if DEBUG
        builder.Logging.AddDebug();
        #endif

        // ─── Core game services ────────────────────────────────────────────────

        // Register Plugin.Maui.Audio's IAudioManager (must be called on builder, not builder.Services)
        builder.AddAudio();

        // Game engine (singleton — single source of truth for game state)
        builder.Services.AddSingleton<IGameEngine, GameEngine>();

        // Infrastructure services
        builder.Services.AddSingleton<IHighScoreRepository, HighScoreRepository>();
        builder.Services.AddSingleton<IAudioService, AudioService>();

        // Command queue (thread-safe bridge between input and game loop)
        builder.Services.AddSingleton<CommandQueue>();

        // Game loop (runs on its own background thread)
        builder.Services.AddSingleton<GameLoop>();

        // ─── Presentation layer ────────────────────────────────────────────────

        builder.Services.AddSingleton<LayoutManager>();
        builder.Services.AddSingleton<GameRenderer>();
        builder.Services.AddSingleton<InputHandler>();

        // ─── Pages ────────────────────────────────────────────────────────────

        builder.Services.AddSingleton<GamePage>();

        // ─── Build the app ────────────────────────────────────────────────────

        var app = builder.Build();

        // ─── Wire Observer events ─────────────────────────────────────────────
        // After building, resolve singletons and subscribe to GameEngine events.

        var engine = app.Services.GetRequiredService<IGameEngine>();
        var audioService = app.Services.GetRequiredService<IAudioService>();
        var highScoreRepo = app.Services.GetRequiredService<IHighScoreRepository>();
        var gameRenderer = app.Services.GetRequiredService<GameRenderer>();

        // Wire GameRenderer to the engine (so it can read state during Draw)
        gameRenderer.SetGameEngine(engine);

        // Load the persisted high score into the engine
        _ = LoadHighScoreAsync(engine, highScoreRepo);

        // AudioService reacts to game events
        engine.PieceLocked    += () => _ = audioService.PlaySfxAsync(SoundEffect.Lock);
        engine.PieceSpawned   += type => _ = audioService.PlaySfxAsync(SoundEffect.Move);
        engine.LinesCleared   += (count, level) =>
        {
            var effect = count >= 4 ? SoundEffect.Tetris : SoundEffect.LineClear;
            _ = audioService.PlaySfxAsync(effect);
        };
        engine.GameOverOccurred += () =>
        {
            _ = audioService.PlaySfxAsync(SoundEffect.GameOver);
            _ = audioService.StopMusicAsync();
        };

        // HighScoreRepository saves when game ends (if score beats record)
        engine.GameOverOccurred += () =>
        {
            // Engine already updated HighScore in TransitionTo(GameOverState)
            _ = highScoreRepo.SaveAsync(engine.HighScore);
        };

        // GameRenderer invalidation triggers on score/level/piece events
        // (The GameLoop already calls OnRender each frame, so no extra wiring needed here.
        //  The renderer reads state directly from the engine during Draw().)

        return app;
    }

    /// <summary>
    /// Loads the persisted high score and injects it into the engine.
    /// The engine exposes HighScore as a settable property via the concrete class.
    /// </summary>
    private static async Task LoadHighScoreAsync(IGameEngine engine, IHighScoreRepository repo)
    {
        try
        {
            var score = await repo.LoadAsync();
            if (engine is GameEngine concreteEngine)
                concreteEngine.HighScore = score;
        }
        catch
        {
            // Silently ignore — high score defaults to 0
        }
    }
}
