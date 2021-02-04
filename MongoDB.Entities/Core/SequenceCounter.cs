using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace MongoDB.Entities
{
    [Name("[SEQUENCE_COUNTERS]")]
    internal class SequenceCounter : IEntity
    {
        [BsonId]
        public string Id { get; set; }

        IClientSessionHandle IEntity.Session { get; set; }

        [BsonRepresentation(BsonType.Int64)]
        public ulong Count { get; set; }

        public string GenerateNewId()
            => throw new System.NotImplementedException();
    }
}
