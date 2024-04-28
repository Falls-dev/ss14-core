using Content.Server.GameTicking;
using Content.Shared._Lfwb.Skills;
using Content.Shared.Humanoid;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Lfwb.Skills;

public sealed class SkillsSystem : SharedSkillsSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if(!HasComp<HumanoidAppearanceComponent>(ev.Mob))
            return;

        var skillsComponent = EnsureComp<SkillsComponent>(ev.Mob);

        if (string.IsNullOrEmpty(ev.JobId))
            return;

        var job = _prototypeManager.Index<JobPrototype>(ev.JobId);

        if (string.IsNullOrEmpty(job.SkillsModification))
            return;

        var modification = _prototypeManager.Index<JobSkillsModification>(job.SkillsModification);

        ApplyJobSkillModification(ev.Mob, skillsComponent, modification);
    }

    private void ApplyJobSkillModification(EntityUid owner, SkillsComponent skillsComponent, JobSkillsModification modification)
    {
        foreach (var (skill, skillLevel) in modification.StatsModification)
        {
            skillsComponent.SetSkillLevel(skill, skillLevel);
            Dirty(owner, skillsComponent);
        }
    }
}
