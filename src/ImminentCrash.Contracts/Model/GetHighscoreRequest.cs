using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ImminentCrash.Contracts.Model
{
    [DataContract]
    public class GetHighscoreRequest
    {
        [DataMember(Order = 1)]
        public Guid SessionId { get; set; }
    }
}
