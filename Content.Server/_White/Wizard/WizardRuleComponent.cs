using Content.Server.RoundEnd;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Random;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Wizard;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class WizardRuleComponent : Component
{
    public readonly List<EntityUid> WizardMinds = new();

    [DataField("points")]
    public int Points = 10; //TODO: wizard shop prototype

    [DataField] public int MinObjectives = 5;
    [DataField] public int MaxObjectives = 5;

    [DataField("wizardRoleProto")]
    public ProtoId<AntagPrototype> WizardRoleProto = "WizardRole";

    public readonly RoundEndBehavior RoundEndBehavior = RoundEndBehavior.InstantEnd;

    [DataField]
    public string RoundEndTextSender = "comms-console-announcement-title-centcom";

    [DataField]
    public string RoundEndTextShuttleCall = "wizard-no-more-threat-announcement-shuttle-call";

    [DataField]
    public string RoundEndTextAnnouncement = "wizard-no-more-threat-announcement";

    [DataField]
    public TimeSpan EvacShuttleTime = TimeSpan.FromMinutes(5);

    [DataField]
    public ProtoId<WeightedRandomPrototype> ObjectiveGroup = "WizardObjectiveGroups";
}
