using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities.Interceptors;

namespace MongoDB.Entities
{
    internal class InterceptedAndFiltered : Entity, IDataFiltered, ISaveIntercepted
    {
        public bool Test { get; set; }
    }
}