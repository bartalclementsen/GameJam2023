using System.Runtime.Serialization;

namespace ImminentCrash.Contracts.Model;

[DataContract]
public record QuitGameRequest
{
    [DataMember(Order = 1)]
    public Guid SessionId { get; init; } = Guid.Empty;
}
