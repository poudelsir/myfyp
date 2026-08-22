using Microsoft.EntityFrameworkCore;
using SajhaSikshya.Data;

namespace SajhaSikshya.Tests;

/// <summary>
/// One fresh, isolated in-memory database per test (unique name = no cross-test bleed,
/// no shared-state flakiness) — real EF Core query/save behavior, not a hand-rolled fake
/// repository, so these tests exercise the actual LINQ the services rely on.
/// </summary>
public static class TestDbContextFactory
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
