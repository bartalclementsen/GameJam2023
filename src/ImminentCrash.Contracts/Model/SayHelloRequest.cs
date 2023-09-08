using System.Runtime.Serialization;

namespace ImminentCrash.Contracts.Model;

[DataContract]
public record SayHelloRequest
{
    [DataMember(Order = 1)]
    public string Name { get; set; } = string.Empty;
}
