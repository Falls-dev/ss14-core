using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

namespace Content.Client.Chemistry.Components
{
    [RegisterComponent]
    public sealed partial class PatchComponent : SharedPatchComponent
    {
        [ViewVariables]
        public FixedPoint2 CurrentVolume;
        [ViewVariables]
        public FixedPoint2 TotalVolume;
    }
}
