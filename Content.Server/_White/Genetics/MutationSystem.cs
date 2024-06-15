using Content.Server.Genetics.Components;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics;

/// <summary>
/// Apply Mutation Effect?
/// </summary>
public sealed class MutationSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;


    public override void Initialize()
    {
        base.Initialize();
    }

    public int GetDamage(string race)
    {
        var races = new Dictionary<string, int>()
        {
            { "Arachnid", 5},
            { "Diona", 5},
            { "Dwarf", 5},
            { "Human", 5}
        };

        //TODO: больше рас

        return races[race];
    }

    public void SuperStrength(EntityUid uid, GenomeComponent genomeComp, bool state)
    {
        if (!HasComp<GenomeComponent>(uid))
            return;

        if (state == true)
        {
            TryComp<MeleeWeaponComponent>(uid, out var comp);
            comp?.Damage.DamageDict.Add("Blunt", 30);

            genomeComp.Mutations.Add("SuperStrength");
        }
        else
        {
            TryComp<MeleeWeaponComponent>(uid, out var comp);
            comp?.Damage.DamageDict.Add("Blunt", 5);

            genomeComp.Mutations.Remove("SuperStrength");
        }
    }

    public void XrayVision(EntityUid uid, GenomeComponent genomeComp, bool state)
    {
        if (!HasComp<GenomeComponent>(uid))
            return;

        if (state == true)
        {
            //TODO
        }
        else
        {
            //TODO
        }
    }
}
