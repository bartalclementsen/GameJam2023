using System.Runtime.Serialization;

namespace ImminentCrash.Contracts.Model;

[DataContract]
public record QuitGameRequest
{
    [DataMember(Order = 1)]
    public Guid SessionId { get; init; } = Guid.Empty;
}

[DataContract]
public record BuyCoinsRequest
{
    [DataMember(Order = 1)]
    public Guid SessionId { get; init; } = Guid.Empty;

    [DataMember(Order = 2)]
    public int CoinId { get; init; }

    [DataMember(Order = 3)]
    public int Amount { get; init; }
}

[DataContract]
public record BuyCoinsResponse { }

[DataContract]
public record SellCoinRequest
{
    [DataMember(Order = 1)]
    public Guid SessionId { get; init; } = Guid.Empty;

    [DataMember(Order = 2)]
    public int CoinId { get; init; }

    [DataMember(Order = 3)]
    public int Amount { get; init; }
}

[DataContract]
public record SellCoinsResponse { }

