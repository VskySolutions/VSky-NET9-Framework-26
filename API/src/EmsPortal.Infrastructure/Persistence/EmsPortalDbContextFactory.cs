using System.Text.Json;
using EmsPortal.Infrastructure.Security;
using EmsPortal.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EmsPortal.Infrastructure.Persistence;

/// <summary>
/// Builds the DbContext for the EF tooling (<c>dotnet ef migrations add</c>, <c>dotnet ef database update</c>)
/// without starting the API host, so a migration can be added before the database exists. The connection
/// string comes from the <c>ConnectionStrings__SqlServer</c> environment variable, else from the API's
/// <c>appsettings.json</c> found by walking up from the working directory; adding a migration needs only
/// the provider, so a placeholder stands in when neither is present.
/// </summary>
internal sealed class EmsPortalDbContextFactory : IDesignTimeDbContextFactory<EmsPortalDbContext>
{
    private const string Placeholder =
        "Server=(localdb)\\MSSQLLocalDB;Database=VSkyBaseFramework;Trusted_Connection=True;TrustServerCertificate=True;";

    public EmsPortalDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<EmsPortalDbContext>()
            .UseSqlServer(
                ResolveConnectionString(),
                sql => sql.MigrationsAssembly(typeof(EmsPortalDbContext).Assembly.FullName))
            .Options;

        return new EmsPortalDbContext(options, new TenantContext(), new SystemActorAccessor());
    }

    private static string ResolveConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                var candidates = new[]
                {
                    Path.Combine(dir.FullName, "appsettings.json"),
                    Path.Combine(dir.FullName, "EmsPortal.Api", "appsettings.json"),
                    Path.Combine(dir.FullName, "src", "EmsPortal.Api", "appsettings.json"),
                };

                foreach (var candidate in candidates)
                {
                    if (ReadSqlServerConnection(candidate) is { } connection)
                    {
                        return connection;
                    }
                }
            }
        }

        return Placeholder;
    }

    private static string? ReadSqlServerConnection(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using var document = JsonDocument.Parse(
            File.ReadAllText(path),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

        return document.RootElement.TryGetProperty("ConnectionStrings", out var connections)
            && connections.TryGetProperty("SqlServer", out var value)
            && value.GetString() is { Length: > 0 } connection
            ? connection
            : null;
    }
}
