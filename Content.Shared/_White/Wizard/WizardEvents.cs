using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Magic;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._White.Wizard;

[Serializable, NetSerializable]
public sealed partial class ScrollDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class AddWizardChargeEvent : EntityEventArgs
{
    public string ChargeProto;

    public AddWizardChargeEvent(string chargeProto)
    {
        ChargeProto = chargeProto;
    }
}

[Serializable, NetSerializable]
public sealed partial class RemoveWizardChargeEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class RequestSpellChargingAudio : EntityEventArgs
{
    public SoundSpecifier Sound;
    public bool Loop;

    public RequestSpellChargingAudio(SoundSpecifier sound, bool loop)
    {
        Sound = sound;
        Loop = loop;
    }
}

[Serializable, NetSerializable]
public sealed partial class RequestSpellChargedAudio : EntityEventArgs
{
    public SoundSpecifier Sound;
    public bool Loop;

    public RequestSpellChargedAudio(SoundSpecifier sound, bool loop)
    {
        Sound = sound;
        Loop = loop;
    }
}

[Serializable, NetSerializable]
public sealed partial class RequestAudioSpellStop : EntityEventArgs
{
}

public sealed partial class ArcSpellEvent : WorldTargetActionEvent, ISpeakSpell
{
    /// <summary>
    /// What entity should be spawned.
    /// </summary>
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    /// <summary>
    /// Gets the targeted spawn positions; may lead to multiple entities being spawned.
    /// </summary>
    [DataField("posData")] public MagicSpawnData Pos = new TargetCasterPos();

    [DataField("speech")]
    public string? Speech { get; private set; }
}
