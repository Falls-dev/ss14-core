using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._White.DeviceLinking.Silo.Components;

[RegisterComponent]
public sealed partial class BluespaceMaterialStorageComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string Hub = "MaterialHub";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string LocalStorage = "MaterialLocalStorage";
}
