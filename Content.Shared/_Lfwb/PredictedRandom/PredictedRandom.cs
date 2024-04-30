using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Lfwb.PredictedRandom;

public sealed class PredictedRandomSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private System.Random _random = new();

    #region Next

    public int Next(int minValue, int maxValue)
    {
        SetSeed();
        return _random.Next(minValue, maxValue);
    }

    public int Next(int minValue, int maxValue, int value)
    {
        SetSeed(value);
        return _random.Next(minValue, maxValue);
    }

    #endregion

    #region Prob

    public bool Prob(float chance)
    {
        DebugTools.Assert(chance <= 1 && chance >= 0, $"Chance must be in the range 0-1. It was {chance}.");

        SetSeed();
        return _random.NextDouble() < chance;
    }

    #endregion

    #region Pick

    public T Pick<T>(IReadOnlyList<T> list)
    {
        SetSeed();

        var index = _random.Next(list.Count);
        return list[index];
    }

    #endregion

    #region Private

    private void SetSeed()
    {
        var currentTick = _timing.CurTime.Milliseconds.GetHashCode();
        _random = new System.Random(currentTick);
    }

    private void SetSeed(int value)
    {
        var currentTick = _timing.CurTime.Milliseconds.GetHashCode();
        var valueHash = value.GetHashCode();

        var hash = HashCode.Combine(currentTick, valueHash);

        _random = new System.Random(hash);
    }

    #endregion
}
