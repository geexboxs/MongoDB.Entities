using MongoDB.Bson.Serialization.Attributes;
using System;
using MongoDB.Driver;

namespace MongoDB.Entities.Tests.Models
{
    public class CustomerWithCustomId : IEntity
    {
        [BsonId]
        public string Id { get; set; }

        IClientSessionHandle IEntity.Session { get; set; }
        public DateTime CreatedOn { get; set; }

        public string GenerateNewId()
            => $"{Guid.NewGuid()}-{DateTime.UtcNow.Ticks}";
    }
}
