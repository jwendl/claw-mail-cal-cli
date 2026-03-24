namespace ClawMailCalCli.IntegrationTests.TestHelpers;

/// <summary>
/// A simple <see cref="IDbContextFactory{TContext}"/> implementation for integration testing.
/// Creates <see cref="ApplicationDbContext"/> instances using the provided pre-configured options.
/// </summary>
internal sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
	: IDbContextFactory<ApplicationDbContext>
{
	/// <inheritdoc />
	public ApplicationDbContext CreateDbContext() => new(options);
}
