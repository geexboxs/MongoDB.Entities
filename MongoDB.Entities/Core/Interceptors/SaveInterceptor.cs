namespace MongoDB.Entities.Interceptors
{
    public interface ISaveIntercepted : IEntity
    {
    }
    public abstract class SaveInterceptor<T> : ISaveInterceptor where T : ISaveIntercepted
    {
        public abstract void Apply(T entity);

        void ISaveInterceptor.Apply(IEntity entity)
        {
            this.Apply((T)entity);
        }
    }
    /// <summary>
    /// invoke when entity is saved(not commit)
    /// </summary>
    public interface ISaveInterceptor
    {
        void Apply(IEntity entity);
    }
}