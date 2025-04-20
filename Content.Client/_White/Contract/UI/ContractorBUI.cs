namespace Content.Client._White.Contract.UI;

public sealed class ContractorBUI : BoundUserInterface
{
    private ContractorWindow? _window;

    public ContractorBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }
}
