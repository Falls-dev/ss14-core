namespace Content.Server.Genetics.Components
{
    [RegisterComponent]
    public sealed partial class DNAConsoleComponent : Component
    {
        public const string ScannerPort = "DNAModifierSender";

        [ViewVariables]
        public EntityUid? Modifier = null;

        /// Maximum distance between console and one if its machines
        [DataField("maxDistance")]
        public float MaxDistance = 4f;

        public bool ModifierInRange = true;
    }
}
