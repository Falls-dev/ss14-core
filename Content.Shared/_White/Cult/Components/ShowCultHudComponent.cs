using Content.Shared._White.Cult.Interfaces;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Cult.Components;

[Virtual, RegisterComponent, NetworkedComponent]
public partial class ShowCultHudComponent : Component, IShowCultHud
{
}
