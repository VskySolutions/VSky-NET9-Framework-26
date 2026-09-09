using EmsPortal.Application.Abstractions.UniversalFeatures;
using EmsPortal.Domain.Enums;

namespace EmsPortal.Infrastructure.Persistence;

/// <summary>
/// Restamps an aggregate root's audit fields by marking it modified on the current unit of work; the
/// DbContext's existing audit stamping then writes Updated By / Updated On as part of the same commit.
/// <para>
/// Only entity types whose LIST shows a parent whose children are edited elsewhere need this. For every
/// type in the base framework the list IS the root — a role, a tag, a tenant — and editing it already
/// restamps it, so there is nothing to do here yet. A module whose list shows a parent worked on through
/// child records adds a case: load the root through the DbContext (the tenant filter stays on), and when
/// its entry is Unchanged mark a single scalar such as <c>UpdatedOnUtc</c> modified so StampAudit sees it.
/// </para>
/// </summary>
internal sealed class AggregateRootTouch : IAggregateRootTouch
{
    private readonly EmsPortalDbContext _dbContext;

    public AggregateRootTouch(EmsPortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task TouchAsync(EntityType entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        _ = _dbContext;
        return Task.CompletedTask;
    }
}
