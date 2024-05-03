namespace Content.Server._White.Wizard;


[RegisterComponent]
public sealed partial class WizardComponent : Component
{
    [DataField]
    public bool AnnouncementOnWizardDeath = true;
}
