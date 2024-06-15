using Content.Shared.DNAModifier;
using Robust.Shared.Containers;

namespace Content.Server.Genetics.Components
{
    [RegisterComponent]
    public sealed partial class DNAModifierComponent : SharedDNAModifierComponent
    {
        public const string ScannerPort = "DNAModifierReceiver";
        public ContainerSlot BodyContainer = default!;
        public EntityUid? ConnectedConsole;

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float CloningFailChanceMultiplier = 1f;
    }
}
