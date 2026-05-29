using RetroTetris.Core.Interfaces;
using RetroTetris.Core.States;

namespace RetroTetris.Core.Engine;

/// <summary>
/// Central coordinator that implements <see cref="IGameEngine"/>.
/// Coordinates all subsystems: Board, BagRandomizer, ScoreSystem, LevelSystem,
/// LockDelayController, GhostCalculator, and SRSRotationSystem.
/// Implements the State Machine pattern — delegating state transitions via TransitionTo().
/// </summary>
public class GameEngine : IGameEngine
{
    private readonly Board _board = new();
    private readonly BagRandomizer _bag = new();
    private readonly ScoreSystem _scoreSystem = new();
    private readonly LevelSystem _levelSystem = new();
    private readonly LockDelayController _lockDelay = new();

    private IGameState _currentState;
    private Tetromino? _activePiece;
    private Tetromino? _ghostPiece;
    private Tetromino? _holdPiece;
    private bool _holdUsed;
    private TimeSpan _dropAccumulator;

    public GameEngine()
    {
        _currentState = new StartScreenState();
        _currentState.Enter(this);
    }

    #region IGameEngine properties 

    public IGameState CurrentState => _currentState;
    public Board Board => _board;
    public Tetromino? ActivePiece => _activePiece;
    public Tetromino? GhostPiece => _ghostPiece;
    public Tetromino? HoldPiece => _holdPiece;
    public IReadOnlyList<Tetromino> NextPieces => GetNextPieces();
    public int Score => _scoreSystem.Score;
    public int HighScore { get; set; }
    public int Level => _levelSystem.Level;
    public int TotalLinesCleared => _levelSystem.TotalLinesCleared;

    #endregion

    #region Observer events

    public event Action<int, int>? LinesCleared;
    public event Action<int>? ScoreChanged;
    public event Action<int>? LevelChanged;
    public event Action? PieceLocked;
    public event Action<TetrominoType>? PieceSpawned;
    public event Action? GameOverOccurred;

    #endregion

    #region Lifecycle control

    /// <summary>
    /// Resets all subsystems and starts a fresh game.
    /// </summary>
    public void StartNewGame()
    {
        _board.Reset();
        _bag.Reset();
        _scoreSystem.Reset();
        _levelSystem.Reset();
        _lockDelay.Cancel();

        _holdPiece = null;
        _holdUsed = false;
        _dropAccumulator = TimeSpan.Zero;

        SpawnNextPiece();
        TransitionTo(new PlayingState());
    }

    /// <summary>
    /// Transitions to a new game state, calling Exit on the old and Enter on the new.
    /// Fires <see cref="GameOverOccurred"/> when entering <see cref="GameOverState"/>.
    /// </summary>
    public void TransitionTo(IGameState newState)
    {
        _currentState.Exit(this);
        _currentState = newState;
        _currentState.Enter(this);

        if (newState is GameOverState)
        {
            HighScore = Math.Max(HighScore, Score);
            GameOverOccurred?.Invoke();
        }
    }

    /// <summary>
    /// Called each frame by the game loop. Delegates update logic based on current state.
    /// </summary>
    public void Update(TimeSpan delta)
    {
        if (_currentState is PlayingState)
            UpdatePlaying(delta);
        // PausedState, StartScreenState, GameOverState: do nothing
    }

    #endregion

    #region Player actions

    /// <summary>
    /// Moves the active piece one column to the left.
    /// </summary>
    public void MoveLeft()
    {
        if (_currentState is not PlayingState || _activePiece is null)
            return;

        if (_board.CanPlace(_activePiece, _activePiece.Row, _activePiece.Col - 1))
        {
            _activePiece.Col--;
            if (_lockDelay.IsActive)
                _lockDelay.Reset();
            UpdateGhost();
        }
    }

    /// <summary>
    /// Moves the active piece one column to the right.
    /// </summary>
    public void MoveRight()
    {
        if (_currentState is not PlayingState || _activePiece is null)
            return;

        if (_board.CanPlace(_activePiece, _activePiece.Row, _activePiece.Col + 1))
        {
            _activePiece.Col++;
            if (_lockDelay.IsActive)
                _lockDelay.Reset();
            UpdateGhost();
        }
    }

    /// <summary>
    /// Moves the active piece one row down (soft drop).
    /// </summary>
    public void SoftDrop()
    {
        if (_currentState is not PlayingState || _activePiece is null)
            return;

        if (_board.CanPlace(_activePiece, _activePiece.Row + 1, _activePiece.Col))
        {
            _activePiece.Row++;
            if (_lockDelay.IsActive)
                _lockDelay.Reset();
        }
        else
        {
            if (!_lockDelay.IsActive)
                _lockDelay.Start();
        }
    }

    /// <summary>
    /// Instantly drops the active piece to the ghost position and locks it.
    /// </summary>
    public void HardDrop()
    {
        if (_currentState is not PlayingState || _activePiece is null)
            return;

        // Move piece to ghost row (lowest valid position)
        if (_ghostPiece is not null)
            _activePiece.Row = _ghostPiece.Row;

        LockPiece();
    }

    /// <summary>
    /// Rotates the active piece 90 degrees clockwise using SRS.
    /// </summary>
    public void RotateClockwise()
    {
        if (_currentState is not PlayingState || _activePiece is null)
            return;

        var rotated = SRSRotationSystem.TryRotate(_activePiece, _board, clockwise: true);
        if (rotated is not null)
        {
            _activePiece = rotated;
            if (_lockDelay.IsActive)
                _lockDelay.Reset();
            UpdateGhost();
        }
    }

