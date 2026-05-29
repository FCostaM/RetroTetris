using RetroTetris.Core.Engine;
using RetroTetris.Core.States;

namespace RetroTetris.Core.Interfaces;

public interface IGameEngine
{
    // State and data
    IGameState CurrentState { get; }
    Board Board { get; }
    Tetromino? ActivePiece { get; }
    Tetromino? GhostPiece { get; }
    Tetromino? HoldPiece { get; }
    IReadOnlyList<Tetromino> NextPieces { get; }  // 3 next pieces
    int Score { get; }
    int HighScore { get; }
    int Level { get; }
    int TotalLinesCleared { get; }

    // Lifecycle control
    void StartNewGame();
    void TransitionTo(IGameState newState);
    void Update(TimeSpan delta);

    // Player actions (called by IGameCommand)
    void MoveLeft();
    void MoveRight();
    void SoftDrop();
    void HardDrop();
    void RotateClockwise();
    void RotateCounterClockwise();
    void Hold();
    void Pause();
    void Resume();
    void ShowControls();
    void HideControls();

    // Observer events
    event Action<int, int> LinesCleared;      // (linesCount, currentLevel)
    event Action<int> ScoreChanged;           // (newScore)
    event Action<int> LevelChanged;           // (newLevel)
    event Action PieceLocked;
    event Action<TetrominoType> PieceSpawned;
    event Action GameOverOccurred;
}
