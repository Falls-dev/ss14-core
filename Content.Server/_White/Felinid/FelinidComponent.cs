using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Abilities.Felinid
{
    [RegisterComponent]
    public sealed partial class FelinidComponent : Component
    {
        /// <summary>
        /// The hairball prototype to use.
        /// </summary>
        [DataField("hairballPrototype")]
        public EntProtoId HairballPrototype = "Hairball";

        [DataField]
        public SoundSpecifier MouseEatingSound = new SoundCollectionSpecifier("eating");

        public EntityUid? HairballAction;

        public EntityUid? EatMouseAction;

        public EntityUid? PotentialTarget = null;
    }
}
