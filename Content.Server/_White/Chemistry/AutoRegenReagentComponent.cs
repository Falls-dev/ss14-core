using Content.Shared.Chemistry.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._White.Chemistry
{
    [RegisterComponent, AutoGenerateComponentPause]
    public sealed partial class AutoRegenReagentComponent : Component
    {
        [DataField("solution", required: true), ViewVariables(VVAccess.ReadWrite)]
        public string SolutionName = string.Empty;
        
        [DataField("reagents", required: true)]
        public List<string> Reagents = default!;

        public string CurrentReagent = "";

        public int CurrentIndex = 0;

        /// <summary>
        /// The solution to add reagents to.
        /// </summary>
        [DataField("solutionRef")]
        public Entity<SolutionComponent>? Solution = null;

        /// <summary>
        /// The reagent(s) to be regenerated in the solution.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public Solution? Generated = default!;

        [DataField("unitsPerSecond")]
        public float RegenAmout = 5f;

        /// <summary>
        /// How long it takes to regenerate once.
        /// </summary>
        [DataField("duration"), ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan Duration = TimeSpan.FromSeconds(1);

        /// <summary>
        /// The time when the next regeneration will occur.
        /// </summary>
        [DataField("nextChargeTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
        [AutoPausedField]
        public TimeSpan NextRegenTime = TimeSpan.FromSeconds(0);
    }
}
