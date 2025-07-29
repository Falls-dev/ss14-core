using Content.Shared._RMC14.Medical.Surgery;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Medical.Surgery.Steps.Parts;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMSurgerySystem))]
public sealed partial class CMSkinRetractedComponent : Component {};
