using Robust.Shared.Serialization;

namespace Content.Shared._White.Guardian;

public enum GuardianSelector : byte
{
    Assasin,
    Standart,
    Charger,
    Lighting
}

[Serializable, NetSerializable]
public enum GuardianSelectorUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class GuardianSelectorBUIState : BoundUserInterfaceState
{
    public IReadOnlyCollection<string> Ids { get; set; }

    public GuardianSelectorBUIState(IReadOnlyCollection<string> ids)
    {
        Ids = ids;
    }
}

[Serializable, NetSerializable]
public sealed class GuardianSelectorSelectedBuiMessage : BoundUserInterfaceMessage
{
    public GuardianSelector GuardianType;

    public GuardianSelectorSelectedBuiMessage(GuardianSelector guardianType)
    {
        GuardianType = guardianType;
    }
}
