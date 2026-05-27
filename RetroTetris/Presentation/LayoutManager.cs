using Microsoft.Maui.Graphics;
using RetroTetris.Core.Engine;

namespace RetroTetris.Presentation;

/// <summary>
/// Calculates layout dimensions and positions for all game UI elements
/// based on the available window size. Supports landscape and portrait orientations.
/// </summary>
public class LayoutManager
{
    private const int BoardCols = Board.Columns;     // 10
    private const int BoardRows = Board.VisibleRows; // 20
    private const float SidePanelRatio = 0.2f;
    private const float Padding = 4f;

    public float CellSize { get; private set; }
    public RectF BoardRect { get; private set; }
    public RectF LeftPanelRect { get; private set; }
    public RectF RightPanelRect { get; private set; }
    public bool IsLandscape { get; private set; }

    public RectF HoldRect { get; private set; }
    public RectF ScoreRect { get; private set; }
    public RectF HighScoreRect { get; private set; }
    public RectF NextRect { get; private set; }
    public RectF LevelRect { get; private set; }

    /// <summary>
    /// Recalculates all layout rectangles based on the given total available size.
    /// Call this whenever the window is resized.
    /// </summary>
    public void Update(float totalWidth, float totalHeight)
    {
        IsLandscape = totalWidth >= totalHeight;

        if (IsLandscape)
            CalculateLandscape(totalWidth, totalHeight);
        else
            CalculatePortrait(totalWidth, totalHeight);
    }

    private void CalculateLandscape(float w, float h)
    {
        float sideW = w * SidePanelRatio;
        float boardW = w - 2 * sideW;

        float cellByWidth  = boardW / BoardCols;
        float cellByHeight = h / BoardRows;
        CellSize = Math.Min(cellByWidth, cellByHeight);

        float actualBoardW = CellSize * BoardCols;
        float actualBoardH = CellSize * BoardRows;

        float boardX = sideW + (boardW - actualBoardW) / 2f;
        float boardY = (h - actualBoardH) / 2f;
        BoardRect = new RectF(boardX, boardY, actualBoardW, actualBoardH);

        // Left panel: Hold (top half) + Score/HighScore (bottom half)
        LeftPanelRect = new RectF(0, boardY, sideW - Padding, actualBoardH);
        float halfH = actualBoardH / 2f;
        HoldRect      = new RectF(Padding, boardY + Padding, sideW - 2 * Padding, halfH - 2 * Padding);
        ScoreRect     = new RectF(Padding, boardY + halfH + Padding, sideW - 2 * Padding, halfH / 2f - Padding);
        HighScoreRect = new RectF(Padding, boardY + halfH + halfH / 2f + Padding, sideW - 2 * Padding, halfH / 2f - Padding);

        // Right panel: Next (top 3/4) + Level (bottom 1/4)
        float rightX = boardX + actualBoardW + Padding;
        RightPanelRect = new RectF(rightX, boardY, sideW - Padding, actualBoardH);
        float nextH = actualBoardH * 0.75f;
        NextRect  = new RectF(rightX + Padding, boardY + Padding, sideW - 2 * Padding, nextH - 2 * Padding);
        LevelRect = new RectF(rightX + Padding, boardY + nextH + Padding, sideW - 2 * Padding, actualBoardH * 0.25f - 2 * Padding);
    }

    private void CalculatePortrait(float w, float h)
    {
        float cellByWidth  = w / BoardCols;
        float cellByHeight = (h * 0.7f) / BoardRows;
        CellSize = Math.Min(cellByWidth, cellByHeight);

        float actualBoardW = CellSize * BoardCols;
        float actualBoardH = CellSize * BoardRows;

        float boardX = (w - actualBoardW) / 2f;
        float topPanelH = (h - actualBoardH) * 0.5f;
        float boardY = topPanelH;

        BoardRect = new RectF(boardX, boardY, actualBoardW, actualBoardH);

        float panelW = w / 3f;
        HoldRect      = new RectF(0,         Padding, panelW - Padding, topPanelH - 2 * Padding);
        ScoreRect     = new RectF(panelW,     Padding, panelW - Padding, (topPanelH - 2 * Padding) / 2f);
        HighScoreRect = new RectF(panelW,     Padding + (topPanelH - 2 * Padding) / 2f, panelW - Padding, (topPanelH - 2 * Padding) / 2f);
        NextRect      = new RectF(2 * panelW, Padding, panelW - Padding, topPanelH - 2 * Padding);

        float bottomY = boardY + actualBoardH + Padding;
        LevelRect = new RectF(Padding, bottomY, w - 2 * Padding, h - bottomY - Padding);

        LeftPanelRect  = new RectF(0, 0, w, topPanelH);
        RightPanelRect = new RectF(0, bottomY, w, h - bottomY);
    }

    /// <summary>
    /// Returns the pixel rectangle for a specific board cell (row, col).
    /// Visible rows start at Board.BufferRows (rows 0 and 1 are the hidden buffer).
    /// </summary>
    public RectF GetCellRect(int row, int col)
    {
        int visibleRow = row - Board.BufferRows;
        float x = BoardRect.X + col * CellSize;
        float y = BoardRect.Y + visibleRow * CellSize;
        return new RectF(x, y, CellSize, CellSize);
    }
}
