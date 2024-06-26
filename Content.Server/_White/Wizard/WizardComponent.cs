namespace Content.Server._White.Wizard;


[RegisterComponent]
public sealed partial class WizardComponent : Component
{
    [DataField("isRoundStartWizard")]
    public bool IsRoundStartWizard = false;
}
