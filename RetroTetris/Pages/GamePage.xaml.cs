using RetroTetris.Core.Commands;
using RetroTetris.Core.Engine;
using RetroTetris.Core.Interfaces;
using RetroTetris.Core.States;
using RetroTetris.Presentation;
using CoreSwipeDirection = RetroTetris.Core.Interfaces.SwipeDirection;

#if WINDOWS
using Microsoft.UI.Xaml.Input;
using Windows.System;
#endif

namespace RetroTetris.Pages;

/// <summary>
/// Main game page. Hosts the GraphicsView, wires up keyboard/touch input,
/// starts the GameLoop on appear and stops it on disappear.
/// The page handles all game states (Start, Playing, Paused, GameOver) via
/// the GameRenderer — no separate StartPage is needed.
/// </summary>
public partial class GamePage : ContentPage
{
    private readonly GameRenderer _renderer;
    private readonly InputHandler _inputHandler;
    private readonly GameLoop _gameLoop;
    private readonly IGameEngine _engine;
    private readonly LayoutManager _layout;

    // Touch gesture tracking
    private double _panStartX;
    private double _panStartY;
    private const double SwipeThreshold = 40.0;

    public GamePage(
        GameRenderer renderer,
        InputHandler inputHandler,
        GameLoop gameLoop,
        IGameEngine engine,
        LayoutManager layout)
    {
        InitializeComponent();

        _renderer = renderer;
        _inputHandler = inputHandler;
        _gameLoop = gameLoop;
        _engine = engine;
        _layout = layout;

        // Wire renderer to canvas
        GameCanvas.Drawable = _renderer;

        // Wire game loop render callback to invalidate the canvas
        _gameLoop.OnRender = () =>
            MainThread.BeginInvokeOnMainThread(() => GameCanvas.Invalidate());

        // Wire InputHandler to GameLoop for DAS/ARR
        _gameLoop.InputHandler = _inputHandler;

        // Wire touch gestures
        SetupTouchGestures();
    }

    // ─── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Start the game loop so the start screen renders.
        // The engine starts in StartScreenState — StartNewGame() is called
        // only when the player presses the Start button.
        _gameLoop.Start();

        // Register keyboard handler on Windows
        RegisterKeyboardHandler();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _gameLoop.Stop();
        UnregisterKeyboardHandler();
    }

    // ─── Keyboard (Windows / WinUI 3) ──────────────────────────────────────────

    private void RegisterKeyboardHandler()
    {
#if WINDOWS
        if (Window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window winUiWindow)
        {
            winUiWindow.Content.KeyDown += OnWinUiKeyDown;
            winUiWindow.Content.KeyUp   += OnWinUiKeyUp;
        }
#endif
    }

    private void UnregisterKeyboardHandler()
    {
#if WINDOWS
        if (Window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window winUiWindow)
        {
            winUiWindow.Content.KeyDown -= OnWinUiKeyDown;
            winUiWindow.Content.KeyUp   -= OnWinUiKeyUp;
        }
#endif
    }

#if WINDOWS
    private void OnWinUiKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var key = MapVirtualKey(e.Key);
        if (key.HasValue)
            _inputHandler.OnKeyDown(key.Value);

        // Handle Enter/Space on start screen to begin the game
        if (e.Key is VirtualKey.Enter or VirtualKey.Space)
        {
            if (_engine.CurrentState is StartScreenState)
                _engine.StartNewGame();
        }
    }

    private void OnWinUiKeyUp(object sender, KeyRoutedEventArgs e)
    {
        var key = MapVirtualKey(e.Key);
        if (key.HasValue)
            _inputHandler.OnKeyUp(key.Value);
    }

    private static GameKey? MapVirtualKey(VirtualKey vk) => vk switch
    {
        VirtualKey.Left     => GameKey.Left,
        VirtualKey.Right    => GameKey.Right,
        VirtualKey.Down     => GameKey.Down,
        VirtualKey.Up       => GameKey.Up,
        VirtualKey.Space    => GameKey.Space,
        VirtualKey.Z        => GameKey.Z,
        VirtualKey.X        => GameKey.X,
        VirtualKey.C        => GameKey.C,
        VirtualKey.Shift    => GameKey.Shift,
        VirtualKey.Escape   => GameKey.Escape,
        VirtualKey.P        => GameKey.P,
        VirtualKey.H        => GameKey.H,
        VirtualKey.Enter    => GameKey.Space,  // Enter = hard drop during play
        _ => null
    };
