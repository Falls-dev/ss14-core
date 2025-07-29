using Content.Shared._White.Genetics.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.CombatMode;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;

namespace Content.Shared._White.Genetics;

public partial class SharedGeneticInjectorSystem : EntitySystem
{
    [Dependency] protected readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly MobStateSystem _mobState = default!;
    [Dependency] protected readonly SharedCombatModeSystem _combat = default!;
    [Dependency] protected readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] protected readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GeneticInjectorComponent, ComponentStartup>(OnInjectorStartup);
        SubscribeLocalEvent<GeneticInjectorComponent, UseInHandEvent>(OnInjectorUse);
    }

    private void OnInjectorStartup(Entity<GeneticInjectorComponent> entity, ref ComponentStartup args)
    {
        // ???? why ?????
        // TODO: wtf is this
        Dirty(entity);
    }

    private void OnInjectorUse(Entity<GeneticInjectorComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        // inject yourself here

        args.Handled = true;
    }


}
