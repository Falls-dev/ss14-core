using Robust.Shared.Containers;

namespace Content.Shared._White.Weapons.HisGrace;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class HisGraceComponent : Component
{
    [ViewVariables, DataField, AutoNetworkedField]
    public bool Ascended;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Thirst = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    public float DamagerPerVictim = 4;

    [ViewVariables(VVAccess.ReadWrite)]
    public int VictimsNeeded = 25;

    [ViewVariables]
    public int CurrentVictims = 0;

    [ViewVariables, AutoNetworkedField]
    public bool Awakened;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? Master;

    [ViewVariables]
    public TimeSpan NextUpdateTime = TimeSpan.Zero;

    public Container Container = default!;
}