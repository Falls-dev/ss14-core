using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;

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
