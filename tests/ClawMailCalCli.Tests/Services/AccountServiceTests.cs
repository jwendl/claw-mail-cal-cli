using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
using Microsoft.Extensions.Logging;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AccountService"/>.
/// </summary>
[Trait("Category", "Unit")]
public class AccountServiceTests
{
	private readonly Mock<ISecretStore> _mockSecretStore;
	private readonly AccountService _accountService;

	/// <summary>
	/// Initializes a new instance of <see cref="AccountServiceTests"/>.
	/// </summary>
	public AccountServiceTests()
	{
		_mockSecretStore = new Mock<ISecretStore>();
		_accountService = new AccountService(_mockSecretStore.Object, Mock.Of<ILogger<AccountService>>());
	}

	[Fact]
	public async Task AddAccountAsync_WithNewAccount_ReturnsTrue()
	{
		// Arrange
		_mockSecretStore
			.Setup(s => s.GetSecretValueAsync("account-names", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		// Act
		var result = await _accountService.AddAccountAsync("myaccount", "user@example.com");

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public async Task AddAccountAsync_WithDuplicateAccountName_ReturnsFalse()
	{
		// Arrange
		_mockSecretStore
			.Setup(s => s.GetSecretValueAsync("account-names", It.IsAny<CancellationToken>()))
			.ReturnsAsync("myaccount");

		// Act
		var result = await _accountService.AddAccountAsync("myaccount", "user@example.com");

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task AddAccountAsync_WithNewAccount_StoresEmailSecret()
	{
		// Arrange
		_mockSecretStore
			.Setup(s => s.GetSecretValueAsync("account-names", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		// Act
		await _accountService.AddAccountAsync("newaccount", "new@example.com");

		// Assert
		_mockSecretStore.Verify(
			s => s.SetSecretValueAsync("account-newaccount-email", "new@example.com", It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task AddAccountAsync_WithExistingAccounts_UpdatesAccountNamesList()
	{
		// Arrange
		_mockSecretStore
			.Setup(s => s.GetSecretValueAsync("account-names", It.IsAny<CancellationToken>()))
			.ReturnsAsync("existing");

		// Act
		await _accountService.AddAccountAsync("newaccount", "new@example.com");

		// Assert
		_mockSecretStore.Verify(
			s => s.SetSecretValueAsync("account-names", It.Is<string>(v => v.Contains("existing") && v.Contains("newaccount")), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ListAccountsAsync_WithNoAccountsSecret_ReturnsEmptyList()
	{
		// Arrange
		_mockSecretStore
			.Setup(s => s.GetSecretValueAsync("account-names", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		// Act
		var accounts = await _accountService.ListAccountsAsync();

		// Assert
		accounts.Should().BeEmpty();
	}

	[Fact]
	public async Task ListAccountsAsync_WithStoredAccounts_ReturnsAllAccounts()
	{
		// Arrange
		_mockSecretStore
			.Setup(s => s.GetSecretValueAsync("account-names", It.IsAny<CancellationToken>()))
			.ReturnsAsync("alice,bob");
		_mockSecretStore
			.Setup(s => s.GetSecretValueAsync("account-alice-email", It.IsAny<CancellationToken>()))
			.ReturnsAsync("alice@example.com");
		_mockSecretStore
			.Setup(s => s.GetSecretValueAsync("account-bob-email", It.IsAny<CancellationToken>()))
			.ReturnsAsync("bob@example.com");

		// Act
		var accounts = await _accountService.ListAccountsAsync();

		// Assert
		accounts.Should().HaveCount(2);
		accounts.Should().Contain(new Account("alice", "alice@example.com"));
		accounts.Should().Contain(new Account("bob", "bob@example.com"));
	}

	[Fact]
	public async Task ListAccountsAsync_WithEmptyAccountNames_ReturnsEmptyList()
	{
		// Arrange
		_mockSecretStore
			.Setup(s => s.GetSecretValueAsync("account-names", It.IsAny<CancellationToken>()))
			.ReturnsAsync(string.Empty);

		// Act
		var accounts = await _accountService.ListAccountsAsync();

		// Assert
		accounts.Should().BeEmpty();
	}

	[Fact]
	public async Task DeleteAccountAsync_WithExistingAccount_ReturnsTrue()
	{
		// Arrange
		_mockSecretStore
			.Setup(s => s.GetSecretValueAsync("account-names", It.IsAny<CancellationToken>()))
			.ReturnsAsync("myaccount");

		// Act
		var result = await _accountService.DeleteAccountAsync("myaccount");

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public async Task DeleteAccountAsync_WithNonExistentAccount_ReturnsFalse()
	{
		// Arrange
		_mockSecretStore
			.Setup(s => s.GetSecretValueAsync("account-names", It.IsAny<CancellationToken>()))
			.ReturnsAsync("otheraccount");

		// Act
		var result = await _accountService.DeleteAccountAsync("nonexistent");

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task DeleteAccountAsync_WithExistingAccount_DeletesEmailSecret()
	{
		// Arrange
		_mockSecretStore
			.Setup(s => s.GetSecretValueAsync("account-names", It.IsAny<CancellationToken>()))
			.ReturnsAsync("alice,bob");

		// Act
		await _accountService.DeleteAccountAsync("alice");

		// Assert
		_mockSecretStore.Verify(
			s => s.DeleteSecretAsync("account-alice-email", It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task DeleteAccountAsync_WithExistingAccount_RemovesFromNamesList()
	{
		// Arrange
		_mockSecretStore
			.Setup(s => s.GetSecretValueAsync("account-names", It.IsAny<CancellationToken>()))
			.ReturnsAsync("alice,bob");

		// Act
		await _accountService.DeleteAccountAsync("alice");

		// Assert
		_mockSecretStore.Verify(
			s => s.SetSecretValueAsync("account-names", It.Is<string>(v => !v.Contains("alice") && v.Contains("bob")), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task SetDefaultAccountAsync_WithExistingAccount_ReturnsTrue()
	{
		// Arrange
		_mockSecretStore
			.Setup(s => s.GetSecretValueAsync("account-names", It.IsAny<CancellationToken>()))
			.ReturnsAsync("myaccount");

		// Act
		var result = await _accountService.SetDefaultAccountAsync("myaccount");

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public async Task SetDefaultAccountAsync_WithNonExistentAccount_ReturnsFalse()
	{
		// Arrange
		_mockSecretStore
			.Setup(s => s.GetSecretValueAsync("account-names", It.IsAny<CancellationToken>()))
			.ReturnsAsync("otheraccount");

		// Act
		var result = await _accountService.SetDefaultAccountAsync("nonexistent");

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task SetDefaultAccountAsync_WithExistingAccount_StoresDefaultAccountSecret()
	{
		// Arrange
		_mockSecretStore
			.Setup(s => s.GetSecretValueAsync("account-names", It.IsAny<CancellationToken>()))
			.ReturnsAsync("myaccount");

		// Act
		await _accountService.SetDefaultAccountAsync("myaccount");

		// Assert
		_mockSecretStore.Verify(
			s => s.SetSecretValueAsync("default-account", "myaccount", It.IsAny<CancellationToken>()),
			Times.Once);
	}
}

