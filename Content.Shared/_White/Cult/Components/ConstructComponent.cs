using Content.Shared._White.Cult.Interfaces;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Cult.Components;

[RegisterComponent]
public sealed partial class ConstructComponent : Component, ICultChat
{
    [DataField("actions")]
    public List<EntProtoId> Actions = new();

    [ViewVariables]
    public List<EntityUid?> ActionEntities = new();
}
