using RetroTetris.Core.Engine;
using Xunit;

namespace RetroTetris.Tests.Unit;

public class LockDelayControllerTests
{
    // ─── Initial State ─────────────────────────────────────────────────────────

    [Fact]
    public void InitialState_IsActiveIsFalse()
    {
        var controller = new LockDelayController();
        Assert.False(controller.IsActive);
    }

    // ─── Start ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Start_SetsIsActiveToTrue()
    {
        var controller = new LockDelayController();
        controller.Start();
        Assert.True(controller.IsActive);
    }

    // ─── HasExpired when not active ────────────────────────────────────────────

    [Fact]
    public void HasExpired_BeforeStart_ReturnsFalse()
    {
        var controller = new LockDelayController();
        Assert.False(controller.HasExpired(TimeSpan.FromMilliseconds(600)));
    }

    // ─── HasExpired timing ─────────────────────────────────────────────────────

    [Fact]
    public void HasExpired_Before500ms_ReturnsFalse()
    {
        var controller = new LockDelayController();
        controller.Start();
        Assert.False(controller.HasExpired(TimeSpan.FromMilliseconds(499)));
    }

    [Fact]
    public void HasExpired_AtExactly500ms_ReturnsTrue()
    {
        var controller = new LockDelayController();
        controller.Start();
        Assert.True(controller.HasExpired(TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public void HasExpired_After500ms_ReturnsTrue()
    {
        var controller = new LockDelayController();
        controller.Start();
        Assert.True(controller.HasExpired(TimeSpan.FromMilliseconds(600)));
    }

    // ─── Reset ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Reset_RestartsTimer()
    {
        var controller = new LockDelayController();
        controller.Start();

        // Accumulate 400ms
        controller.HasExpired(TimeSpan.FromMilliseconds(400));

        // Reset the timer
        controller.Reset();

        // 400ms after reset — should NOT have expired (only 400ms since reset)
        Assert.False(controller.HasExpired(TimeSpan.FromMilliseconds(400)));
    }

    [Fact]
    public void Reset_IncrementsCounter_BlockedAfterMaxResets()
    {
        var controller = new LockDelayController();
        controller.Start();

        // Perform exactly MaxResets resets
        for (int i = 0; i < LockDelayController.MaxResets; i++)
        {
            controller.Reset();
        }

        // Accumulate 400ms after the last valid reset
        controller.HasExpired(TimeSpan.FromMilliseconds(400));

        // 16th reset should be a no-op — timer keeps running from 400ms
        controller.Reset();

        // 200ms more = 600ms total since last valid reset → should have expired
        Assert.True(controller.HasExpired(TimeSpan.FromMilliseconds(200)));
    }

    [Fact]
    public void Reset_BlockedAfterMaxResets_AccumulatedTimeContinues()
    {
        var controller = new LockDelayController();
        controller.Start();

        // Exhaust all resets
        for (int i = 0; i < LockDelayController.MaxResets; i++)
        {
            controller.Reset();
        }

        // Accumulate 400ms
        controller.HasExpired(TimeSpan.FromMilliseconds(400));

        // 16th reset is a no-op
        controller.Reset();

        // 100ms more = 500ms total → should expire (timer was NOT reset)
        Assert.True(controller.HasExpired(TimeSpan.FromMilliseconds(100)));
    }

    // ─── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_SetsIsActiveToFalse()
    {
        var controller = new LockDelayController();
        controller.Start();
        controller.Cancel();
        Assert.False(controller.IsActive);
    }

    [Fact]
    public void HasExpired_AfterCancel_ReturnsFalse()
    {
        var controller = new LockDelayController();
        controller.Start();
        controller.Cancel();
        Assert.False(controller.HasExpired(TimeSpan.FromMilliseconds(600)));
    }

    // ─── Start resets counter ──────────────────────────────────────────────────

    [Fact]
    public void Start_AfterMaxResets_ResetsCounterAllowingMoreResets()
    {
        var controller = new LockDelayController();
        controller.Start();

        // Exhaust all resets
        for (int i = 0; i < LockDelayController.MaxResets; i++)
        {
            controller.Reset();
        }

        // Restart — should reset the counter to 0
        controller.Start();

        // Now 15 more resets should be allowed
        for (int i = 0; i < LockDelayController.MaxResets; i++)
        {
            controller.Reset();
        }

        // After 15 resets, accumulate 400ms — should NOT have expired
        Assert.False(controller.HasExpired(TimeSpan.FromMilliseconds(400)));
    }

    // ─── Accumulation across multiple HasExpired calls ─────────────────────────

    [Fact]
    public void HasExpired_MultipleCallsAccumulateTime()
    {
        var controller = new LockDelayController();
        controller.Start();

        // Three calls of 200ms each = 600ms total
        Assert.False(controller.HasExpired(TimeSpan.FromMilliseconds(200))); // 200ms
        Assert.False(controller.HasExpired(TimeSpan.FromMilliseconds(200))); // 400ms
        Assert.True(controller.HasExpired(TimeSpan.FromMilliseconds(200)));  // 600ms
    }
}
