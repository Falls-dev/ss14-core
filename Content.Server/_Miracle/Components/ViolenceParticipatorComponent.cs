namespace Content.Server._Miracle.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class ViolenceParticipatorComponent : Component
{
    /// <summary>
    /// List of factions in this gamemode.
    /// </summary>
    [DataField]
    public EntityUid? MatchUid { get; private set; } = null;
}
