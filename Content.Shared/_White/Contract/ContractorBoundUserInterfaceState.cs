using Robust.Shared.Serialization;

namespace Content.Shared._White.Contract
{
    [Serializable, NetSerializable]
    public enum ContractorUplinkUiKey
    {
        Key
    }

    public sealed class ContractorBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly List<ContractEntry> Contracts;
        public readonly int Reputation;

        public ContractorBoundUserInterfaceState(List<ContractEntry> contracts, int reputation)
        {
            Contracts = contracts;
            Reputation = reputation;
        }
    }
}
