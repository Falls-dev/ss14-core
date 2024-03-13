using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    public sealed partial class PatchComponent : SharedPatchComponent
    {
        /// <summary>
        /// We DO NOT want to use pathes on beakers or something like that
        /// Because this is at least illogical
        /// </summary>
        [DataField("onlyMobs")]
        public bool OnlyMobs = true;
    }
}
