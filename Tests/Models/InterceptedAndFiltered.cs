using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities.Interceptors;

namespace MongoDB.Entities
{
    internal class InterceptedAndFiltered : IDataFiltered, ISaveIntercepted
    {
        public string Id { get; set; }
        public IClientSessionHandle Session { get; set; }
        public bool Test { get; set; }

        public string GenerateNewId()
        {
            return ObjectId.GenerateNewId().ToString();
        }
    }
}