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

    [DataMember(Order = 4)]
    public string CurrentDateString { get; set; } = string.Empty;

    [DataMember(Order = 5)]
    public IEnumerable<CoinMovement>? CoinMovements { get; init; }

    [DataMember(Order = 6)]
    public IEnumerable<Coin>? NewCoins { get; init; }

    [DataMember(Order = 7)]
    public IEnumerable<Coin>? RemoveCoins { get; init; }

    [DataMember(Order = 8)]
    public IEnumerable<LivingCost>? NewLivingCosts { get; init; }

    [DataMember(Order = 9)]
    public IEnumerable<LivingCost>? RemoveLivingCosts { get; init; }
}

[DataContract]
public record BalanceMovement
{
    [DataMember(Order = 1)]
    public decimal Amount { get; init; }

    [DataMember(Order = 2)]
    public string Name { get; init; } = string.Empty;
}

[DataContract]
public record CoinMovement
{
    [DataMember(Order = 1)]
    public int Id { get; init; }


    [DataMember(Order = 2)]
    public decimal Amount { get; init; }
}

[DataContract]
public record Coin
{
    [DataMember(Order = 1)]
    public int Id { get; init; }


    [DataMember(Order = 2)]
    public string Name { get; init; } = string.Empty;
}

[DataContract]
public record LivingCost
{
    [DataMember(Order = 1)]
    public string Name { get; init; } = string.Empty;

    [DataMember(Order = 2)]
    public decimal Amount { get; init; } = 0;

    [DataMember(Order = 3)]
    public LivingCostType LivingCostType { get; init; } = LivingCostType.Daily;
}

public enum LivingCostType
{
    Daily,
    Weekly,
    Monthly
}


