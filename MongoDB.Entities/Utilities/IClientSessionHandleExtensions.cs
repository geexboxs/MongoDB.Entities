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
        public static void Attach(this IClientSessionHandle session, IEntity entity)
        {
            if (entity != null) entity.Session = session;
        }
    }
}
