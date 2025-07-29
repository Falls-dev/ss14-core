using Robust.Shared.Serialization;

namespace Content.Shared.DNAConsole
{
    [Serializable, NetSerializable]
    public sealed class DNAConsoleBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string? ModifierBodyInfo;
        public readonly ModifierStatus ModifierStatus;
        public readonly bool ModifierConnected;
        public readonly bool ModifierInRange;
        public DNAConsoleBoundUserInterfaceState(string? scannerBodyInfo, ModifierStatus cloningStatus, bool scannerConnected, bool scannerInRange)
        {
            ModifierBodyInfo = scannerBodyInfo;
            ModifierStatus = cloningStatus;
            ModifierConnected = scannerConnected;
            ModifierInRange = scannerInRange;
        }
    }

    [Serializable, NetSerializable]
    public enum ModifierStatus : byte
    {
        Ready,
        ModifierEmpty,
        ModifierOccupantAlive,
        OccupantMetaphyiscal,
        ModifierOccupied
    }

    [Serializable, NetSerializable]
    public enum DNAConsoleUiKey : byte
    {
        Key
    }

    [Serializable, NetSerializable]
    public enum UiButton : byte
    {
        Clone,
        Eject
    }

    [Serializable, NetSerializable]
    public sealed class UiButtonPressedMessage : BoundUserInterfaceMessage
    {
        public readonly UiButton Button;

        public UiButtonPressedMessage(UiButton button)
        {
            Button = button;
        }
    }
}
