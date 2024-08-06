using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server._White.AutoRegenReagent
{
    [RegisterComponent]
    public sealed partial class AutoRegenReagentComponent : Component
    {
        [DataField("solution", required: true)]
        public string? SolutionName = null; // we'll fail during tests otherwise

        [DataField("reagents", required: true)]
        public List<ReagentId> Reagents = default!;

        public string CurrentReagent = "";

        public int CurrentIndex = 0;

        public Entity<SolutionComponent>? Solution = default!;

        [DataField("interval")]
        public TimeSpan Interval = TimeSpan.FromSeconds(1);

        public TimeSpan NextUpdate = TimeSpan.Zero;

        [DataField("unitsPerInterval")]
        public float UnitsPerInterval = 1f;
    }
}
