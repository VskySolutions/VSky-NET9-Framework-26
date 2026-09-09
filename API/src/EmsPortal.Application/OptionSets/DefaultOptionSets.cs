using EmsPortal.Domain.Enums;

namespace EmsPortal.Application.OptionSets;

/// <summary>
/// Platform-standard option lists seeded on startup (TenantId = null, IsSystem = true), visible to every
/// tenant. They are the STARTING values, not fixed ones — their items can be managed in the app, and a
/// tenant created via <see cref="TenantOptionSetSeeder"/> gets its own copy. Because the seeded rows are
/// shared, editing one there changes it for every tenant that has no copy of its own.
/// Mirrors the <c>DefaultEmailTemplates</c> definition pattern.
/// </summary>
public static class DefaultOptionSets
{
    /// <summary>
    /// <paramref name="Description"/> is what the value MEANS, surfaced as its tooltip wherever the value
    /// is offered or displayed. Worth filling in wherever the labels alone could be mistaken for one
    /// another; left null where the label speaks for itself.
    /// </summary>
    public sealed record ItemDefinition(
        string Value,
        string Label,
        int SortOrder,
        string? MetadataJson = null,
        string? Description = null,
        /// <summary>Badge background, as hex. Seeded so a status list arrives already coloured.</summary>
        string? BackgroundColor = null,
        /// <summary>Badge text colour, as hex.</summary>
        string? TextColor = null,
        /// <summary>Material icon name shown beside the value, e.g. <c>o_support_agent</c>.</summary>
        string? Icon = null,
        /// <summary>
        /// Whether the value is OFFERED. False seeds it hidden: the code stays on the list, records
        /// already recorded against it keep reading correctly, and nothing new can be filed under it —
        /// which is the difference between retiring a value and deleting one. A tenant can put it back
        /// in Administration → Option Sets, so this is a starting position like every other field here.
        /// </summary>
        bool IsActive = true);

    /// <summary>
    /// Two different kinds of protection, because two different things can be true of a list.
    /// <para>
    /// <paramref name="IsClosed"/> — the application branches on the values AND the set of them is fixed.
    /// Nothing may be added, and no seeded value may be deleted, re-coded or hidden. A list that mirrors a
    /// C# enum is this kind: a status the server never writes is a status nothing can reach.
    /// </para>
    /// <para>
    /// <paramref name="LockSeededValues"/> — the application branches on the values it SEEDED, but the
    /// list itself is open. A tenant may add a value of its own and hide one it does not use; what it may
    /// not do is delete or re-code a seeded value that feature code is written against.
    /// </para>
    /// Everything a tenant would actually want to change — the label, the description, the colours, the
    /// icon, the order — stays theirs on both.
    /// </summary>
    public sealed record Definition(
        EntityType EntityType,
        string Key,
        string Name,
        OptionItemSortMode ItemSortMode,
        IReadOnlyList<ItemDefinition> Items,
        bool IsClosed = false,
        bool LockSeededValues = false)
    {
        /// <summary>Whether the values seeded into this list are the application's own.</summary>
        public bool SeedsSystemValues => IsClosed || LockSeededValues;
    }

    /// <summary>The option-set key of the department list a user is placed in (see <c>UserDepartment</c>).</summary>
    public const string UserDepartmentKey = "User.Department";

    /// <summary>
    /// The platform-standard option lists to seed. The chosen item's <c>Value</c> is what gets stored on
    /// the row that references a CODE-valued list; a list referenced by item ID is a foreign key instead.
    /// A module adds its own lists here, keyed to its <see cref="EntityType"/>.
    /// </summary>
    public static IReadOnlyList<Definition> All { get; } = new[]
    {
        // The departments a user can be placed in. Nothing branches on these codes, so a tenant may
        // rename, reorder, add or remove them freely; they are only a starting list.
        new Definition(EntityType.User, UserDepartmentKey, "Department", OptionItemSortMode.Custom, new[]
        {
            new ItemDefinition("operations", "Operations", 1),
            new ItemDefinition("finance", "Finance", 2),
            new ItemDefinition("sales", "Sales", 3),
            new ItemDefinition("support", "Support", 4),
            new ItemDefinition("it", "IT", 5),
            new ItemDefinition("hr", "Human Resources", 6),
        }),
    };
}
