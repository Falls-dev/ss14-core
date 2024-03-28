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
    public int EmergencyLength = 3;

    [ViewVariables(VVAccess.ReadWrite)]
    public int EmpLength = 6;

    [ViewVariables(VVAccess.ReadWrite)]
    public List<int> RandomRotations = new() {90, -90};

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
