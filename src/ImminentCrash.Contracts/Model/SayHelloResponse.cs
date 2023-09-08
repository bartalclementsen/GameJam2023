using System.Runtime.Serialization;

namespace ImminentCrash.Contracts.Model;

[DataContract]
public record SayHelloResponse
{
    [DataMember(Order = 1)]
    public string Message { get; set; } = string.Empty;
}
