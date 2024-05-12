using System.Linq;
using Content.Server.GameTicking;
using Content.Shared._Lfwb.Stats;
using Content.Shared.Humanoid;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Lfwb.Stats;

public sealed class StatsSystem : SharedStatsSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatsComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
    }

    #region Handlers

    private void OnComponentInit(EntityUid uid, StatsComponent component, ComponentInit args)
    {
        var preset = _prototypeManager.Index<StatsPresetPrototype>(component.StatsPreset);

        foreach (var (stat, range) in preset.Preset)
        {
            var statValue = GetValue(range);
            SetStatValue(uid, stat, statValue, true);
        }
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if(!HasComp<HumanoidAppearanceComponent>(ev.Mob))
            return;

        var statsComponent = EnsureComp<StatsComponent>(ev.Mob);

        if (string.IsNullOrEmpty(ev.JobId))
        {
            var modification = _prototypeManager.Index<JobStatsModification>(JobPrototype.DefaultStatsModification);
            ApplyJobStatsModification(ev.Mob, statsComponent, modification);
        }
        else
        {
            var job = _prototypeManager.Index<JobPrototype>(ev.JobId);
            var modification = _prototypeManager.Index<JobStatsModification>(job.StatsModification);
            ApplyJobStatsModification(ev.Mob, statsComponent, modification);
        }
    }

    #endregion

    #region Private

    private void ApplyJobStatsModification(EntityUid owner, StatsComponent component, JobStatsModification modification)
    {
        foreach (var (stat, range) in modification.StatsModification)
        {
            var modificationValue = GetValue(range);
            ModifyStat(owner, stat, modificationValue);
        }
    }

    private int GetValue(List<int> range)
    {
        var minValue = range.First();
        var maxValue = range.Last();
        return _random.Next(minValue, maxValue + 1);
    }

    #endregion
}
