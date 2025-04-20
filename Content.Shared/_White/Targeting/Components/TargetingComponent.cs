using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Targeting.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TargetingComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public TargetingBodyParts TargetBodyPart = TargetingBodyParts.Chest;

    [DataField]
    public Dictionary<TargetingBodyParts, float> TargetingChance = new()
    {
        { TargetingBodyParts.Head, 0.1f },
        { TargetingBodyParts.Chest, 0.4f },
        { TargetingBodyParts.LeftArm, 0.125f },
        { TargetingBodyParts.RightArm, 0.125f },
        { TargetingBodyParts.LeftLeg, 0.125f },
        { TargetingBodyParts.Stomach, 0.125f },
        { TargetingBodyParts.RightFoot, 0.100f },
        { TargetingBodyParts.LeftFoot, 0.100f },
        { TargetingBodyParts.RightHand, 0.100f },
        { TargetingBodyParts.LeftHand, 0.100f }
    };

    [ViewVariables, AutoNetworkedField]
    public Dictionary<TargetingBodyParts, TargetIntegrity> TargetIntegrities = new()
    {
        { TargetingBodyParts.Head, TargetIntegrity.Healthy },
        { TargetingBodyParts.Chest, TargetIntegrity.Healthy },
        { TargetingBodyParts.LeftArm, TargetIntegrity.Healthy },
        { TargetingBodyParts.RightArm, TargetIntegrity.Healthy },
        { TargetingBodyParts.LeftLeg, TargetIntegrity.Healthy },
        { TargetingBodyParts.RightLeg, TargetIntegrity.Healthy },
        { TargetingBodyParts.Stomach, TargetIntegrity.Healthy },
        { TargetingBodyParts.RightHand, TargetIntegrity.Healthy },
        { TargetingBodyParts.LeftHand, TargetIntegrity.Healthy },
        { TargetingBodyParts.RightFoot, TargetIntegrity.Healthy },
        { TargetingBodyParts.LeftFoot, TargetIntegrity.Healthy },
    };

    // Maybe in future, not now, very bad sound
    //[DataField, ViewVariables(VVAccess.ReadWrite)]
   //public SoundSpecifier SoundToggle = new SoundPathSpecifier("/Audio/White/Targeting/targetingToggle.ogg");
}

public enum TargetingBodyParts
{
    Head,
    Chest,
    LeftArm,
    RightArm,
    LeftLeg,
    RightLeg,
    Stomach,
    LeftHand,
    RightHand,
    LeftFoot,
    RightFoot
}

public enum TargetIntegrity
{
    Healthy,
    LightlyWounded,
    SomewhatWounded,
    ModeratelyWounded,
    HeavilyWounded,
    CriticallyWounded,
    Severed,
    Dead,
    Disabled,
}
