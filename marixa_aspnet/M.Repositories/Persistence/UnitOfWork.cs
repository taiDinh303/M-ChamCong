using System.Data;
using M.Contract.Repositories.Persistence;
using M.Repositories.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace M.Repositories.Persistence;

public sealed class UnitOfWork(DatabaseContext context) : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = new();
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public IGenericRepository<T> GetRepository<T>() where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_repositories.TryGetValue(typeof(T), out var repository))
            _repositories[typeof(T)] = repository = new GenericRepository<T>(context);
        return (IGenericRepository<T>)repository;
    }

    public void Save() => context.SaveChanges();
    public async Task SaveAsync(CancellationToken cancellationToken = default) =>
        await context.SaveChangesAsync(cancellationToken);

    public void BeginTransaction()
    {
        EnsureNoTransaction();
        _transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        EnsureNoTransaction();
        _transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
    }

    public void CommitTransaction()
    {
        var transaction = RequireTransaction();
        transaction.Commit();
        transaction.Dispose();
        _transaction = null;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = RequireTransaction();
        await transaction.CommitAsync(cancellationToken);
        await transaction.DisposeAsync();
        _transaction = null;
    }

    public void RollBack()
    {
        var transaction = RequireTransaction();
        transaction.Rollback();
        context.ChangeTracker.Clear();
        transaction.Dispose();
        _transaction = null;
    }

    public async Task RollBackAsync(CancellationToken cancellationToken = default)
    {
        var transaction = RequireTransaction();
        await transaction.RollbackAsync(cancellationToken);
        context.ChangeTracker.Clear();
        await transaction.DisposeAsync();
        _transaction = null;
    }

    private void EnsureNoTransaction()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_transaction != null) throw new InvalidOperationException("A transaction is already active.");
    }

    private IDbContextTransaction RequireTransaction() => _transaction
        ?? throw new InvalidOperationException("No transaction is active.");

    public void Dispose()
    {
        if (_disposed) return;
        _transaction?.Dispose();
        _repositories.Clear();
        // DatabaseContext is owned and disposed by the DI scope, not by this wrapper.
        _disposed = true;
    }
}
