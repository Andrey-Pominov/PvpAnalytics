using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using PaymentEntity = PaymentService.Core.Entities.Payment;
using PaymentService.Core.Enum;
using PaymentService.Core.Repositories;
using Xunit;

namespace PvpAnalytics.Tests.Payment;

public class PaymentServiceUnitTests
{
    [Fact]
    public async Task GetAsync_ReturnsPayment_WhenExists()
    {
        // Arrange
        var payment = new PaymentEntity
        {
            Id = 1,
            Amount = 100.00m,
            Status = PaymentStatus.Pending,
            UserId = "user-123",
            TransactionId = "txn-001",
            PaymentMethod = "CreditCard",
            CreatedAt = DateTime.UtcNow
        };

        var repository = new MockRepository<PaymentEntity>();
        repository.SetupGetById(payment.Id, payment);
        var service = new PaymentService.Application.Services.PaymentService(repository);

        // Act
        var result = await service.GetAsync([payment.Id]);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(payment.Id);
        result.TransactionId.Should().Be("txn-001");
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        var repository = new MockRepository<PaymentEntity>();
        var service = new PaymentService.Application.Services.PaymentService(repository);

        // Act
        var result = await service.GetAsync([999]);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllPayments()
    {
        // Arrange
        var payments = new List<PaymentEntity>
        {
            new() { Id = 1, Amount = 100.00m, Status = PaymentStatus.Pending, UserId = "user-1", TransactionId = "txn-001", PaymentMethod = "CreditCard", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Amount = 200.00m, Status = PaymentStatus.Completed, UserId = "user-2", TransactionId = "txn-002", PaymentMethod = "PayPal", CreatedAt = DateTime.UtcNow }
        };

        var repository = new MockRepository<PaymentEntity>();
        repository.SetupList(payments);
        var service = new PaymentService.Application.Services.PaymentService(repository);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task FindAsync_ReturnsFilteredPayments()
    {
        // Arrange
        var payments = new List<PaymentEntity>
        {
            new() { Id = 1, Amount = 100.00m, Status = PaymentStatus.Pending, UserId = "user-1", TransactionId = "txn-001", PaymentMethod = "CreditCard", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Amount = 200.00m, Status = PaymentStatus.Completed, UserId = "user-1", TransactionId = "txn-002", PaymentMethod = "PayPal", CreatedAt = DateTime.UtcNow }
        };

        var repository = new MockRepository<PaymentEntity>();
        repository.SetupListWithPredicate(p => p.UserId == "user-1", payments.Where(p => p.UserId == "user-1").ToList());
        var service = new PaymentService.Application.Services.PaymentService(repository);

        // Act
        var result = await service.FindAsync(p => p.UserId == "user-1");

        // Assert
        result.Should().HaveCount(2);
        result.All(p => p.UserId == "user-1").Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_CreatesPayment()
    {
        // Arrange
        var payment = new PaymentEntity
        {
            Amount = 150.00m,
            Status = PaymentStatus.Pending,
            UserId = "user-123",
            TransactionId = "txn-create",
            PaymentMethod = "CreditCard",
            CreatedAt = DateTime.UtcNow
        };

        var repository = new MockRepository<PaymentEntity>();
        var service = new PaymentService.Application.Services.PaymentService(repository);

        // Act
        var result = await service.CreateAsync(payment);

        // Assert
        result.Should().NotBeNull();
        repository.AddedItems.Should().Contain(payment);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesPayment()
    {
        // Arrange
        var payment = new PaymentEntity
        {
            Id = 1,
            Amount = 100.00m,
            Status = PaymentStatus.Pending,
            UserId = "user-123",
            TransactionId = "txn-001",
            PaymentMethod = "CreditCard",
            CreatedAt = DateTime.UtcNow
        };

        var repository = new MockRepository<PaymentEntity>();
        var service = new PaymentService.Application.Services.PaymentService(repository);

        // Act
        payment.Amount = 200.00m;
        payment.Status = PaymentStatus.Completed;
        await service.UpdateAsync(payment);

        // Assert
        repository.UpdatedItems.Should().Contain(payment);
    }

    [Fact]
    public async Task DeleteAsync_DeletesPayment()
    {
        // Arrange
        var payment = new PaymentEntity
        {
            Id = 1,
            Amount = 100.00m,
            Status = PaymentStatus.Pending,
            UserId = "user-123",
            TransactionId = "txn-001",
            PaymentMethod = "CreditCard",
            CreatedAt = DateTime.UtcNow
        };

        var repository = new MockRepository<PaymentEntity>();
        var service = new PaymentService.Application.Services.PaymentService(repository);

        // Act
        await service.DeleteAsync(payment);

        // Assert
        repository.DeletedItems.Should().Contain(payment);
    }
}

// Mock repository implementation for unit testing
public class MockRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private readonly Dictionary<long, TEntity> _entities = new();
    private readonly List<TEntity> _allEntities = [];
    private Func<Expression<Func<TEntity, bool>>, IReadOnlyList<TEntity>>? _predicateHandler;
    private long _nextId = 1;
    private static readonly PropertyInfo? IdProperty = typeof(TEntity).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);

    public List<TEntity> AddedItems { get; } = new();
    public List<TEntity> UpdatedItems { get; } = new();
    public List<TEntity> DeletedItems { get; } = new();

    public void SetupGetById(long id, TEntity? entity)
    {
        if (entity != null)
        {
            _entities[id] = entity;
            UpdateNextId(id);
        }
    }

    public void SetupList(IReadOnlyList<TEntity> entities)
    {
        _allEntities.Clear();
        _allEntities.AddRange(entities);
        // Update _nextId based on entities in the list
        foreach (var entity in entities)
        {
            var entityId = GetEntityId(entity);
            if (entityId > 0)
            {
                UpdateNextId(entityId);
            }
        }
    }
    
    private void UpdateNextId(long id)
    {
        if (id >= _nextId)
        {
            _nextId = id + 1;
        }
    }

    public void SetupListWithPredicate(Expression<Func<TEntity, bool>> predicate, IReadOnlyList<TEntity> entities)
    {
        _predicateHandler = _ => entities;
    }

    public Task<TEntity?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        _entities.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<TEntity?> GetByIdAsync(object[] keyValues, CancellationToken ct = default)
    {
        if (keyValues == null || keyValues.Length == 0)
            return Task.FromResult<TEntity?>(null);
        
        var id = keyValues[0] switch
        {
            long l => l,
            int i => (long)i,
            _ => 0L
        };
        
        _entities.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<TEntity>>(_allEntities);
    }

    public Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
    {
        if (_predicateHandler != null)
        {
            return Task.FromResult(_predicateHandler(predicate));
        }
        return Task.FromResult<IReadOnlyList<TEntity>>(_allEntities.Where(CompilePredicate(predicate)).ToList());
    }

    public Task<TEntity> AddAsync(TEntity entity, bool autoSave = true, CancellationToken ct = default)
    {
        AddedItems.Add(entity);
        
        // Get or assign a unique ID using reflection
        long entityId = GetEntityId(entity);
        if (entityId == 0)
        {
            entityId = _nextId++;
            SetEntityId(entity, entityId);
        }
        else
        {
            // Ensure _nextId is always greater than any existing ID
            UpdateNextId(entityId);
        }
        
        _entities[entityId] = entity;
        _allEntities.Add(entity);
        return Task.FromResult(entity);
    }
    
    private static long GetEntityId(TEntity entity)
    {
        if (IdProperty == null) return 0;
        var value = IdProperty.GetValue(entity);
        return value is long id ? id : 0;
    }
    
    private static void SetEntityId(TEntity entity, long id)
    {
        IdProperty?.SetValue(entity, id);
    }

    public Task UpdateAsync(TEntity entity, bool autoSave = true, CancellationToken ct = default)
    {
        UpdatedItems.Add(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TEntity entity, bool autoSave = true, CancellationToken ct = default)
    {
        DeletedItems.Add(entity);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken ct = default)
    {
        foreach (var entity in entities)
        {
            AddedItems.Add(entity);
        }
        return Task.CompletedTask;
    }

    public Task UpdateRangeAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken ct = default)
    {
        UpdatedItems.AddRange(entities);
        return Task.CompletedTask;
    }

    public IQueryable<TEntity> GetQueryable()
    {
        return _allEntities.AsQueryable();
    }

    public Task<(IReadOnlyList<TEntity> Items, int Total)> GetPagedAsync(
        IQueryable<TEntity> query,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var total = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult<(IReadOnlyList<TEntity>, int)>((items, total));
    }

    private static Func<TEntity, bool> CompilePredicate(Expression<Func<TEntity, bool>> predicate)
    {
        return predicate.Compile();
    }
}
