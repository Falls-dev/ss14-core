using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._White.Genetics.Components;
using Content.Server.GameTicking.Events;
using Content.Shared._White.Genetics;
using Content.Shared.Damage;
using Content.Shared.Flash.Components;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Radiation.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._White.Genetics.Systems;

/// <summary>
/// Assigns each <see cref="GenomePrototype"/> a random <see cref="GenomeLayout"/> roundstart.
/// </summary>
public sealed partial class GenomeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ILogManager _log = default!;


    protected ISawmill _sawmill = default!;
    // This is where all the genome layouts are stored.
    // TODO: store on round entity when thats done, so persistence reloading doesnt scramble genes
    [ViewVariables]
    private readonly Dictionary<string, GenomeLayout> _layouts = new();

    private string _mutationsPool = "StandardHumanMutations";
    private bool _mutationsInitialized = false;
    private Dictionary<string, (Genome Genome, MutationEffect[] Effects, int Instability)> _mutations = new ();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GenomeComponent, ComponentInit>(OnGenomeCompInit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        SubscribeLocalEvent<GenomeChangedEvent>(OnGenomeChanged);
        SubscribeLocalEvent<GenomeComponent, OnIrradiatedEvent>(OnIrradiated);
        InitializeMutations();
        _sawmill = _log.GetSawmill("genetics");
    }


    /// <summary>
    /// TODO: test this whole thing
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="comp"></param>
    /// <param name="args"></param>
    private void OnGenomeCompInit(EntityUid uid, GenomeComponent comp, ComponentInit args)
    {
        // only empty in test and when vving
        if (comp.GenomeId != string.Empty)
            comp.Layout = GetOrCreateLayout(comp.GenomeId);

        var mutationsPool = _mutations;
        foreach (var (name, (index, len)) in comp.Layout.Values)
        {
            if (!name.Contains("mutation"))
                continue;

            var mutationName = mutationsPool.Keys.ToArray()[_random.Next(mutationsPool.Count)];

            var (genome, effect, _) = mutationsPool[mutationName]; //TODO: are mutations standardised in size?
            var bits = genome.Bits;
            if (bits.Length == 0)
            {
                _sawmill.Error($"Error while initializing a sequence in GenomeComponent. Name: {name}; Length: {len}");
                //throw new Exception()
                continue;
            }
            var mutatedBits = Array.Empty<int>();

            var prob = 0.99f;
            while (prob > 0.001f)
            {
                if (!_random.Prob(prob))
                {
                    if (prob == 0.99f)
                    {
                        ApplyMutatorMutation(uid, comp, mutationName);
                    }
                    break;
                }

                var i = _random.Next(len);

                if (mutatedBits.Contains(i))
                {
                    break;
                }

                bits[i] = !bits[i];
                mutatedBits.Append(i);

                prob = prob / 2;
            }

            mutationsPool.Remove(mutationName);
            comp.Layout.SetBitArray(comp.Genome, name, bits);
            comp.MutationRegions.Add(name, mutationName);
        }
    }


    /// <summary>
    /// TODO: Test this shit
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="comp"></param>
    /// <param name="args"></param>
    public void OnIrradiated(EntityUid uid, GenomeComponent comp, OnIrradiatedEvent args)
    {
        if (args.TotalRads > 20)
        {
            // TODO: change genome, apply random mutation via mutator
        }
    }

    private void Reset(RoundRestartCleanupEvent args)
    {
        _layouts.Clear();
        _mutations.Clear();
        _mutationsInitialized = false;
    }

    private void InitializeMutations()
    {
        _proto.TryIndex<MutationCollectionPrototype>(_mutationsPool, out var pool);
        if (pool == null)
        {
            //TODO: throw an error here
            return;
        }

        foreach (var mutation in pool.Mutations)
        {
            _proto.TryIndex<MutationPrototype>(mutation, out var mutationProto);
            if (mutationProto != null)
                _mutations.Add(mutationProto.Name, (GenerateSomeRandomGeneticSequenceAndCheckIfItIsIn_mutationsFunction(mutationProto.Length), mutationProto.Effect, mutationProto.Instability));
        }

        _mutationsInitialized = true;
    }

    public Genome GenerateSomeRandomGeneticSequenceAndCheckIfItIsIn_mutationsFunction(int length, int cycle = 0)
    {
        var sequence = new Genome(length);
        sequence.Mutate(0, length, 0.5f);

        if (cycle >= 10) // i think 10 cycles is enough
        {
            _sawmill.Error("ActivatedMutations initialization error. Try making longer sequences or less mutations");
            return sequence;
        }

        foreach (var (_, (seq, _, _)) in _mutations)
        {
            if (sequence == seq)
                return GenerateSomeRandomGeneticSequenceAndCheckIfItIsIn_mutationsFunction(length, cycle + 1);
        }
        return sequence;
    }

    /// <summary>
    /// Either gets an existing genome layout or creates a new random one.
    /// Genome layouts are reset between rounds.
    /// Anything with <see cref="GenomeComponent"/> calls this on mapinit to ensure it uses the correct layout.
    /// </summary>
    /// <param name="id">Genome prototype id to create the layout from</param>
    public GenomeLayout GetOrCreateLayout(string id)
    {
        // already created for this round so just use it
        if (TryGetLayout(id, out var layout))
            return layout;

        // prototype must exist
        var proto = _proto.Index<GenomePrototype>(id);

        // create the new random genome layout!
        layout = new GenomeLayout();
        var names = new List<string>(proto.ValueBits.Keys);
        _random.Shuffle(names);
        foreach (var name in names)
        {
            var length = proto.ValueBits[name];
            layout.Add(name, length);
        }

        // save it for the rest of the round
        AddLayout(id, layout);
        return layout;
    }

    /// <summary>
    /// Copies the <c>Genome</c> bits from a parent to a child.
    /// They must use the same genome layout or it will be logged and copy nothing.
    /// </summary>
    public void CopyParentGenes(EntityUid uid, EntityUid parent, GenomeComponent? comp = null, GenomeComponent? parentComp = null)
    {
        if (!Resolve(uid, ref comp) || !Resolve(parent, ref parentComp))
            return;

        if (parentComp.GenomeId != comp.GenomeId)
        {
            Log.Error($"Tried to copy incompatible genome from {ToPrettyString(parent):parent)} ({parentComp.GenomeId}) to {ToPrettyString(uid):child)} ({comp.GenomeId})");
            return;
        }

        parentComp.Genome.CopyTo(comp.Genome);
    }

    private bool TryGetLayout(string id, [NotNullWhen(true)] out GenomeLayout? layout)
    {
        return _layouts.TryGetValue(id, out layout);
    }

    private void AddLayout(string id, GenomeLayout layout)
    {
        _layouts.Add(id, layout);
    }
}
