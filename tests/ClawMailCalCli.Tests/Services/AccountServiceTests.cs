using ClawMailCalCli.Data;
using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AccountService"/>.
/// </summary>
[Trait("Category", "Unit")]
public class AccountServiceTests : IAsyncLifetime
{
	private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
	private readonly AccountService _accountService;

	/// <summary>
	/// Initializes a new instance of <see cref="AccountServiceTests"/> with an in-memory SQLite database.
	/// </summary>
	public AccountServiceTests()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		_dbContextFactory = new TestDbContextFactory(options);
		_accountService = new AccountService(_dbContextFactory, Mock.Of<ILogger<AccountService>>());
	}

	/// <inheritdoc />
	public async Task InitializeAsync()
	{
		await using var context = await _dbContextFactory.CreateDbContextAsync();
		await context.Database.EnsureCreatedAsync();
	}

	/// <inheritdoc />
	public async Task DisposeAsync()
	{
		await using var context = await _dbContextFactory.CreateDbContextAsync();
		await context.Database.EnsureDeletedAsync();
	}

	[Fact]
	public async Task AddAccountAsync_WithNameContainingComma_ReturnsFalse()
	{
		// Arrange (no DB interaction expected — validation fails before any IO)

		// Act
		var result = await _accountService.AddAccountAsync("bad,name", "user@example.com");

		// Assert
		result.Should().BeFalse();

		using var context = _dbContextFactory.CreateDbContext();
		var accountCount = await context.Accounts.CountAsync();
		accountCount.Should().Be(0);
	}

	[Fact]
	public async Task AddAccountAsync_WithEmptyName_ReturnsFalse()
	{
		// Arrange (no DB interaction expected — validation fails before any IO)

		// Act
		var result = await _accountService.AddAccountAsync("   ", "user@example.com");

		// Assert
		result.Should().BeFalse();

		using var context = _dbContextFactory.CreateDbContext();
		var accountCount = await context.Accounts.CountAsync();
		accountCount.Should().Be(0);
	}

	[Fact]
	public async Task AddAccountAsync_WithMixedCaseName_NormalizesToLowercase()
	{
		// Arrange (empty database)

		// Act
		await _accountService.AddAccountAsync("MyAccount", "user@example.com");

		// Assert — stored with normalized (lowercase) name
		using var context = _dbContextFactory.CreateDbContext();
		var storedAccount = await context.Accounts.SingleOrDefaultAsync(a => a.Name == "myaccount");
		storedAccount.Should().NotBeNull();
		storedAccount!.Email.Should().Be("user@example.com");
	}

	[Fact]
	public async Task AddAccountAsync_WithNameWithLeadingTrailingWhitespace_NormalizesName()
	{
		// Arrange (empty database)

		// Act
		await _accountService.AddAccountAsync("  myaccount  ", "user@example.com");

		// Assert — stored with trimmed name
		using var context = _dbContextFactory.CreateDbContext();
		var storedAccount = await context.Accounts.SingleOrDefaultAsync(a => a.Name == "myaccount");
		storedAccount.Should().NotBeNull();
	}

	[Fact]
	public async Task DeleteAccountAsync_WithNameContainingComma_ReturnsFalse()
	{
		// Arrange (no DB interaction expected — validation fails before any IO)

		// Act
		var result = await _accountService.DeleteAccountAsync("bad,name");

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task SetDefaultAccountAsync_WithNameContainingComma_ReturnsFalse()
	{
		// Arrange (no DB interaction expected — validation fails before any IO)

		// Act
		var result = await _accountService.SetDefaultAccountAsync("bad,name");

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task AddAccountAsync_WithNewAccount_ReturnsTrue()
	{
		// Arrange (empty database)

		// Act
		var result = await _accountService.AddAccountAsync("myaccount", "user@example.com");

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public async Task AddAccountAsync_WithDuplicateAccountName_ReturnsFalse()
	{
		// Arrange
		using (var context = _dbContextFactory.CreateDbContext())
		{
			context.Accounts.Add(new AccountEntity { Name = "myaccount", Email = "user@example.com" });
			await context.SaveChangesAsync();
		}

		// Act
		var result = await _accountService.AddAccountAsync("myaccount", "other@example.com");

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task AddAccountAsync_WithNewAccount_StoresEmailInDatabase()
	{
		// Arrange (empty database)

		// Act
		await _accountService.AddAccountAsync("newaccount", "new@example.com");

		// Assert
		using var context = _dbContextFactory.CreateDbContext();
		var storedAccount = await context.Accounts.SingleOrDefaultAsync(a => a.Name == "newaccount");
		storedAccount.Should().NotBeNull();
		storedAccount!.Email.Should().Be("new@example.com");
	}

	[Fact]
	public async Task AddAccountAsync_WithExistingAccounts_AddsNewAccountAlongside()
	{
		// Arrange
		using (var context = _dbContextFactory.CreateDbContext())
		{
			context.Accounts.Add(new AccountEntity { Name = "existing", Email = "existing@example.com" });
			await context.SaveChangesAsync();
		}

		// Act
		await _accountService.AddAccountAsync("newaccount", "new@example.com");

		// Assert
		using var verifyContext = _dbContextFactory.CreateDbContext();
		var allAccounts = await verifyContext.Accounts.ToListAsync();
		allAccounts.Should().HaveCount(2);
		allAccounts.Should().Contain(a => a.Name == "existing");
		allAccounts.Should().Contain(a => a.Name == "newaccount");
	}

	[Fact]
	public async Task ListAccountsAsync_WithNoAccounts_ReturnsEmptyList()
	{
		// Arrange (empty database)

		// Act
		var accounts = await _accountService.ListAccountsAsync();

		// Assert
		accounts.Should().BeEmpty();
	}

	[Fact]
	public async Task ListAccountsAsync_WithStoredAccounts_ReturnsAllAccounts()
	{
		// Arrange
		using (var context = _dbContextFactory.CreateDbContext())
		{
			context.Accounts.Add(new AccountEntity { Name = "alice", Email = "alice@example.com" });
			context.Accounts.Add(new AccountEntity { Name = "bob", Email = "bob@example.com" });
			await context.SaveChangesAsync();
		}

		// Act
		var accounts = await _accountService.ListAccountsAsync();

		// Assert
		accounts.Should().HaveCount(2);
		accounts.Should().Contain(new Account("alice", "alice@example.com"));
		accounts.Should().Contain(new Account("bob", "bob@example.com"));
	}

	[Fact]
	public async Task ListAccountsAsync_WithSingleAccount_ReturnsSingleAccount()
	{
		// Arrange
		using (var context = _dbContextFactory.CreateDbContext())
		{
			context.Accounts.Add(new AccountEntity { Name = "alice", Email = "alice@example.com" });
			await context.SaveChangesAsync();
		}

		// Act
		var accounts = await _accountService.ListAccountsAsync();

		// Assert
		accounts.Should().HaveCount(1);
		accounts[0].Name.Should().Be("alice");
	}

	[Fact]
	public async Task DeleteAccountAsync_WithExistingAccount_ReturnsTrue()
	{
		// Arrange
		using (var context = _dbContextFactory.CreateDbContext())
		{
			context.Accounts.Add(new AccountEntity { Name = "myaccount", Email = "user@example.com" });
			await context.SaveChangesAsync();
		}

		// Act
		var result = await _accountService.DeleteAccountAsync("myaccount");

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public async Task DeleteAccountAsync_WithNonExistentAccount_ReturnsFalse()
	{
		// Arrange
		using (var context = _dbContextFactory.CreateDbContext())
		{
			context.Accounts.Add(new AccountEntity { Name = "otheraccount", Email = "other@example.com" });
			await context.SaveChangesAsync();
		}

		// Act
		var result = await _accountService.DeleteAccountAsync("nonexistent");

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task DeleteAccountAsync_WithExistingAccount_RemovesFromDatabase()
	{
		// Arrange
		using (var context = _dbContextFactory.CreateDbContext())
		{
			context.Accounts.Add(new AccountEntity { Name = "alice", Email = "alice@example.com" });
			context.Accounts.Add(new AccountEntity { Name = "bob", Email = "bob@example.com" });
			await context.SaveChangesAsync();
		}

		// Act
		await _accountService.DeleteAccountAsync("alice");

		// Assert
		using var verifyContext = _dbContextFactory.CreateDbContext();
		var allAccounts = await verifyContext.Accounts.ToListAsync();
		allAccounts.Should().HaveCount(1);
		allAccounts.Should().NotContain(a => a.Name == "alice");
		allAccounts.Should().Contain(a => a.Name == "bob");
	}

	[Fact]
	public async Task DeleteAccountAsync_WithExistingAccount_LeavesOtherAccountsIntact()
	{
		// Arrange
		using (var context = _dbContextFactory.CreateDbContext())
		{
			context.Accounts.Add(new AccountEntity { Name = "alice", Email = "alice@example.com" });
			context.Accounts.Add(new AccountEntity { Name = "bob", Email = "bob@example.com" });
			await context.SaveChangesAsync();
		}

		// Act
		await _accountService.DeleteAccountAsync("alice");

		// Assert
		using var verifyContext = _dbContextFactory.CreateDbContext();
		var remainingAccount = await verifyContext.Accounts.SingleOrDefaultAsync(a => a.Name == "bob");
		remainingAccount.Should().NotBeNull();
	}

	[Fact]
	public async Task SetDefaultAccountAsync_WithExistingAccount_ReturnsTrue()
	{
		// Arrange
		using (var context = _dbContextFactory.CreateDbContext())
		{
			context.Accounts.Add(new AccountEntity { Name = "myaccount", Email = "user@example.com" });
			await context.SaveChangesAsync();
		}

		// Act
		var result = await _accountService.SetDefaultAccountAsync("myaccount");

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public async Task SetDefaultAccountAsync_WithNonExistentAccount_ReturnsFalse()
	{
		// Arrange
		using (var context = _dbContextFactory.CreateDbContext())
		{
			context.Accounts.Add(new AccountEntity { Name = "otheraccount", Email = "other@example.com" });
			await context.SaveChangesAsync();
		}

		// Act
		var result = await _accountService.SetDefaultAccountAsync("nonexistent");

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task SetDefaultAccountAsync_WithExistingAccount_SetsIsDefaultTrue()
	{
		// Arrange
		using (var context = _dbContextFactory.CreateDbContext())
		{
			context.Accounts.Add(new AccountEntity { Name = "myaccount", Email = "user@example.com" });
			await context.SaveChangesAsync();
		}

		// Act
		await _accountService.SetDefaultAccountAsync("myaccount");

		// Assert
		using var verifyContext = _dbContextFactory.CreateDbContext();
		var account = await verifyContext.Accounts.SingleOrDefaultAsync(a => a.Name == "myaccount");
		account.Should().NotBeNull();
		account!.IsDefault.Should().BeTrue();
	}

	[Fact]
	public async Task SetDefaultAccountAsync_WhenAnotherAccountIsDefault_ClearsOldDefault()
	{
		// Arrange
		using (var context = _dbContextFactory.CreateDbContext())
		{
			context.Accounts.Add(new AccountEntity { Name = "alice", Email = "alice@example.com", IsDefault = true });
			context.Accounts.Add(new AccountEntity { Name = "bob", Email = "bob@example.com" });
			await context.SaveChangesAsync();
		}

		// Act
		await _accountService.SetDefaultAccountAsync("bob");

		// Assert — alice should no longer be default, bob should be
		using var verifyContext = _dbContextFactory.CreateDbContext();
		var alice = await verifyContext.Accounts.SingleOrDefaultAsync(a => a.Name == "alice");
		var bob = await verifyContext.Accounts.SingleOrDefaultAsync(a => a.Name == "bob");
		alice!.IsDefault.Should().BeFalse();
		bob!.IsDefault.Should().BeTrue();
	}

	/// <summary>
	/// A simple <see cref="IDbContextFactory{TContext}"/> implementation for testing.
	/// </summary>
	private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
		: IDbContextFactory<ApplicationDbContext>
	{
		/// <inheritdoc />
		public ApplicationDbContext CreateDbContext() => new(options);
	}
}


