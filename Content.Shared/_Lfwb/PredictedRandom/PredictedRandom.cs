using Robust.Shared.Timing;

namespace Content.Shared._Lfwb.PredictedRandom;

public sealed class PredictedRandomSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private System.Random _random = new();

    public int Next(int minValue, int maxValue)
    {
        SetSeed();
        return _random.Next(minValue, maxValue);
    }

    private void SetSeed()
    {
        var currentTick = _timing.CurTime.Seconds.GetHashCode();
        _random = new System.Random(currentTick);
    }
}
