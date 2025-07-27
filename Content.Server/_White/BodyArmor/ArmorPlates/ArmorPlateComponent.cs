using Content.Shared.FixedPoint;

namespace Content.Server._White.BodyArmor.ArmorPlates;

[RegisterComponent]
public sealed partial class ArmorPlateComponent : Component
{
    [DataField]
    public int AllowedDamage = 100;

    [DataField]
    public int ReceivedDamage;

    [DataField("tier")]
    public PlateTier PlateTier = 0;

    [DataField]
    public Dictionary<PlateTier, FixedPoint2> DamageOfTier = new()
    {
        { PlateTier.TierOne, 7 },
        { PlateTier.TierTwo, 12 },
        { PlateTier.TierThree, 14 }
    };
}

public enum PlateTier : byte
{
    TierOne = 0,
    TierTwo = 1,
    TierThree = 2,
}
