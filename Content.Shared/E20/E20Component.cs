using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.E20;
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class E20Component : Component
{

    /// <summary>
    ///     Checks activation of dice to prevent double-triple-.. activation in case of double-triple-.. using dice in hands
    ///     Or rolling value of dice again after activation.
    /// </summary>

    public bool IsActivated = false;

    /// <summary>
    /// Person who used E20. Required when 1 is rolled.
    /// </summary>

    public EntityUid LastUser;

    [DataField]
    [AutoNetworkedField]
    public  List<string> Events = new();

    /// <summary>
    ///     Delay for dice action(Gib or Explosion).
    /// </summary>
    [DataField]
    public float Delay = 3;

    [DataField]
    public SoundSpecifier Sound { get; private set; } = new SoundCollectionSpecifier("Dice");

    [DataField]

    public SoundSpecifier SoundDie { get; private set; } = new SoundPathSpecifier("/Audio/Items/E20/e20_1.ogg");

    [DataField]
    public SoundSpecifier Beep = new SoundPathSpecifier("/Audio/Machines/Nuke/general_beep.ogg");

    /// <summary>
    ///     Multiplier for the value  of a die. Applied after the <see cref="Offset"/>.
    /// </summary>
    [DataField]
    public int Multiplier { get; private set; } = 1;

    /// <summary>
    ///     Quantity that is subtracted from the value of a die. Can be used to make dice that start at "0". Applied
    ///     before the <see cref="Multiplier"/>
    /// </summary>
    [DataField]
    public int Offset { get; private set; } = 0;

    [DataField]
    public int Sides { get; private set; } = 20;

    /// <summary>
    ///     The currently displayed value.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int CurrentValue { get; set; } = 20;


}
