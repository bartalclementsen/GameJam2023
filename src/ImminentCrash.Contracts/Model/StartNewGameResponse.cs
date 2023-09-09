using System.Runtime.Serialization;

namespace ImminentCrash.Contracts.Model;

[DataContract]
public record CreateNewGameRequest
{
    // TODO: Add create game options
}

[DataContract]
public record CreateNewGameResponse
{
    [DataMember(Order = 1)]
    public Guid SessionId { get; set; } = Guid.Empty;
}

[DataContract]
public record StartGameRequest
{
    [DataMember(Order = 1)]
    public Guid SessionId { get; set; } = Guid.Empty;
}

[DataContract]
public record GameEvent
{
    // TODO: Add events
    public DateTime Time { get; set; }
}

[DataContract]
public record PauseGameRequest
{
    [DataMember(Order = 1)]
    public Guid SessionId { get; set; } = Guid.Empty;
}

[DataContract]
public record PauseGameResponse { }

[DataContract]
public record ContinueGameRequest
{
    [DataMember(Order = 1)]
    public Guid SessionId { get; set; } = Guid.Empty;
}

[DataContract]
public record ContinueGameResponse { }

[DataContract]
public record QuitGameRequest
{
    [DataMember(Order = 1)]
    public Guid SessionId { get; set; } = Guid.Empty;
}

[DataContract]
public record QuitGameResponse { }
