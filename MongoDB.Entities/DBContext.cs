﻿using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Interceptors;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents a transaction used to carry out inter-related write operations.
    /// <para>TIP: Remember to always call .Dispose() after use or enclose in a 'Using' statement.</para>
    /// <para>IMPORTANT: Use the methods on this transaction to perform operations and not the methods on the DB class.</para>
    /// </summary>
    public class DbContext : IDisposable
    {
        protected internal IClientSessionHandle
            session; //this will be set by Transaction class when inherited. otherwise null.

        public IClientSessionHandle Session
        {
            get => session;
        }
        /// <summary>
        /// static interceptors shortcut property.
        /// </summary>
        public IReadOnlyList<IDataFilter> DataFilters => DB.DataFilters.Values.ToList();
        public IReadOnlyList<ISaveInterceptor> SaveInterceptors => DB.SaveInterceptors.Values.ToList();

        /// <summary>
        /// Instantiates and begins a transaction.
        /// </summary>
        /// <param name="database">The name of the database to use for this transaction. default db is used if not specified</param>
        /// <param name="transactional"></param>
        /// <param name="options">Client session options for this transaction</param>
        public DbContext(string database = default, bool transactional = false, ClientSessionOptions options = null)
        {
            session = DB.Database(database).Client.StartSession(options);
            Session.StartTransaction(new TransactionOptions(new Optional<ReadConcern>(ReadConcern.Local)));
        }

        /// <summary>
        /// Gets an accurate count of how many entities are matched for a given expression/filter in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="expression">A lambda expression for getting the count for a subset of the data</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<long> CountAsync<T>(Expression<Func<T, bool>> expression,
            CancellationToken cancellation = default) where T : IEntity
        {
            return DB.CountAsync(expression, session, cancellation);
        }

        /// <summary>
        /// Register interceptors
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="interceptors"></param>
        /// <returns></returns>
        public virtual void RegisterSaveInterceptors(params ISaveInterceptor[] interceptors)
        {
            DB.RegisterSaveInterceptors(interceptors);
        }

        public virtual void RegisterDataFilters(params IDataFilter[] dataFilters)
        {
            DB.RegisterDataFilters(dataFilters);
        }


        public virtual void AttachContextSession(IEntity entity)
        {
            this.session?.Attach(entity);
        }

        /// <summary>
        /// Gets an accurate count of how many total entities are in the collection for a given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<long> CountAsync<T>(CancellationToken cancellation = default) where T : IEntity
        {
            return DB.CountAsync<T>(_ => true, session, cancellation);
        }

        /// <summary>
        /// Gets an accurate count of how many total entities are in the collection for a given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="filter">A filter definition</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<long> CountAsync<T>(FilterDefinition<T> filter, CancellationToken cancellation = default)
            where T : IEntity
        {
            return DB.CountAsync(filter, session, cancellation);
        }

        /// <summary>
        /// Gets an accurate count of how many total entities are in the collection for a given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<long> CountAsync<T>(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter,
            CancellationToken cancellation = default) where T : IEntity
        {
            return DB.CountAsync(filter, session, cancellation);
        }

        /// <summary>
        /// Starts an update command for the given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        public virtual Update<T> Update<T>() where T : IEntity
        {
            return new Update<T>(session);
        }

        /// <summary>
        /// Starts an update-and-get command for the given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        public virtual UpdateAndGet<T> UpdateAndGet<T>() where T : IEntity
        {
            return new UpdateAndGet<T>(session);
        }

        /// <summary>
        /// Starts an update-and-get command with projection support for the given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TProjection">The type of the end result</typeparam>
        public virtual UpdateAndGet<T, TProjection> UpdateAndGet<T, TProjection>() where T : IEntity
        {
            return new UpdateAndGet<T, TProjection>(session);
        }

        /// <summary>
        /// Starts a find command for the given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        public virtual Find<T> Find<T>() where T : IEntity
        {
            return new Find<T>(session);
        }

        /// <summary>
        /// Starts a find command with projection support for the given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TProjection">The type of the end result</typeparam>
        public virtual Find<T, TProjection> Find<T, TProjection>() where T : IEntity
        {
            return new Find<T, TProjection>(session);
        }

        /// <summary>
        /// Exposes the MongoDB collection for the given entity type as IAggregateFluent in order to facilitate Fluent queries in the transaction sope.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        public virtual IAggregateFluent<T> Fluent<T>(AggregateOptions options = null) where T : IEntity
        {
            return DB.Fluent<T>(options, session);
        }

        /// <summary>
        /// Exposes the MongoDB collection for the given entity type as IQueryable in order to facilitate LINQ queries in the transaction scope.
        /// </summary>
        /// <param name="options">The aggregate options</param>
        /// <typeparam name="T">The type of entity</typeparam>
        public virtual IMongoQueryable<T> Queryable<T>(AggregateOptions options = null) where T : IEntity
        {
            return DB.Queryable<T>(options, session);
        }

        /// <summary>
        /// Executes an aggregation pipeline in the transaction scope by supplying a 'Template' object.
        /// Gets a cursor back as the result.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<IAsyncCursor<TResult>> PipelineCursorAsync<T, TResult>(Template<T, TResult> template,
            AggregateOptions options = null, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.PipelineCursorAsync(template, options, session, cancellation);
        }

        /// <summary>
        /// Executes an aggregation pipeline in the transaction scope by supplying a 'Template' object.
        /// Gets a list back as the result.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<List<TResult>> PipelineAsync<T, TResult>(Template<T, TResult> template,
            AggregateOptions options = null, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.PipelineAsync(template, options, session, cancellation);
        }

        /// <summary>
        /// Executes an aggregation pipeline in the transaction scope by supplying a 'Template' object.
        /// Gets a single or default value as the result.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<TResult> PipelineSingleAsync<T, TResult>(Template<T, TResult> template,
            AggregateOptions options = null, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.PipelineSingleAsync(template, options, session, cancellation);
        }

        /// <summary>
        /// Executes an aggregation pipeline in the transaction scope by supplying a 'Template' object.
        /// Gets the first or default value as the result.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<TResult> PipelineFirstAsync<T, TResult>(Template<T, TResult> template,
            AggregateOptions options = null, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.PipelineFirstAsync(template, options, session, cancellation);
        }

        /// <summary>
        /// Start a fluent aggregation pipeline with a $GeoNear stage with the supplied parameters in the transaction scope.
        /// </summary>
        /// <param name="NearCoordinates">The coordinates from which to find documents from</param>
        /// <param name="DistanceField">x => x.Distance</param>
        /// <param name="Spherical">Calculate distances using spherical geometry or not</param>
        /// <param name="MaxDistance">The maximum distance in meters from the center point that the documents can be</param>
        /// <param name="MinDistance">The minimum distance in meters from the center point that the documents can be</param>
        /// <param name="Limit">The maximum number of documents to return</param>
        /// <param name="Query">Limits the results to the documents that match the query</param>
        /// <param name="DistanceMultiplier">The factor to multiply all distances returned by the query</param>
        /// <param name="IncludeLocations">Specify the output field to store the point used to calculate the distance</param>
        /// <param name="IndexKey"></param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <typeparam name="T">The type of entity</typeparam>
        public virtual IAggregateFluent<T> GeoNear<T>(Coordinates2D NearCoordinates,
            Expression<Func<T, object>> DistanceField, bool Spherical = true, int? MaxDistance = null,
            int? MinDistance = null, int? Limit = null, BsonDocument Query = null, int? DistanceMultiplier = null,
            Expression<Func<T, object>> IncludeLocations = null, string IndexKey = null,
            AggregateOptions options = null) where T : IEntity
        {
            return DB.FluentGeoNear(NearCoordinates, DistanceField, Spherical, MaxDistance, MinDistance, Limit, Query,
                DistanceMultiplier, IncludeLocations, IndexKey, options, session);
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
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<UpdateResult> SaveOnlyAsync<T>(T entity, Expression<Func<T, object>> members,
            CancellationToken cancellation = default) where T : IEntity
        {
            return DB.SaveOnlyAsync(entity, members, session, cancellation);
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
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<BulkWriteResult<T>> SaveOnlyAsync<T>(IEnumerable<T> entities,
            Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.SaveOnlyAsync(entities, members, session, cancellation);
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
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<UpdateResult> SaveExceptAsync<T>(T entity, Expression<Func<T, object>> members,
            CancellationToken cancellation = default) where T : IEntity
        {
            return DB.SaveExceptAsync(entity, members, session, cancellation);
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
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<BulkWriteResult<T>> SaveExceptAsync<T>(IEnumerable<T> entities,
            Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.SaveExceptAsync(entities, members, session, cancellation);
        }

        /// <summary>
        /// Saves an entity partially while excluding some properties in the transaction scope.
        /// The properties to be excluded can be specified using the [Preserve] or [DontPreserve] attributes.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<UpdateResult> SavePreservingAsync<T>(T entity, CancellationToken cancellation = default)
            where T : IEntity
        {
            return DB.SavePreservingAsync(entity, session, cancellation);
        }

        /// <summary>
        /// Deletes a single entity from MongoDB in the transaction scope.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="id">The Id of the entity to delete</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<DeleteResult> DeleteAsync<T>(string id, CancellationToken cancellation = default)
            where T : IEntity
        {
            return DB.DeleteAsync<T>(id, session, cancellation);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB in the transaction scope
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<DeleteResult> DeleteAsync<T>(Expression<Func<T, bool>> expression,
            CancellationToken cancellation = default) where T : IEntity
        {
            return DB.DeleteAsync(expression, session, cancellation);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB in the transaction scope
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="Ids">An IEnumerable of entity Ids</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<DeleteResult> DeleteAsync<T>(IEnumerable<string> Ids,
            CancellationToken cancellation = default) where T : IEntity
        {
            return DB.DeleteAsync<T>(Ids, session, cancellation);
        }

        /// <summary>
        /// Start a fluent aggregation pipeline with a $text stage with the supplied parameters in the transaction scope.
        /// <para>TIP: Make sure to define a text index with DB.Index&lt;T&gt;() before searching</para>
        /// </summary>
        /// <param name="searchType">The type of text matching to do</param>
        /// <param name="searchTerm">The search term</param>
        /// <param name="caseSensitive">Case sensitivity of the search (optional)</param>
        /// <param name="diacriticSensitive">Diacritic sensitivity of the search (optional)</param>
        /// <param name="language">The language for the search (optional)</param>
        /// <param name="options">Options for finding documents (not required)</param>
        public virtual IAggregateFluent<T> FluentTextSearch<T>(Search searchType, string searchTerm,
            bool caseSensitive = false, bool diacriticSensitive = false, string language = null,
            AggregateOptions options = null) where T : IEntity
        {
            return DB.FluentTextSearch<T>(searchType, searchTerm, caseSensitive, diacriticSensitive, language, options,
                session);
        }

        /// <summary>
        /// Commits a transaction to MongoDB
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public async Task CommitAsync(CancellationToken cancellation = default)
        {
            await Session.CommitTransactionAsync(cancellation);
            if (this.OnCommitted != default)
            {
                await this.OnCommitted.Invoke(this);
            }
        }
        public event Func<DbContext,Task> OnCommitted;
        /// <summary>
        /// Aborts and rolls back a transaction
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task AbortAsync(CancellationToken cancellation = default) => Session.AbortTransactionAsync(cancellation);

        #region IDisposable Support

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    session.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        public void AttachContextSession(IEnumerable<IEntity> entities)
        {
            foreach (var entity in entities)
            {
                entity.Session = this.Session;
            }
        }


    }
}