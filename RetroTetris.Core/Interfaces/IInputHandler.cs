namespace RetroTetris.Core.Interfaces;

public interface IInputHandler
{
    void OnKeyDown(GameKey key);
    void OnKeyUp(GameKey key);
    void OnSwipe(SwipeDirection direction);
    void OnTap(TapSide side);
    void Update(TimeSpan elapsed);
}

public enum GameKey
{
    Left, Right, Down, Up, Space, Z, X, C, Shift, Escape, P
}

public enum SwipeDirection
{
    Left, Right, Up, Down
}

public enum TapSide
{
    Left, Right
}
