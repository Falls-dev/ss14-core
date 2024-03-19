using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Administration;

namespace Content.Shared._White.MechComp;
public abstract class SharedMechCompDeviceSystem : EntitySystem
{
}

[RegisterComponent]
public partial class BaseMechCompComponent : Component
{
    [DataField]
    public bool hasConfig = true;
}





[RegisterComponent]
public sealed partial class MechCompButtonComponent : Component
{
    [DataField("clickSound")]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");
    public object pressedAnimation = default!;

    [DataField]
    public string outSignal = "1";
}
[RegisterComponent]
public sealed partial class MechCompSpeakerComponent : Component
{
    public object speakAnimation = default!;

    [DataField]
    public bool inRadio = false;
    [DataField]
    public string name = "";
}

[RegisterComponent]
public sealed partial class MechCompTeleportComponent : Component
{
    public object firingAnimation = default!; // i genuinely cannot believe people over at wizden think this is okay
    public object glowAnimation = default!;

    [DataField]
    public int teleId = -1;

    [DataField("maxDistance", serverOnly: true)]
    public float MaxDistance = 25f;
}

[Serializable, NetSerializable]
public enum MechCompDeviceVisualLayers : byte
{
    Base = 0, // base sprite, changed by anchoring visualiser
    Effect1,
    Effect2,
    Effect3,
    Effect4,
    Effect5,
    Effect6
}
[Serializable, NetSerializable]
public enum MechCompDeviceVisuals : byte
{
    Mode = 0,
    Anchored
}


[RegisterComponent]
public sealed partial class MechCompMathComponent : Component
{
    [DataField]
    public string mode = "A+B"; // keep in sync with the list of available ops in serverside system
    [DataField]
    public float A = 0;
    [DataField]
    public float B = 0;
}


[RegisterComponent]
public sealed partial class MechCompPressurePadComponent : Component
{
    [DataField]
    public bool reactToMobs = true;
    [DataField]
    public bool reactToItems = false;

}

[RegisterComponent]
public sealed partial class MechCompComparerComponent : Component
{
    [DataField]
    public string mode = "A==B"; // keep in sync with the list of available ops in serverside system
    [DataField]
    public string A = "0";
    [DataField]
    public string B = "0";
    [DataField]
    public string outputTrue = "1";
    [DataField]
    public string outputFalse = "1";
}

[RegisterComponent]
public sealed partial class MechCompTranseiverComponent : Component
{
    public object blinkAnimation = default!;
    [DataField]
    public int thisId = -1;
    [DataField]
    public int targetId = 0;
}
