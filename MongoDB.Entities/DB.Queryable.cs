using System.Linq;

using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        /// <summary>
        /// Exposes the MongoDB collection for the given IEntity as an IQueryable in order to facilitate LINQ queries.
        /// </summary>
        /// <param name="options">The aggregate options</param>
        /// <param name="session">An optional session if used within a transaction</param>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static IMongoQueryable<T> Queryable<T>(AggregateOptions options = null, IClientSessionHandle session = null) where T : IEntity
        {

            var result = session == null
                   ? Collection<T>().AsQueryable(options)
                   : Collection<T>().AsQueryable(session, options);

            if (DataFilters.Any())
            {
                foreach (var (targetType, value) in DataFilters)
                {
                    if (targetType.IsAssignableFrom(typeof(T)))
                    {
                        var filter = new ExpressionFilterDefinition<T>(x => true);
                        value.Apply(filter);
                        result = result.Where(filter.Expression);
                    }
                }
            }

            return result;
        }
    }
}
