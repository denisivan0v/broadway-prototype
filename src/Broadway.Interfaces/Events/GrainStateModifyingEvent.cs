using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NuClear.Broadway.Interfaces.Events
{
    public struct GrainStateModifyingEvent
    {
        private static readonly JsonSerializerSettings SerializerSettings =
            new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };

        public GrainStateModifyingEvent(string grainType, long grainKey, int grainVersion)
        {
            GrainType = grainType;
            GrainKey = grainKey;
            GrainVersion = grainVersion;
        }

        public string GrainType { get; }
        public long GrainKey { get; }
        public int GrainVersion { get; }

        public static GrainStateModifyingEvent Deserialize(string json)
            => JsonConvert.DeserializeObject<GrainStateModifyingEvent>(json, SerializerSettings);

        public string Serialize() => JsonConvert.SerializeObject(this, SerializerSettings);
    }
}