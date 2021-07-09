using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MongoDB.Driver;
using MongoDB.Entities;

namespace MongoDB.Driver
{
    public static class ClientSessionHandleExtensions
    {
        public static void Attach<TEntity>(this IClientSessionHandle session, TEntity entity) where TEntity : IEntity
        {
            if (entity != null) entity.Session = session;
        }
        public static void Attach<TEntity>(this IClientSessionHandle session, IEnumerable<TEntity> entities) where TEntity : IEntity
        {
            foreach (IEntity entity in entities)
            {
                if (entity != null) entity.Session = session;
            }
        }
    }
}