    /// <summary>
    /// Rotates the active piece 90 degrees counter-clockwise using SRS.
    /// </summary>
    public void RotateCounterClockwise()
    {
        if (_currentState is not PlayingState || _activePiece is null)
            return;

        var rotated = SRSRotationSystem.TryRotate(_activePiece, _board, clockwise: false);
        if (rotated is not null)
        {
            _activePiece = rotated;
            if (_lockDelay.IsActive)
                _lockDelay.Reset();
            UpdateGhost();
        }
    }

    /// <summary>
    /// Holds the current piece. If a held piece exists, swaps it with the active piece.
    /// Can only be used once per piece lock.
    /// </summary>
    public void Hold()
    {
        if (_currentState is not PlayingState || _activePiece is null || _holdUsed)
            return;

        if (_holdPiece is null)
        {
            // No held piece yet — store current and spawn next
            _holdPiece = Tetromino.Create(_activePiece.Type);
            _holdUsed = true;
            SpawnNextPiece();
        }
        else
        {
            // Swap active piece with held piece
            var previousHoldType = _holdPiece.Type;
            _holdPiece = Tetromino.Create(_activePiece.Type);
            _activePiece = Tetromino.Create(previousHoldType);
            _holdUsed = true;
            UpdateGhost();
        }
    }

    /// <summary>
    /// Pauses the game. Only valid when in PlayingState.
    /// </summary>
    public void Pause()
    {
        if (_currentState is not PlayingState)
            return;

        TransitionTo(new PausedState());
    }

    /// <summary>
    /// Resumes the game. Only valid when in PausedState.
    /// </summary>
    public void Resume()
    {
        if (_currentState is not PausedState)
            return;

        TransitionTo(new PlayingState());
        // Do NOT reset _dropAccumulator — the piece continues from where it was
    }

    /// <summary>
    /// Opens the Controls_Screen. Only valid when in StartScreenState or PausedState.
    /// </summary>
    public void ShowControls()
    {
        if (_currentState is StartScreenState or PausedState)
            TransitionTo(new ControlsScreenState(_currentState));
    }

    /// <summary>
    /// Closes the Controls_Screen and returns to the stored PreviousState.
    /// Only valid when in ControlsScreenState.
    /// </summary>
    public void HideControls()
    {
        if (_currentState is ControlsScreenState css)
            TransitionTo(css.PreviousState);
    }

    #endregion

    #region Private helpers 

    /// <summary>
    /// Core update logic executed each frame while in PlayingState.
    /// Handles auto-drop and lock delay expiry.
    /// </summary>
    private void UpdatePlaying(TimeSpan delta)
    {
        if (_activePiece is null)
            return;

        // Auto-drop
        _dropAccumulator += delta;
        if (_dropAccumulator >= GetDropInterval())
        {
            _dropAccumulator = TimeSpan.Zero;

            if (_board.CanPlace(_activePiece, _activePiece.Row + 1, _activePiece.Col))
            {
                _activePiece.Row++;
                _lockDelay.Cancel();
            }
            else
            {
                if (!_lockDelay.IsActive)
                    _lockDelay.Start();
            }
        }

        // Lock delay
        if (_lockDelay.IsActive && _lockDelay.HasExpired(delta))
        {
            LockPiece();
        }
    }

    /// <summary>
    /// Locks the active piece onto the board, processes line clears, and spawns the next piece.
    /// </summary>
    private void LockPiece()
    {
        if (_activePiece is null)
            return;

        _board.LockPiece(_activePiece);
        PieceLocked?.Invoke();

        var lines = _board.FindCompleteLines();
        if (lines.Count > 0)
        {
            _board.ClearLines(lines);
            _scoreSystem.AddLinesClear(lines.Count, _levelSystem.Level);
            _levelSystem.AddLines(lines.Count);

            LinesCleared?.Invoke(lines.Count, _levelSystem.Level);
            ScoreChanged?.Invoke(_scoreSystem.Score);
            LevelChanged?.Invoke(_levelSystem.Level);
        }

        _lockDelay.Cancel();
        _holdUsed = false;

        SpawnNextPiece();
    }

    /// <summary>
    /// Dequeues the next piece from the bag, places it at the spawn position,
    /// calculates the ghost, fires PieceSpawned, and checks for game over.
    /// </summary>
    private void SpawnNextPiece()
    {
        var type = _bag.Dequeue();
        _activePiece = Tetromino.Create(type); // Row=0, Col=3

        UpdateGhost();
        PieceSpawned?.Invoke(type);

        // Check if spawn position is blocked → game over
        if (!_board.CanPlace(_activePiece, _activePiece.Row, _activePiece.Col))
        {
            TransitionTo(new GameOverState());
        }
    }

    /// <summary>Recalculates the ghost piece based on the current active piece position.</summary>
    private void UpdateGhost()
    {
        if (_activePiece is null)
        {
            _ghostPiece = null;
            return;
        }

        _ghostPiece = GhostCalculator.Calculate(_activePiece, _board);
    }

    /// <summary>Returns the next 3 pieces from the bag as Tetromino instances.</summary>
    private IReadOnlyList<Tetromino> GetNextPieces()
    {
        var result = new List<Tetromino>();
        for (int i = 0; i < 3; i++)
        {
            var type = _bag.Peek(i);
            var piece = Tetromino.Create(type);
            result.Add(piece);
        }
        return result;
    }

    /// <summary>Returns the drop interval for the current level.</summary>
    private TimeSpan GetDropInterval() =>
        TimeSpan.FromMilliseconds(_levelSystem.GetDropIntervalMs());

    #endregion
}
