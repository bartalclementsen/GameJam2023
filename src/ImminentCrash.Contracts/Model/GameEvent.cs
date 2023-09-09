using System.Runtime.Serialization;

namespace ImminentCrash.Contracts.Model;

[DataContract]
public record GameEvent
{
    [DataMember(Order = 1)]
    public bool IsDead { get; init; }

    [DataMember(Order = 2)]
    public decimal CurrentBalance { get; init; }

    [DataMember(Order = 3)]
    public IEnumerable<BalanceMovement>? BalanceMovements { get; init; }
}

[DataContract]
public record BalanceMovement
{
    [DataMember(Order = 1)]
    public decimal Amount { get; init; }

    [DataMember(Order = 2)]
    public string Name { get; init; } = string.Empty;
}
