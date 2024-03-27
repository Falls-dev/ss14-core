using System.Numerics;
using Robust.Shared.Audio;

namespace Content.Server._White.ExperimentalSyndicateTeleporter;

[RegisterComponent]
public sealed partial class ExperimentalSyndicateTeleporterComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public int Uses = 12;

    [ViewVariables(VVAccess.ReadWrite)]
    public int MinTeleportRange = 3;

    [ViewVariables(VVAccess.ReadWrite)]
    public int MaxTeleportRange = 8;

    [ViewVariables(VVAccess.ReadWrite)]
    public List<Vector2> EmergencyVectors = new() {new Vector2(3,3), new Vector2(3,-3), new Vector2(-3,3), new Vector2(-3,-3)};

    [ViewVariables(VVAccess.ReadOnly)]
    public string? ExpSyndicateTeleportInEffect = "ExpSyndicateTeleporterInEffect";

    [ViewVariables(VVAccess.ReadOnly)]
    public string? ExpSyndicateTeleportOutEffect = "ExpSyndicateTeleporterOutEffect";

    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/White/Devices/expsyndicateteleport.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(10);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextUse = TimeSpan.Zero;
}