#endif

    // ─── Touch gestures ────────────────────────────────────────────────────────

    private void SetupTouchGestures()
    {
        // Tap gesture — handles start/game-over buttons and in-game rotation
        var tap = new TapGestureRecognizer();
        tap.Tapped += OnTapped;
        GameCanvas.GestureRecognizers.Add(tap);

        // Pan gesture — swipe detection
        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnPanUpdated;
        GameCanvas.GestureRecognizers.Add(pan);
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        var pos = e.GetPosition(GameCanvas);
        if (pos is null) return;

        float tapX = (float)pos.Value.X;
        float tapY = (float)pos.Value.Y;

        var state = _engine.CurrentState;

        // ── Controls screen: tap the BACK button ────────────────────────────
        if (state is ControlsScreenState)
        {
            if (IsBackButtonHit(tapX, tapY))
                _engine.HideControls();
            return;
        }

        // ── Start screen: tap the Start or Controls button ──────────────────
        if (state is StartScreenState)
        {
            if (IsStartButtonHit(tapX, tapY))
                _engine.StartNewGame();
            else if (IsControlsButtonHit(tapX, tapY))
                _engine.ShowControls();
            return;
        }

        // ── Paused: tap the Controls button ─────────────────────────────────
        if (state is PausedState)
        {
            if (IsControlsButtonHitOnPause(tapX, tapY))
                _engine.ShowControls();
            return;
        }

        // ── Game over screen: tap Try Again or Exit ──────────────────────────
        if (state is GameOverState)
        {
            if (IsTryAgainButtonHit(tapX, tapY))
            {
                _engine.StartNewGame();
            }
            else if (IsExitButtonHit(tapX, tapY))
            {
                Application.Current?.Quit();
            }
            return;
        }

        // ── Playing: tap left/right half to rotate ───────────────────────────
        if (state is PlayingState)
        {
            var side = tapX < GameCanvas.Width / 2
                ? TapSide.Left
                : TapSide.Right;
            _inputHandler.OnTap(side);
        }
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panStartX = e.TotalX;
                _panStartY = e.TotalY;
                break;

            case GestureStatus.Completed:
                double dx = e.TotalX - _panStartX;
                double dy = e.TotalY - _panStartY;

                if (Math.Abs(dx) > Math.Abs(dy))
                {
                    // Horizontal swipe
                    if (Math.Abs(dx) >= SwipeThreshold)
                        _inputHandler.OnSwipe(dx < 0 ? CoreSwipeDirection.Left : CoreSwipeDirection.Right);
                }
                else
                {
                    // Vertical swipe
                    if (Math.Abs(dy) >= SwipeThreshold)
                        _inputHandler.OnSwipe(dy < 0 ? CoreSwipeDirection.Up : CoreSwipeDirection.Down);
                }
                break;
        }
    }

    // ─── Button hit-testing ────────────────────────────────────────────────────
    // These mirror the button positions calculated in GameRenderer.

    private bool IsStartButtonHit(float x, float y)
    {
        float w = (float)GameCanvas.Width;
        float h = (float)GameCanvas.Height;
        float btnW = w * 0.3f;
        float btnH = h * 0.08f;
        float btnX = (w - btnW) / 2f;
        float btnY = h * 0.58f;
        return x >= btnX && x <= btnX + btnW && y >= btnY && y <= btnY + btnH;
    }

    private bool IsTryAgainButtonHit(float x, float y)
    {
        var boardRect = _layout.BoardRect;
        float btnW = boardRect.Width * 0.7f;
        float btnH = Math.Max(20, boardRect.Height * 0.09f);
        float centerY = boardRect.Y + boardRect.Height * 0.72f;
        float btnX = boardRect.X + (boardRect.Width - btnW) / 2f;
        float btnY = centerY - btnH / 2f;
        return x >= btnX && x <= btnX + btnW && y >= btnY && y <= btnY + btnH;
    }

    private bool IsExitButtonHit(float x, float y)
    {
        var boardRect = _layout.BoardRect;
        float btnW = boardRect.Width * 0.7f;
        float btnH = Math.Max(20, boardRect.Height * 0.09f);
        float centerY = boardRect.Y + boardRect.Height * 0.84f;
        float btnX = boardRect.X + (boardRect.Width - btnW) / 2f;
        float btnY = centerY - btnH / 2f;
        return x >= btnX && x <= btnX + btnW && y >= btnY && y <= btnY + btnH;
    }

    /// <summary>
    /// Mirrors DrawControlsButton() in GameRenderer — btnY = h * 0.68f.
    /// </summary>
    private bool IsControlsButtonHit(float x, float y)
    {
        float w = (float)GameCanvas.Width;
        float h = (float)GameCanvas.Height;
        float btnW = w * 0.3f;
        float btnH = h * 0.08f;
        float btnX = (w - btnW) / 2f;
        float btnY = h * 0.68f;
        return x >= btnX && x <= btnX + btnW && y >= btnY && y <= btnY + btnH;
    }

    /// <summary>
    /// Mirrors DrawControlsButtonOnPause() in GameRenderer — centerY = r.Y + r.Height * 0.62f.
    /// </summary>
    private bool IsControlsButtonHitOnPause(float x, float y)
    {
        var r = _layout.BoardRect;
        float btnW = r.Width * 0.7f;
        float btnH = Math.Max(20, r.Height * 0.09f);
        float centerY = r.Y + r.Height * 0.62f;
        float btnX = r.X + (r.Width - btnW) / 2f;
        float btnY = centerY - btnH / 2f;
        return x >= btnX && x <= btnX + btnW && y >= btnY && y <= btnY + btnH;
    }

    /// <summary>
    /// Mirrors DrawBackButton() in GameRenderer — centerY = r.Y + r.Height * 0.91f.
    /// </summary>
    private bool IsBackButtonHit(float x, float y)
    {
        var r = _layout.BoardRect;
        float btnW = r.Width * 0.5f;
        float btnH = Math.Max(18, r.Height * 0.08f);
        float centerY = r.Y + r.Height * 0.91f;
        float btnX = r.X + (r.Width - btnW) / 2f;
        float btnY = centerY - btnH / 2f;
        return x >= btnX && x <= btnX + btnW && y >= btnY && y <= btnY + btnH;
    }

    // ─── Size changes ──────────────────────────────────────────────────────────

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        GameCanvas.Invalidate();
    }
}
