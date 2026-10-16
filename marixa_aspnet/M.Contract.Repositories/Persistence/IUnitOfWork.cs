namespace M.Contract.Repositories.Persistence
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<T> GetRepository<T>() where T : class;
        void Save();
        Task SaveAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollBackAsync(CancellationToken cancellationToken = default);
        void BeginTransaction();
        void CommitTransaction();
        void RollBack();
    }
}
