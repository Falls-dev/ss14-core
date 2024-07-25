using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Holosign
{
    [RegisterComponent]
    public sealed partial class HolosignProjectorComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("signProto", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string SignProto = "HolosignWetFloor";

        /// <summary>
        /// How much charge a single use expends.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField]
        public int Uses = 6;

        [ViewVariables(VVAccess.ReadWrite), DataField]
        public List<EntityUid?> Signs = new();

        [ViewVariables(VVAccess.ReadWrite), DataField]
        public TimeSpan UseDelay = TimeSpan.FromSeconds(0.5);
    }
}
