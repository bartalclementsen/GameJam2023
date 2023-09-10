using System.Runtime.Serialization;

namespace ImminentCrash.Contracts.Model
{
    [DataContract]
    public class CreateHighscoreRequest
    {
        [DataMember(Order = 1)]
        public Guid SessionId { get; set; }

        [DataMember(Order = 2)]
        public string Name { get; set; } = string.Empty;
    }
}