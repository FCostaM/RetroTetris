using Microsoft.Maui.Graphics;
using RetroTetris.Core.Engine;
using RetroTetris.Core.Interfaces;
using RetroTetris.Core.States;
using MauiFont = Microsoft.Maui.Graphics.Font;

namespace RetroTetris.Presentation;

/// <summary>
/// Renders the entire game state onto an ICanvas using MAUI's GraphicsView.
/// Implements IDrawable (called by GraphicsView) and IGameRenderer (called by GameEngine events).
/// Covers: board, active piece, ghost piece, line-clear flash, hold/next panels,
/// score/level/highscore labels, start screen, pause overlay, and game-over screen.
/// </summary>
public class GameRenderer : IDrawable, IGameRenderer
{
    // ─── Dependencies ──────────────────────────────────────────────────────────

    private readonly LayoutManager _layout;
    private IGameEngine? _engine;

    // ─── Flash animation state ─────────────────────────────────────────────────

    private IReadOnlyList<int> _flashingLines = Array.Empty<int>();
    private DateTime _flashStart = DateTime.MinValue;
    private const double FlashDurationMs = 200.0;

    // ─── Game-over glitch animation ────────────────────────────────────────────

    private DateTime _gameOverStart = DateTime.MinValue;

    // ─── Colors ────────────────────────────────────────────────────────────────

    private static readonly Color Background    = Color.FromArgb("#0A0A0A");
    private static readonly Color BoardBorder   = Color.FromArgb("#444444");
    private static readonly Color EmptyCell     = Color.FromArgb("#111111");
    private static readonly Color EmptyCellGrid = Color.FromArgb("#1A1A1A");
    private static readonly Color TextColor     = Color.FromArgb("#EEEEEE");
    private static readonly Color PanelBg       = Color.FromArgb("#161616");
    private static readonly Color PanelBorder   = Color.FromArgb("#333333");
    private static readonly Color GhostAlpha    = Color.FromArgb("#44FFFFFF");
    private static readonly Color FlashColor    = Color.FromArgb("#FFFFFF");
    private static readonly Color GameOverRed   = Color.FromArgb("#FF2222");
    private static readonly Color OverlayBg     = Color.FromArgb("#CC000000");
    private static readonly Color ButtonBg      = Color.FromArgb("#222222");
    private static readonly Color ButtonBorder  = Color.FromArgb("#888888");

    // ─── Font name ─────────────────────────────────────────────────────────────

    private const string PixelFont = "PressStart2P";

    // ─── Controls screen data ──────────────────────────────────────────────────

    private static readonly (string key, string action)[] _keyboardEntries =
    {
        ("←",         "MOVE LEFT"),
        ("→",         "MOVE RIGHT"),
        ("↓",         "SOFT DROP"),
        ("SPACE",     "HARD DROP"),
        ("↑ / Z",     "ROTATE CW"),
        ("X",         "ROTATE CCW"),
        ("C / SHIFT", "HOLD"),
        ("P / ESC",   "PAUSE/RESUME"),
        ("H",         "CONTROLS"),
    };

    private static readonly (string key, string action)[] _touchEntries =
    {
        ("SWIPE ←", "MOVE LEFT"),
        ("SWIPE →", "MOVE RIGHT"),
        ("SWIPE ↓", "SOFT DROP"),
        ("SWIPE ↑", "HARD DROP"),
        ("TAP ←",   "ROTATE CCW"),
        ("TAP →",   "ROTATE CW"),
    };

    // ─── Constructor ───────────────────────────────────────────────────────────

    public GameRenderer(LayoutManager layout)
    {
        _layout = layout;
    }

    // ─── IGameRenderer ─────────────────────────────────────────────────────────

    public void SetGameEngine(IGameEngine engine)
    {
        _engine = engine;
        engine.GameOverOccurred += () => _gameOverStart = DateTime.UtcNow;
    }

    public void SetFlashingLines(IReadOnlyList<int> lines)
    {
        _flashingLines = lines;
        _flashStart = DateTime.UtcNow;
    }

    // ─── IDrawable.Draw ────────────────────────────────────────────────────────

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (_engine is null) return;

        // Update layout every frame (handles resize)
        _layout.Update(dirtyRect.Width, dirtyRect.Height);

