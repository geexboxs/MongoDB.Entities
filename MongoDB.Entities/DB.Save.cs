﻿using MongoDB.Driver;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Entities.Interceptors;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        private static readonly BulkWriteOptions unOrdBlkOpts = new BulkWriteOptions { IsOrdered = false };
        private static readonly UpdateOptions updateOptions = new UpdateOptions { IsUpsert = true };
        public static ConcurrentDictionary<Type, ISaveInterceptor> SaveInterceptors { get; private set; } = new()
        {
        };
        public static ConcurrentDictionary<Type, IDataFilter> DataFilters { get; private set; } = new()
        {
        };
        public static void RegisterSaveInterceptors(params ISaveInterceptor[] interceptors)
        {
            foreach (var interceptor in interceptors)
            {
                if (!interceptor.GetType().BaseType.Name.EndsWith("Interceptor`1"))
                {
                    throw new NotSupportedException("interceptors must directly inherit abstract Interceptor<>");
                }

                DB.SaveInterceptors.AddOrUpdate(interceptor.GetType().BaseType.GenericTypeArguments[0], interceptor, (_, _) => interceptor);
            }
        }

        public static void RegisterDataFilters(params IDataFilter[] dataFilters)
        {
            foreach (var dataFilter in dataFilters)
            {
                if (!dataFilter.GetType().BaseType.Name.EndsWith("DataFilter`1"))
                {
                    throw new NotSupportedException("interceptors must directly inherit abstract Interceptor<>");
                }

                DB.DataFilters.AddOrUpdate(dataFilter.GetType().BaseType.GenericTypeArguments[0], dataFilter, (_, _) => dataFilter);
            }
        }
        /// <summary>
        /// Saves a complete entity replacing an existing entity or creating a new one if it does not exist. 
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is replaced.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">And optional cancellation token</param>
        public static Task<ReplaceOneResult> SaveAsync<T>(T entity, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            PrepareForSave(entity);

            return session == null
                   ? Collection<T>().ReplaceOneAsync(x => x.Id == entity.Id, entity, new ReplaceOptions { IsUpsert = true }, cancellation)
                   : Collection<T>().ReplaceOneAsync(session, x => x.Id == entity.Id, entity, new ReplaceOptions { IsUpsert = true }, cancellation);
        }

        /// <summary>
        /// Saves a batch of complete entities replacing existing ones or creating new ones if they do not exist. 
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is replaced.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">And optional cancellation token</param>
        public static Task<BulkWriteResult<T>> SaveAsync<T>(IEnumerable<T> entities, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            var models = new List<WriteModel<T>>();
            foreach (var ent in entities)
            {
                PrepareForSave(ent);

                var upsert = new ReplaceOneModel<T>(
                        filter: Builders<T>.Filter.Eq(e => e.Id, ent.Id),
                        replacement: ent)
                { IsUpsert = true };
                models.Add(upsert);
            }

            return session == null
                   ? Collection<T>().BulkWriteAsync(models, unOrdBlkOpts, cancellation)
                   : Collection<T>().BulkWriteAsync(session, models, unOrdBlkOpts, cancellation);
        }

        /// <summary>
        /// Saves an entity partially with only the specified subset of properties. 
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be saved can be specified with a 'New' expression. 
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<UpdateResult> SaveOnlyAsync<T>(T entity, Expression<Func<T, object>> members, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return SavePartial(entity, members, session, cancellation);
        }

        /// <summary>
        /// Saves a batch of entities partially with only the specified subset of properties. 
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be saved can be specified with a 'New' expression. 
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The batch of entities to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<BulkWriteResult<T>> SaveOnlyAsync<T>(IEnumerable<T> entities, Expression<Func<T, object>> members, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return SavePartial(entities, members, session, cancellation);
        }

        /// <summary>
        /// Saves an entity partially excluding the specified subset of properties. 
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be excluded can be specified with a 'New' expression. 
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<UpdateResult> SaveExceptAsync<T>(T entity, Expression<Func<T, object>> members, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return SavePartial(entity, members, session, cancellation, true);
        }

        /// <summary>
        /// Saves a batch of entities partially excluding the specified subset of properties. 
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be excluded can be specified with a 'New' expression. 
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The batch of entities to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<BulkWriteResult<T>> SaveExceptAsync<T>(IEnumerable<T> entities, Expression<Func<T, object>> members, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return SavePartial(entities, members, session, cancellation, true);
        }

        /// <summary>
        /// Saves an entity partially while excluding some properties. 
        /// The properties to be excluded can be specified using the [Preserve] or [DontPreserve] attributes.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<UpdateResult> SavePreservingAsync<T>(T entity, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            var propsToUpdate = Cache<T>.UpdatableProps(entity);

            IEnumerable<string> propsToPreserve = default;

            var dontProps = propsToUpdate.Where(p => p.IsDefined(typeof(DontPreserveAttribute), false)).Select(p => p.Name);
            var presProps = propsToUpdate.Where(p => p.IsDefined(typeof(PreserveAttribute), false)).Select(p => p.Name);

            if (dontProps.Any() && presProps.Any())
                throw new NotSupportedException("[Preseve] and [DontPreserve] attributes cannot be used together on the same entity!");

            if (dontProps.Any())
                propsToPreserve = propsToUpdate.Where(p => !dontProps.Contains(p.Name)).Select(p => p.Name);

            if (presProps.Any())
                propsToPreserve = propsToUpdate.Where(p => presProps.Contains(p.Name)).Select(p => p.Name);

            if (!propsToPreserve.Any())
                throw new ArgumentException("No properties are being preserved. Please use .Save() method instead!");

            propsToUpdate = propsToUpdate.Where(p => !propsToPreserve.Contains(p.Name));

            if (!propsToUpdate.Any())
                throw new ArgumentException("At least one property must be not preserved!");

            var defs = new Collection<UpdateDefinition<T>>();

            foreach (var p in propsToUpdate)
            {
                if (p.Name == Cache<T>.ModifiedOnPropName)
                    defs.Add(Builders<T>.Update.CurrentDate(Cache<T>.ModifiedOnPropName));
                else
                    defs.Add(Builders<T>.Update.Set(p.Name, p.GetValue(entity)));
            }

            return
                session == null
                ? Collection<T>().UpdateOneAsync(e => e.Id == entity.Id, Builders<T>.Update.Combine(defs), updateOptions, cancellation)
                : Collection<T>().UpdateOneAsync(session, e => e.Id == entity.Id, Builders<T>.Update.Combine(defs), updateOptions, cancellation);
        }

        private static void PrepareForSave<T>(T entity) where T : IEntity
        {
            if (entity is ISaveIntercepted)
            {
                foreach (var (entityType, interceptor) in DB.SaveInterceptors)
                {
                    if (entityType.IsInstanceOfType(entity))
                    {
                        interceptor.Apply(entity);
                    }
                }
            }

            if (string.IsNullOrEmpty(entity.Id))
            {
                entity.Id = entity.GenerateNewId();
                entity.CreatedOn = DateTime.UtcNow;
            }

            if (entity is IModifiedOn modifiedOn)
                modifiedOn.ModifiedOn = DateTime.UtcNow;
        }

        private static IEnumerable<string> RootPropNames<T>(Expression<Func<T, object>> members) where T : IEntity
        {
            return (members?.Body as NewExpression)?.Arguments
                .Select(a => a.ToString().Split('.')[1]);
        }

        private static IEnumerable<UpdateDefinition<T>> BuildUpdateDefs<T>(T entity, Expression<Func<T, object>> members, bool excludeMode = false) where T : IEntity
        {
            var propNames = RootPropNames(members);

            if (!propNames.Any())
                throw new ArgumentException("Unable to get any properties from the members expression!");

            PrepareForSave(entity);

            var props = Cache<T>.UpdatableProps(entity);

            if (excludeMode)
                props = props.Where(p => !propNames.Contains(p.Name));
            else
                props = props.Where(p => propNames.Contains(p.Name));

            return props.Select(p => Builders<T>.Update.Set(p.Name, p.GetValue(entity)));
        }

        private static Task<UpdateResult> SavePartial<T>(T entity, Expression<Func<T, object>> members, IClientSessionHandle session, CancellationToken cancellation, bool excludeMode = false) where T : IEntity
        {
            return
                session == null
                ? Collection<T>().UpdateOneAsync(e => e.Id == entity.Id, Builders<T>.Update.Combine(BuildUpdateDefs(entity, members, excludeMode)), updateOptions, cancellation)
                : Collection<T>().UpdateOneAsync(session, e => e.Id == entity.Id, Builders<T>.Update.Combine(BuildUpdateDefs(entity, members, excludeMode)), updateOptions, cancellation);
        }

        private static Task<BulkWriteResult<T>> SavePartial<T>(IEnumerable<T> entities, Expression<Func<T, object>> members, IClientSessionHandle session, CancellationToken cancellation, bool excludeMode = false) where T : IEntity
        {
            var models = new List<WriteModel<T>>();

            foreach (var ent in entities)
            {
                var update = Builders<T>.Update.Combine(BuildUpdateDefs(ent, members, excludeMode));

                var upsert = new UpdateOneModel<T>(
                        filter: Builders<T>.Filter.Eq(e => e.Id, ent.Id),
                        update: update)
                { IsUpsert = true };
                models.Add(upsert);
            }

            return session == null
                ? Collection<T>().BulkWriteAsync(models, unOrdBlkOpts, cancellation)
                : Collection<T>().BulkWriteAsync(session, models, unOrdBlkOpts, cancellation);
        }
    }
}
