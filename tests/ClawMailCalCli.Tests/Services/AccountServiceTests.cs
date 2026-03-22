using ClawMailCalCli.Models;
using ClawMailCalCli.Services;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AccountService"/>.
/// </summary>
[Trait("Category", "Unit")]
public class AccountServiceTests
{
	private readonly Mock<IKeyVaultService> _mockKeyVaultService;

	public AccountServiceTests()
	{
		_mockKeyVaultService = new Mock<IKeyVaultService>();
	}

	private AccountService CreateAccountService() => new AccountService(_mockKeyVaultService.Object);

	[Fact]
	public async Task GetAccountAsync_WhenSecretDoesNotExist_ReturnsNull()
	{
		// Arrange
		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.GetSecretAsync("account-unknown", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var accountService = CreateAccountService();

		// Act
		var result = await accountService.GetAccountAsync("unknown");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetAccountAsync_WhenSecretExists_ReturnsDeserializedAccount()
	{
		// Arrange
		var json = """{"Name":"jwendl","Email":"jwendl@hotmail.com","Type":0}""";
		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.GetSecretAsync("account-jwendl", It.IsAny<CancellationToken>()))
			.ReturnsAsync(json);

		var accountService = CreateAccountService();

		// Act
		var result = await accountService.GetAccountAsync("jwendl");

		// Assert
		result.Should().NotBeNull();
		result!.Name.Should().Be("jwendl");
		result.Email.Should().Be("jwendl@hotmail.com");
		result.Type.Should().Be(AccountType.Personal);
	}

	[Fact]
	public async Task GetAccountAsync_UsesCorrectSecretName()
	{
		// Arrange
		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.GetSecretAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var accountService = CreateAccountService();

		// Act
		await accountService.GetAccountAsync("my-account");

		// Assert
		_mockKeyVaultService.Verify(
			keyVaultService => keyVaultService.GetSecretAsync("account-my-account", It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task SaveAccountAsync_SerializesAndStoresAccount()
	{
		// Arrange
		var account = new Account("jwendl", "jwendl@hotmail.com", AccountType.Personal);
		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		var accountService = CreateAccountService();

		// Act
		await accountService.SaveAccountAsync(account);

		// Assert — stored under the correct key
		_mockKeyVaultService.Verify(
			keyVaultService => keyVaultService.SetSecretAsync(
				"account-jwendl",
				It.Is<string>(value => value.Contains("jwendl") && value.Contains("hotmail.com")),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task SaveAccountAsync_WorkAccount_SerializesTypeCorrectly()
	{
		// Arrange
		var account = new Account("work", "user@contoso.com", AccountType.Work);
		string? capturedValue = null;
		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.Callback<string, string, CancellationToken>((_, value, _) => capturedValue = value)
			.Returns(Task.CompletedTask);

		var accountService = CreateAccountService();

		// Act
		await accountService.SaveAccountAsync(account);

		// Assert — the serialized JSON must include AccountType.Work (value 1)
		capturedValue.Should().NotBeNullOrWhiteSpace();
		capturedValue!.Should().Contain("contoso.com");
	}
}
