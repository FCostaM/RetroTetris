using RetroTetris.Core.Engine;

namespace RetroTetris.Core.Models;

public record GameSnapshot(
    TetrominoType?[,] Cells,
    TetrominoType ActiveType,
    RotationState ActiveRotation,
    int ActiveRow,
    int ActiveCol,
    TetrominoType? HoldType,
    IReadOnlyList<TetrominoType> NextQueue,
    int Score,
    int Level,
    int LinesCleared
);
