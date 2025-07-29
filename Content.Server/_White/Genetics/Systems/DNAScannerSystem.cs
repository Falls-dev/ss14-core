using Content.Server._White.Genetics.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Forensics;
using Content.Server.PowerCell;
using Content.Shared._White.Genetics;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.PowerCell;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server._White.Genetics.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class DNAScannerSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DNAScannerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DNAScannerComponent, DNAScannerDoAfterEvent>(OnDoAfter);
    }

    /// <summary>
    /// Trigger the doafter for scanning
    /// </summary>
    private void OnAfterInteract(Entity<DNAScannerComponent> uid, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<GenomeComponent>(args.Target) || !_cell.HasDrawCharge(uid, user: args.User))
            return;

        _audio.PlayPvs(uid.Comp.ScanningBeginSound, uid);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, uid.Comp.ScanDelay, new DNAScannerDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = true
        });
    }

    private void OnDoAfter(Entity<DNAScannerComponent> uid, ref DNAScannerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || !_cell.HasDrawCharge(uid, user: args.User))
            return;

        _audio.PlayPvs(uid.Comp.ScanningEndSound, uid);

        if (!TryComp<GenomeComponent>(args.Target, out var genome))
            return;

        uid.Comp.ScannedGenome = genome;

        OpenUserInterface(args.User, uid);

        if (!_uiSystem.TryGetUi(uid.Owner, DNAScannerUiKey.Key, out var ui))
            return;

        string? fingerPrints = null;
        if (TryComp<DnaComponent>(args.Target, out var dna))
            fingerPrints = dna.DNA;

        _uiSystem.SendUiMessage(ui, new DNAScannerScannedGenomeMessage(
            GetNetEntity(args.Target),
            genome.Genome,
            genome.Layout,
            genome.MutationRegions,
            genome.MutatedMutations,
            genome.ActivatedMutations,
            fingerPrints
        ));

        args.Handled = true;
    }

    private void OpenUserInterface(EntityUid user, EntityUid scanner)
    {
        if (!TryComp<ActorComponent>(user, out var actor) || !_uiSystem.TryGetUi(scanner, DNAScannerUiKey.Key, out var ui))
            return;

        _uiSystem.OpenUi(ui, actor.PlayerSession);
    }



}
