using Content.Shared.Random;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Wizard;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class WizardRuleComponent : Component
{
    [DataField("minPlayers")]
    public int MinPlayers = 20;

    [DataField("announcementOnWizardDeath")]
    public bool AnnouncementOnWizardDeath = true;

    [DataField]
    public string WizardKilledText = "nuke-ops-no-more-threat-announcement-shuttle-call";

    [DataField("points")]
    public int Points = 10; //TODO: wizard shop prototype

    [DataField("wizardRoleProto")]
    public ProtoId<AntagPrototype> WizardRoleProto = "WizardRole"; //TODO: wizard role prototype

    [DataField("wizardSpawnPointProto")]
    public EntProtoId SpawnPointProto = "WizardSpawnPoint"; //TODO: wizard ghost role prototype

    [DataField]
    public EntProtoId GhostSpawnPointProto = "SpawnPointGhostWizard"; //TODO

    [DataField("startingGear")]
    public ProtoId<StartingGearPrototype> StartingGear = "WizardStartingGear"; //TODO: wizard starting gear prototype

    [DataField("spawnShuttle")]
    public bool SpawnShuttle = true;

    [DataField]
    public EntityUid? ShuttleMap;

    [DataField("shuttlePath")]
    public string ShuttlePath = "a"; //TODO: shuttle path

    /// <summary>
    /// Maybe erase this
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomPrototype> ObjectiveGroup = "TraitorObjectiveGroups"; //TODO: wizard objectives' prototype, not traitor objectives

    /// <summary>
    ///     Path to antagonist alert sound.
    /// </summary>
    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/traitor_start.ogg");
}