        // Fill background
        canvas.FillColor = Background;
        canvas.FillRectangle(dirtyRect);

        var state = _engine.CurrentState;

        if (state is StartScreenState)
        {
            DrawStartScreen(canvas, dirtyRect);
            return;
        }

        // ControlsScreenState com PreviousState = StartScreenState
        if (state is ControlsScreenState { PreviousState: StartScreenState })
        {
            DrawStartScreen(canvas, dirtyRect);
            DrawControlsOverlay(canvas, dirtyRect);
            return;
        }

        // Draw board area
        DrawBoardBackground(canvas);
        DrawBoardCells(canvas);
        DrawGhostPiece(canvas);
        DrawActivePiece(canvas);
        DrawFlashingLines(canvas);
        DrawBoardBorder(canvas);

        // Draw side panels
        DrawHoldPanel(canvas);
        DrawNextPanel(canvas);
        DrawScorePanel(canvas);
        DrawLevelPanel(canvas);

        // Overlays
        if (state is PausedState)
            DrawPauseOverlay(canvas, dirtyRect);
        else if (state is GameOverState)
            DrawGameOverOverlay(canvas, dirtyRect);
        else if (state is ControlsScreenState { PreviousState: PausedState })
        {
            DrawPauseOverlay(canvas, dirtyRect);
            DrawControlsOverlay(canvas, dirtyRect);
        }
    }

    // ─── Start Screen ──────────────────────────────────────────────────────────

    private void DrawStartScreen(ICanvas canvas, RectF bounds)
    {
        // Title
        canvas.FontColor = Color.FromArgb("#00F0F0");
        canvas.FontSize = Math.Max(12, bounds.Width * 0.04f);
        canvas.Font = new MauiFont(PixelFont);
        canvas.DrawString("RETRO TETRIS", bounds.Width / 2f, bounds.Height * 0.35f,
            HorizontalAlignment.Center);

        // Subtitle
        canvas.FontColor = TextColor;
        canvas.FontSize = Math.Max(8, bounds.Width * 0.02f);
        canvas.DrawString("A .NET MAUI GAME", bounds.Width / 2f, bounds.Height * 0.45f,
            HorizontalAlignment.Center);

        // Start button
        float btnW = bounds.Width * 0.3f;
        float btnH = bounds.Height * 0.08f;
        float btnX = (bounds.Width - btnW) / 2f;
        float btnY = bounds.Height * 0.58f;

        canvas.FillColor = ButtonBg;
        canvas.FillRectangle(btnX, btnY, btnW, btnH);
        canvas.StrokeColor = Color.FromArgb("#00F0F0");
        canvas.StrokeSize = 2;
        canvas.DrawRectangle(btnX, btnY, btnW, btnH);

        canvas.FontColor = Color.FromArgb("#00F0F0");
        canvas.FontSize = Math.Max(8, bounds.Width * 0.022f);
        canvas.DrawString("START", btnX + btnW / 2f, btnY + btnH / 2f,
            HorizontalAlignment.Center);

        // Controls hint
        canvas.FontColor = Color.FromArgb("#666666");
        canvas.FontSize = Math.Max(6, bounds.Width * 0.013f);
        canvas.DrawString("PRESS ENTER OR CLICK START", bounds.Width / 2f, bounds.Height * 0.75f,
            HorizontalAlignment.Center);

        // Controls button
        DrawControlsButton(canvas, bounds);
    }

    // ─── Controls button (Start Screen) ───────────────────────────────────────

    private void DrawControlsButton(ICanvas canvas, RectF bounds)
    {
        float btnW = bounds.Width * 0.3f;
        float btnH = bounds.Height * 0.08f;
        float btnX = (bounds.Width - btnW) / 2f;
        float btnY = bounds.Height * 0.68f;

        canvas.FillColor = ButtonBg;
        canvas.FillRectangle(btnX, btnY, btnW, btnH);
        canvas.StrokeColor = Color.FromArgb("#888888");
        canvas.StrokeSize = 2;
        canvas.DrawRectangle(btnX, btnY, btnW, btnH);

        canvas.FontColor = Color.FromArgb("#888888");
        canvas.FontSize = Math.Max(8, bounds.Width * 0.022f);
        canvas.Font = new MauiFont(PixelFont);
        canvas.DrawString("CONTROLS", btnX + btnW / 2f, btnY + btnH / 2f,
            HorizontalAlignment.Center);
    }

    // ─── Board background ──────────────────────────────────────────────────────

    private void DrawBoardBackground(ICanvas canvas)
    {
        var r = _layout.BoardRect;
        canvas.FillColor = Color.FromArgb("#080808");
        canvas.FillRectangle(r);

        // Draw grid lines
        canvas.StrokeColor = EmptyCellGrid;
        canvas.StrokeSize = 0.5f;
        float cs = _layout.CellSize;

        for (int row = 0; row <= Board.VisibleRows; row++)
        {
            float y = r.Y + row * cs;
            canvas.DrawLine(r.X, y, r.X + r.Width, y);
        }
        for (int col = 0; col <= Board.Columns; col++)
        {
            float x = r.X + col * cs;
            canvas.DrawLine(x, r.Y, x, r.Y + r.Height);
        }
    }

    private void DrawBoardBorder(ICanvas canvas)
    {
        var r = _layout.BoardRect;
        // Outer glow border (retro style)
        canvas.StrokeColor = Color.FromArgb("#00F0F0");
        canvas.StrokeSize = 2;
        canvas.DrawRectangle(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4);
        canvas.StrokeColor = BoardBorder;
        canvas.StrokeSize = 1;
        canvas.DrawRectangle(r.X - 3, r.Y - 3, r.Width + 6, r.Height + 6);
    }

    // ─── Board cells ───────────────────────────────────────────────────────────

    private void DrawBoardCells(ICanvas canvas)
    {
        if (_engine?.Board is null) return;
        var board = _engine.Board;

        for (int row = Board.BufferRows; row < Board.TotalRows; row++)
        {
            for (int col = 0; col < Board.Columns; col++)
            {
                var cell = board.GetCell(row, col);
                if (cell.HasValue)
                    DrawFilledCell(canvas, row, col, cell.Value, 1f);
            }
        }
    }

    // ─── Ghost piece ───────────────────────────────────────────────────────────

    private void DrawGhostPiece(ICanvas canvas)
    {
        var ghost = _engine?.GhostPiece;
        var active = _engine?.ActivePiece;
        if (ghost is null || active is null) return;
        if (ghost.Row == active.Row) return; // same position — don't draw ghost

        var hexColor = TetrominoColors.Primary[ghost.Type];
        var color = Color.FromArgb(hexColor);
        var ghostColor = Color.FromRgba(color.Red, color.Green, color.Blue, 60f / 255f);

        foreach (var (r, c) in ghost.GetCells())
        {
            if (r < Board.BufferRows) continue;
            var rect = _layout.GetCellRect(r, c);
            // Draw outline only
            canvas.StrokeColor = color.WithAlpha(0.4f);
            canvas.StrokeSize = 1.5f;
            canvas.DrawRectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2);
        }
    }

    // ─── Active piece ──────────────────────────────────────────────────────────

    private void DrawActivePiece(ICanvas canvas)
    {
        var piece = _engine?.ActivePiece;
        if (piece is null) return;

        foreach (var (r, c) in piece.GetCells())
        {
            if (r < Board.BufferRows) continue;
            DrawFilledCell(canvas, r, c, piece.Type, 1f);
        }
    }

    // ─── Flash animation ───────────────────────────────────────────────────────

    private void DrawFlashingLines(ICanvas canvas)
    {
        if (_flashingLines.Count == 0) return;

        double elapsed = (DateTime.UtcNow - _flashStart).TotalMilliseconds;
        if (elapsed > FlashDurationMs)
        {
            _flashingLines = Array.Empty<int>();
            return;
        }

        // Alternate flash every 50ms
        bool visible = ((int)(elapsed / 50)) % 2 == 0;
        if (!visible) return;

        foreach (int row in _flashingLines)
        {
            if (row < Board.BufferRows) continue;
            for (int col = 0; col < Board.Columns; col++)
            {
                var rect = _layout.GetCellRect(row, col);
                canvas.FillColor = FlashColor;
                canvas.FillRectangle(rect.X, rect.Y, rect.Width, rect.Height);
            }
        }
    }

    // ─── Cell drawing helper ───────────────────────────────────────────────────

    private void DrawFilledCell(ICanvas canvas, int row, int col, TetrominoType type, float alpha)
    {
        var rect = _layout.GetCellRect(row, col);
        var hexColor = TetrominoColors.Primary[type];
        var color = Color.FromArgb(hexColor).WithAlpha(alpha);

        float inset = 1f;
        float x = rect.X + inset;
        float y = rect.Y + inset;
        float w = rect.Width - 2 * inset;
        float h = rect.Height - 2 * inset;

        // Fill
        canvas.FillColor = color;
        canvas.FillRectangle(x, y, w, h);

        // Highlight (top-left bevel)
        canvas.FillColor = Color.FromRgba(1f, 1f, 1f, 80f / 255f);
        canvas.FillRectangle(x, y, w, 2);
        canvas.FillRectangle(x, y, 2, h);

        // Shadow (bottom-right bevel)
        canvas.FillColor = Color.FromRgba(0f, 0f, 0f, 80f / 255f);
        canvas.FillRectangle(x, y + h - 2, w, 2);
        canvas.FillRectangle(x + w - 2, y, 2, h);

        // Border
        canvas.StrokeColor = color.WithAlpha(0.6f);
        canvas.StrokeSize = 0.5f;
        canvas.DrawRectangle(x, y, w, h);
    }

    // ─── Hold panel ────────────────────────────────────────────────────────────

    private void DrawHoldPanel(ICanvas canvas)
    {
        var r = _layout.HoldRect;
        DrawPanel(canvas, r, "HOLD");

        var hold = _engine?.HoldPiece;
        if (hold is not null)
            DrawMiniPiece(canvas, hold.Type, r, 0.5f);
    }

    // ─── Next panel ────────────────────────────────────────────────────────────

    private void DrawNextPanel(ICanvas canvas)
    {
        var r = _layout.NextRect;
        DrawPanel(canvas, r, "NEXT");

        var nextPieces = _engine?.NextPieces;
        if (nextPieces is null) return;

        float slotH = (r.Height - 20) / 3f;
        for (int i = 0; i < Math.Min(3, nextPieces.Count); i++)
        {
            var slotRect = new RectF(r.X, r.Y + 20 + i * slotH, r.Width, slotH);
            DrawMiniPiece(canvas, nextPieces[i].Type, slotRect, 0.4f);
        }
    }

    // ─── Score panel ───────────────────────────────────────────────────────────

    private void DrawScorePanel(ICanvas canvas)
    {
        DrawPanel(canvas, _layout.ScoreRect, "SCORE");
        DrawPanelValue(canvas, _layout.ScoreRect, (_engine?.Score ?? 0).ToString());

        DrawPanel(canvas, _layout.HighScoreRect, "BEST");
        DrawPanelValue(canvas, _layout.HighScoreRect, (_engine?.HighScore ?? 0).ToString());
    }

    // ─── Level panel ───────────────────────────────────────────────────────────

    private void DrawLevelPanel(ICanvas canvas)
    {
        DrawPanel(canvas, _layout.LevelRect, "LEVEL");
        DrawPanelValue(canvas, _layout.LevelRect, (_engine?.Level ?? 1).ToString());
    }

    // ─── Panel helpers ─────────────────────────────────────────────────────────

    private void DrawPanel(ICanvas canvas, RectF r, string label)
    {
        canvas.FillColor = PanelBg;
        canvas.FillRectangle(r);
        canvas.StrokeColor = PanelBorder;
        canvas.StrokeSize = 1;
        canvas.DrawRectangle(r);

        float fontSize = Math.Max(5, r.Width * 0.12f);
        canvas.FontColor = Color.FromArgb("#888888");
        canvas.FontSize = fontSize;
        canvas.Font = new MauiFont(PixelFont);
        canvas.DrawString(label, r.X + r.Width / 2f, r.Y + fontSize + 2,
            HorizontalAlignment.Center);
    }

    private void DrawPanelValue(ICanvas canvas, RectF r, string value)
    {
        float fontSize = Math.Max(6, r.Width * 0.14f);
        canvas.FontColor = TextColor;
        canvas.FontSize = fontSize;
        canvas.Font = new MauiFont(PixelFont);
        canvas.DrawString(value, r.X + r.Width / 2f, r.Y + r.Height * 0.65f,
            HorizontalAlignment.Center);
    }

    // ─── Mini piece (for Hold/Next panels) ────────────────────────────────────

    private void DrawMiniPiece(ICanvas canvas, TetrominoType type, RectF panelRect, float cellRatio)
    {
        float cs = _layout.CellSize * cellRatio;
        var cells = GetPieceCells(type);

        // Find bounding box
        int minR = cells.Min(c => c.row), maxR = cells.Max(c => c.row);
        int minC = cells.Min(c => c.col), maxC = cells.Max(c => c.col);
        float pieceW = (maxC - minC + 1) * cs;
        float pieceH = (maxR - minR + 1) * cs;

        float startX = panelRect.X + (panelRect.Width - pieceW) / 2f;
        float startY = panelRect.Y + (panelRect.Height - pieceH) / 2f;

        var hexColor = TetrominoColors.Primary[type];
        var color = Color.FromArgb(hexColor);

        foreach (var (r, c) in cells)
        {
            float x = startX + (c - minC) * cs + 1;
            float y = startY + (r - minR) * cs + 1;
            float w = cs - 2;
            float h = cs - 2;

            canvas.FillColor = color;
            canvas.FillRectangle(x, y, w, h);
            canvas.FillColor = Color.FromRgba(1f, 1f, 1f, 60f / 255f);
            canvas.FillRectangle(x, y, w, 2);
            canvas.FillRectangle(x, y, 2, h);
        }
    }

    private static IReadOnlyList<(int row, int col)> GetPieceCells(TetrominoType type)
    {
        var piece = Tetromino.Create(type);
        return piece.GetCells();
    }

    // ─── Pause overlay ─────────────────────────────────────────────────────────

    private void DrawPauseOverlay(ICanvas canvas, RectF bounds)
    {
        // Semi-transparent overlay over the board
        var r = _layout.BoardRect;
        canvas.FillColor = OverlayBg;
        canvas.FillRectangle(r);

        canvas.FontColor = TextColor;
        canvas.FontSize = Math.Max(10, r.Width * 0.08f);
        canvas.Font = new MauiFont(PixelFont);
        canvas.DrawString("PAUSED", r.X + r.Width / 2f, r.Y + r.Height / 2f - 10,
            HorizontalAlignment.Center);

        canvas.FontSize = Math.Max(6, r.Width * 0.045f);
        canvas.FontColor = Color.FromArgb("#888888");
        canvas.DrawString("PRESS P TO RESUME", r.X + r.Width / 2f, r.Y + r.Height / 2f + 20,
            HorizontalAlignment.Center);

        // Controls button below the pause text
        DrawControlsButtonOnPause(canvas, r);
    }

    // ─── Controls button (Pause overlay) ──────────────────────────────────────

    private void DrawControlsButtonOnPause(ICanvas canvas, RectF boardRect)
    {
        var r = boardRect;
        float btnW = r.Width * 0.7f;
        float btnH = Math.Max(20, r.Height * 0.09f);
        float centerY = r.Y + r.Height * 0.62f;
        float btnX = r.X + (r.Width - btnW) / 2f;
        float btnY = centerY - btnH / 2f;

        canvas.FillColor = ButtonBg;
        canvas.FillRectangle(btnX, btnY, btnW, btnH);
        canvas.StrokeColor = Color.FromArgb("#888888");
        canvas.StrokeSize = 1.5f;
        canvas.DrawRectangle(btnX, btnY, btnW, btnH);

        canvas.FontColor = Color.FromArgb("#888888");
        canvas.FontSize = Math.Max(6, r.Width * 0.045f);
        canvas.Font = new MauiFont(PixelFont);
        canvas.DrawString("CONTROLS", btnX + btnW / 2f, centerY,
            HorizontalAlignment.Center);
    }

    // ─── Controls overlay ─────────────────────────────────────────────────────

    private void DrawControlsOverlay(ICanvas canvas, RectF bounds)
    {
        var r = _layout.BoardRect;

        // Semi-transparent background (consistent with PauseOverlay and GameOverOverlay)
        canvas.FillColor = OverlayBg;
        canvas.FillRectangle(r);

        // Decorative retro border — outer cyan line
        canvas.StrokeColor = Color.FromArgb("#00F0F0");
        canvas.StrokeSize = 2;
        canvas.DrawRectangle(r.X + 4, r.Y + 4, r.Width - 8, r.Height - 8);
        // Inner dark line
        canvas.StrokeColor = Color.FromArgb("#444444");
        canvas.StrokeSize = 1;
        canvas.DrawRectangle(r.X + 7, r.Y + 7, r.Width - 14, r.Height - 14);

        // Title
        canvas.FontColor = Color.FromArgb("#00F0F0");
        canvas.FontSize = Math.Max(8, r.Width * 0.07f);
        canvas.Font = new MauiFont(PixelFont);
        canvas.DrawString("CONTROLS", r.X + r.Width / 2f, r.Y + r.Height * 0.07f,
            HorizontalAlignment.Center);

        // Separator below title
        float separatorY = r.Y + r.Height * 0.12f;
        canvas.StrokeColor = Color.FromArgb("#444444");
        canvas.StrokeSize = 1;
        canvas.DrawLine(r.X + 12, separatorY, r.X + r.Width - 12, separatorY);

        // ── Two-column layout ──────────────────────────────────────────────────
        float colW      = r.Width / 2f;
        float padding   = r.Width * 0.04f;
        float contentY  = separatorY + r.Height * 0.04f;

        // Left column — KEYBOARD
        float leftColX = r.X;
        DrawColumnHeader(canvas, r, "KEYBOARD", leftColX + colW / 2f, contentY);
        float kbListY = contentY + r.Height * 0.06f;
        DrawCommandEntriesColumn(canvas, r, _keyboardEntries, leftColX + padding, leftColX + colW - padding, kbListY);

        // Vertical divider
        float divX = r.X + colW;
        canvas.StrokeColor = Color.FromArgb("#333333");
        canvas.StrokeSize = 1;
        canvas.DrawLine(divX, separatorY + 4, divX, r.Y + r.Height * 0.88f);

        // Right column — TOUCH
        float rightColX = r.X + colW;
        DrawColumnHeader(canvas, r, "TOUCH", rightColX + colW / 2f, contentY);
        float touchListY = contentY + r.Height * 0.06f;
        DrawCommandEntriesColumn(canvas, r, _touchEntries, rightColX + padding, rightColX + colW - padding, touchListY);

        // BACK button — always below both columns
        DrawBackButton(canvas, r);
    }

    private void DrawColumnHeader(ICanvas canvas, RectF boardRect, string title, float centerX, float y)
    {
        canvas.FontColor = Color.FromArgb("#666666");
        canvas.FontSize = Math.Max(5, boardRect.Width * 0.035f);
        canvas.Font = new MauiFont(PixelFont);
        canvas.DrawString(title, centerX, y, HorizontalAlignment.Center);
    }

    private void DrawCommandEntriesColumn(ICanvas canvas, RectF boardRect,
        (string key, string action)[] entries, float leftX, float rightX, float startY)
    {
        float rowH     = boardRect.Height * 0.065f;
        float fontSize = Math.Max(4, boardRect.Width * 0.032f);

        canvas.Font = new MauiFont(PixelFont);

        for (int i = 0; i < entries.Length; i++)
        {
            float y = startY + i * rowH;

            // Key label — left-aligned, cyan
            canvas.FontColor = Color.FromArgb("#00F0F0");
            canvas.FontSize = fontSize;
            canvas.DrawString(entries[i].key, leftX, y, HorizontalAlignment.Left);

            // Action description — right-aligned, light text
            canvas.FontColor = TextColor;
            canvas.FontSize = fontSize;
            canvas.DrawString(entries[i].action, rightX, y, HorizontalAlignment.Right);
        }
    }

    private void DrawCommandEntries(ICanvas canvas, RectF boardRect,
        (string key, string action)[] entries, float startY)
    {
        float rowH     = boardRect.Height * 0.072f;
        float fontSize = Math.Max(5, boardRect.Width * 0.038f);
        float leftX    = boardRect.X + boardRect.Width * 0.08f;
        float rightX   = boardRect.X + boardRect.Width * 0.92f;

        canvas.Font = new MauiFont(PixelFont);

        for (int i = 0; i < entries.Length; i++)
        {
            float y = startY + i * rowH;

            canvas.FontColor = Color.FromArgb("#00F0F0");
            canvas.FontSize = fontSize;
            canvas.DrawString(entries[i].key, leftX, y, HorizontalAlignment.Left);

            canvas.FontColor = TextColor;
            canvas.FontSize = fontSize;
            canvas.DrawString(entries[i].action, rightX, y, HorizontalAlignment.Right);
        }
    }

    private void DrawBackButton(ICanvas canvas, RectF boardRect)
    {
        float btnW    = boardRect.Width * 0.5f;
        float btnH    = Math.Max(18, boardRect.Height * 0.08f);
        float centerY = boardRect.Y + boardRect.Height * 0.91f;
        float btnX    = boardRect.X + (boardRect.Width - btnW) / 2f;
        float btnY    = centerY - btnH / 2f;

        canvas.FillColor = ButtonBg;
        canvas.FillRectangle(btnX, btnY, btnW, btnH);
        canvas.StrokeColor = Color.FromArgb("#888888");
        canvas.StrokeSize = 1.5f;
        canvas.DrawRectangle(btnX, btnY, btnW, btnH);

        canvas.FontColor = Color.FromArgb("#888888");
        canvas.FontSize = Math.Max(6, boardRect.Width * 0.04f);
        canvas.Font = new MauiFont(PixelFont);
        canvas.DrawString("BACK", btnX + btnW / 2f, centerY, HorizontalAlignment.Center);
    }

    // ─── Game Over overlay ─────────────────────────────────────────────────────

    private void DrawGameOverOverlay(ICanvas canvas, RectF bounds)
    {
        var r = _layout.BoardRect;
        canvas.FillColor = OverlayBg;
        canvas.FillRectangle(r);

        // Glitch/flicker effect on "GAME OVER" text
        double elapsed = (DateTime.UtcNow - _gameOverStart).TotalMilliseconds;
        bool flicker = elapsed < 2000 && ((int)(elapsed / 100)) % 3 != 0;

        if (!flicker)
        {
            canvas.FontColor = GameOverRed;
            canvas.FontSize = Math.Max(10, r.Width * 0.09f);
            canvas.Font = new MauiFont(PixelFont);
            canvas.DrawString("GAME OVER", r.X + r.Width / 2f, r.Y + r.Height * 0.25f,
                HorizontalAlignment.Center);
        }

        // Score
        canvas.FontColor = TextColor;
        canvas.FontSize = Math.Max(7, r.Width * 0.05f);
        canvas.DrawString($"SCORE", r.X + r.Width / 2f, r.Y + r.Height * 0.42f,
            HorizontalAlignment.Center);
        canvas.FontColor = Color.FromArgb("#F0F000");
        canvas.FontSize = Math.Max(8, r.Width * 0.06f);
        canvas.DrawString((_engine?.Score ?? 0).ToString(), r.X + r.Width / 2f, r.Y + r.Height * 0.50f,
            HorizontalAlignment.Center);

        // High score
        canvas.FontColor = TextColor;
        canvas.FontSize = Math.Max(6, r.Width * 0.04f);
        canvas.DrawString($"BEST: {_engine?.HighScore ?? 0}", r.X + r.Width / 2f, r.Y + r.Height * 0.60f,
            HorizontalAlignment.Center);

        // Buttons
        DrawButton(canvas, r, "TRY AGAIN", r.Y + r.Height * 0.72f, Color.FromArgb("#00F0F0"));
        DrawButton(canvas, r, "EXIT",      r.Y + r.Height * 0.84f, Color.FromArgb("#888888"));
    }

    private void DrawButton(ICanvas canvas, RectF boardRect, string label, float centerY, Color borderColor)
    {
        float btnW = boardRect.Width * 0.7f;
        float btnH = Math.Max(20, boardRect.Height * 0.09f);
        float btnX = boardRect.X + (boardRect.Width - btnW) / 2f;
        float btnY = centerY - btnH / 2f;

        canvas.FillColor = ButtonBg;
        canvas.FillRectangle(btnX, btnY, btnW, btnH);
        canvas.StrokeColor = borderColor;
        canvas.StrokeSize = 1.5f;
        canvas.DrawRectangle(btnX, btnY, btnW, btnH);

        canvas.FontColor = borderColor;
        canvas.FontSize = Math.Max(6, boardRect.Width * 0.045f);
        canvas.Font = new MauiFont(PixelFont);
        canvas.DrawString(label, btnX + btnW / 2f, centerY, HorizontalAlignment.Center);
    }
}
