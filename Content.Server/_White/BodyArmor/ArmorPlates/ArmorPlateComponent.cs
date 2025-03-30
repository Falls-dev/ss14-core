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
}

public enum PlateTier : byte
{
    TierOne = 0,
    TierTwo = 1,
    TierThree = 2,
}
