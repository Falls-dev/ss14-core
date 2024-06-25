using System.Collections;
using Content.Server._White.Genetics.Components;
using Content.Shared._White.Genetics;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Genetics;

/// <summary>
/// Apply Mutation Apply?
/// </summary>
public sealed partial class GenomeSystem
{
    /// <summary>
    /// TODO: recheck
    /// </summary>
    /// <param name="args"></param>
    private void OnGenomeChanged(GenomeChangedEvent args)
    {
        foreach (var (region, (was, became)) in args.RegionsChanged)
        {
            if (!args.Comp.Layout.Values.TryGetValue(region, out var indexes))
                continue;

            if (!args.Comp.MutationRegions.TryGetValue(region, out var possibleMutation))
                continue;

            if (!_mutations.TryGetValue(possibleMutation, out var mutation))
                continue;


            if (args.Comp.ActivatedMutations.Contains(possibleMutation))
            {
                if (args.Comp.Genome.GetInt(indexes.Item1, indexes.Item2) !=
                    mutation.Genome.GetInt(0, mutation.Genome.GetLength()))
                {
                    CancelMutation(args.Uid, args.Comp, possibleMutation);
                }
            }
            else
            {
                if (args.Comp.Genome.GetInt(indexes.Item1, indexes.Item2) ==
                    mutation.Genome.GetInt(0, mutation.Genome.GetLength()))
                {
                    ApplyMutation(args.Uid, args.Comp, possibleMutation);
                }
            }

            //TODO: incorporated mutator mutations?
        }
    }

    public void ApplyMutation(EntityUid uid, GenomeComponent comp, string mutationName)
    {
        if (!_mutations.TryGetValue(mutationName, out var mutation))
            return;

        comp.Instability += mutation.Instability;
        comp.ActivatedMutations.Add(mutationName);
        foreach (var effect in mutation.Effects)
        {
            effect.Apply(uid, EntityManager);
        }
    }

    public void CancelMutation(EntityUid uid, GenomeComponent comp, string mutationName)
    {
        if (!comp.ActivatedMutations.Contains(mutationName) || _mutations.TryGetValue(mutationName, out var mutation))
            return;

        comp.ActivatedMutations.Remove(mutationName);
        comp.Instability -= mutation.Instability;
        foreach (var effect in mutation.Effects)
        {
            effect.Cancel(uid, EntityManager);
        }
    }


    // TODO: снести ес
    /*
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

            genomeComp.ActivatedMutations.Add("SuperStrength");
        }
        else
        {
            TryComp<MeleeWeaponComponent>(uid, out var comp);
            comp?.Damage.DamageDict.Add("Blunt", 5);

            genomeComp.ActivatedMutations.Remove("SuperStrength");
        }
    } */
}
