using RetroTetris.Core.States;

namespace RetroTetris.Tests.Unit;

/// <summary>
/// Unit tests for ControlsScreenState constructor and PreviousState property.
/// Validates Requirement 4.3.
/// </summary>
public class ControlsScreenStateTests
{
    [Fact]
    public void Constructor_AcceptsStartScreenState_AsPreviousState()
    {
        var previous = new StartScreenState();

        var state = new ControlsScreenState(previous);

        Assert.NotNull(state);
    }

    [Fact]
    public void Constructor_AcceptsPausedState_AsPreviousState()
    {
        var previous = new PausedState();

        var state = new ControlsScreenState(previous);

        Assert.NotNull(state);
    }

    [Fact]
    public void Constructor_ThrowsArgumentException_WhenPreviousStateIsControlsScreenState()
    {
        var inner = new ControlsScreenState(new StartScreenState());

        Assert.Throws<ArgumentException>(() => new ControlsScreenState(inner));
    }

    [Fact]
    public void PreviousState_ReturnsSameInstancePassedToConstructor()
    {
        var previous = new StartScreenState();

        var state = new ControlsScreenState(previous);

        Assert.Same(previous, state.PreviousState);
    }

    [Fact]
    public void PreviousState_ReturnsSamePausedStateInstance()
    {
        var previous = new PausedState();

        var state = new ControlsScreenState(previous);

        Assert.Same(previous, state.PreviousState);
    }
}
