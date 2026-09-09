using System.Security.Claims;
using EmsPortal.Domain.Enums;
using EmsPortal.Shared.Security;

namespace EmsPortal.Api.Security;

/// <summary>
/// Maps a Universal Feature target <see cref="EntityType"/> to the base read permission of its parent
/// entity, and checks it on the current principal.
/// </summary>
public static class UniversalFeatureEntityAccess
{
    /// <summary>The base read permission(s) gating UF access to a given entity type.</summary>
    public static IReadOnlyList<string> RequiredReadPermissions(EntityType entityType) => entityType switch
    {
        EntityType.Tenant => new[] { Permissions.TenantsRead },
        EntityType.User => new[] { Permissions.UsersRead },
        EntityType.UserGroup => new[] { Permissions.UsersRead },
        // A module's records gate their conversation/activity/attachments on that module's read
        // permission; add its case here.
        _ => new[] { Permissions.UsersRead },
    };

    /// <summary>True when the caller may read the given entity type (and therefore its UF data).</summary>
    public static bool CanAccess(this ClaimsPrincipal principal, EntityType entityType)
        => principal.IsSuperAdmin() || RequiredReadPermissions(entityType).Any(principal.HasPermission);
}
