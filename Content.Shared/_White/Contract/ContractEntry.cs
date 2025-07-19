using Robust.Shared.Serialization;

namespace Content.Shared._White.Contract;

[Serializable, NetSerializable]
public sealed partial class ContractEntry
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ContractDifficulty Difficulty { get; set; } = ContractDifficulty.Easy;

    public int TCReward { get; set; }

    public int ReputationReward { get; set; }

    public ContractStatus Status { get; set; } = ContractStatus.Available;

    public EntityUid Target { get; set; }

    public ContractEntry(string id, string name, string description, ContractDifficulty difficulty, int tcReward, int reputationReward, ContractStatus status, EntityUid target)
    {
        Id = id;
        Name = name;
        Description = description;
        Difficulty = difficulty;
        TCReward = tcReward;
        ReputationReward = reputationReward;
        Status = status;
        Target = target;
    }
}

public enum ContractStatus : byte
{
    Available,
    Active,
    Completed,
    Cancelled
}

public enum ContractDifficulty : byte
{
    Easy,
    Medium,
    Hard
}

