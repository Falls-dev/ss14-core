using Content.Shared._White.TTS;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.HumanoidCharacterProfileExtensions;

[Serializable, NetSerializable]
public sealed class WhiteHumanoidProfileExtension : IEquatable<WhiteHumanoidProfileExtension>
{
    [DataField]
    public ProtoId<TTSVoicePrototype> VoiceId { get; set; } = "Nord";

    public static readonly Dictionary<Sex, ProtoId<TTSVoicePrototype>> DefaultSexVoice = new()
    {
        { Sex.Male, "Nord" },
        { Sex.Female, "Amina" },
        { Sex.Unsexed, "Alyx" },
    };

    public static WhiteHumanoidProfileExtension Default()
    {
        var profileExtension = new WhiteHumanoidProfileExtension();
        profileExtension.VoiceId = "Nord";

        return profileExtension;
    }
    public static WhiteHumanoidProfileExtension DefaultWithSex(Sex sex)
    {
        var profileExtension = new WhiteHumanoidProfileExtension
        {
            VoiceId = DefaultSexVoice[sex],
        };

        return profileExtension;
    }

    public bool Equals(WhiteHumanoidProfileExtension? other)
    {
        if (other is null)
        {
            return false;
        }

        return VoiceId.Equals(other.VoiceId);
    }


    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

public static class WhiteHumanoidProfileExtensions
{
    public static WhiteHumanoidProfileExtension Copy(this WhiteHumanoidProfileExtension other)
    {
        return new WhiteHumanoidProfileExtension
        {
            VoiceId = other.VoiceId,
        };
    }
}
