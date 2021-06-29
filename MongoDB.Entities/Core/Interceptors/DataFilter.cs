using System;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace MongoDB.Entities.Interceptors
{
    public interface IDataFiltered
    {

    }
    /// <summary>
    /// invoke before entities are queried out
    /// </summary>
    public interface IDataFilter
    {
        FilterDefinition<T> Apply<T>(FilterDefinition<T> filter);
    }

    public abstract class DataFilter<T> : IDataFilter where T : IDataFiltered
    {
        public abstract FilterDefinition<T> Apply(FilterDefinition<T> filter);

        FilterDefinition<T1> IDataFilter.Apply<T1>(FilterDefinition<T1> filter)
        {
            return this.Apply(filter as FilterDefinition<T>) as FilterDefinition<T1>;
        }
    }

    public class DelegateDataFilter<T> : DataFilter<T> where T : IDataFiltered
    {
        public Expression<Func<T, bool>> FilterExpression { get; }

        public DelegateDataFilter(Expression<Func<T,bool>> filterExpression)
        {
            FilterExpression = filterExpression;
        }
        public override FilterDefinition<T> Apply(FilterDefinition<T> filter)
        {
            filter &= FilterExpression;
            return filter;
        }
    }
}