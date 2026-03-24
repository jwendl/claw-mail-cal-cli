using Microsoft.EntityFrameworkCore;

namespace ClawMailCalCli.Data;

/// <summary>
/// Entity Framework Core database context for claw-mail-cal-cli.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
	: DbContext(options)
{
	/// <summary>
	/// The accounts stored in the local database.
	/// </summary>
	public DbSet<AccountEntity> Accounts { get; set; } = null!;

	/// <inheritdoc />
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<AccountEntity>(entity =>
		{
			entity.HasIndex(e => e.Name).IsUnique();
		});
	}
}
