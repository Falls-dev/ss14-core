using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
namespace Content.Shared.Borer;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class BorerComponent : Component
{
    public string ActionInfest = "ActionInfest";

    public EntityUid? ActionInfestEntity;

    public string ActionStun = "ActionBorerStunVictim";

    public EntityUid? ActionStunEntity;

    public EntityUid Target;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public int Points = 0;
}
