using Azure.Identity;
using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
using ClawMailCalCli.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AuthenticationService"/>.
/// </summary>
[Trait("Category", "Unit")]
public class AuthenticationServiceTests
{
	private readonly Mock<IAccountService> _mockAccountService;
	private readonly Mock<IKeyVaultService> _mockKeyVaultService;
	private readonly Mock<IDeviceCodeCredentialProvider> _mockDeviceCodeCredentialProvider;
	private readonly ILogger<AuthenticationService> _logger;

	public AuthenticationServiceTests()
	{
		_mockAccountService = new Mock<IAccountService>();
		_mockKeyVaultService = new Mock<IKeyVaultService>();
		_mockDeviceCodeCredentialProvider = new Mock<IDeviceCodeCredentialProvider>();
		_logger = new NullLogger<AuthenticationService>();

		// Default setups for Key Vault credential secrets — tests can override as needed.
		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.GetSecretAsync("hotmail-client-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync("test-personal-client-id");
		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.GetSecretAsync("hotmail-tenant-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync("common");
		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.GetSecretAsync("exchange-client-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync("test-work-client-id");
		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.GetSecretAsync("exchange-tenant-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync("test-work-tenant-id");
	}

	private AuthenticationService CreateAuthenticationService() =>
		new AuthenticationService(
			_mockAccountService.Object,
			_mockKeyVaultService.Object,
			_mockDeviceCodeCredentialProvider.Object,
			_logger);

	[Fact]
	public async Task AuthenticateAsync_WhenAccountNotFound_ReturnsFalse()
	{
		// Arrange
		_mockAccountService
			.Setup(accountService => accountService.GetAccountAsync("unknown", It.IsAny<CancellationToken>()))
			.ReturnsAsync((Account?)null);

		var authenticationService = CreateAuthenticationService();

		// Act
		var result = await authenticationService.AuthenticateAsync("unknown");

		// Assert — no credential provider calls should happen and false is returned
		result.Should().BeFalse();
		_mockDeviceCodeCredentialProvider.Verify(
			provider => provider.AuthenticateAsync(It.IsAny<DeviceCodeCredentialOptions>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task AuthenticateAsync_WhenClientIdMissing_ReturnsFalse()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetAccountAsync("work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.GetSecretAsync("exchange-client-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var authenticationService = CreateAuthenticationService();

		// Act
		var result = await authenticationService.AuthenticateAsync("work-account");

		// Assert — missing client ID should prevent authentication
		result.Should().BeFalse();
		_mockDeviceCodeCredentialProvider.Verify(
			provider => provider.AuthenticateAsync(It.IsAny<DeviceCodeCredentialOptions>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task AuthenticateAsync_WhenCachedRecordExists_ReturnsTrueAndSkipsDeviceCodeFlow()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetAccountAsync("work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		// Serialize a valid AuthenticationRecord and store it as a base64 secret,
		// exactly as the real service would after a successful first login.
		var validRecord = BuildFakeAuthenticationRecord();
		using var recordStream = new MemoryStream();
		await validRecord.SerializeAsync(recordStream);
		var base64Record = Convert.ToBase64String(recordStream.ToArray());

		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.GetSecretAsync("auth-record-work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(base64Record);

		var authenticationService = CreateAuthenticationService();

		// Act
		var result = await authenticationService.AuthenticateAsync("work-account");

		// Assert — the cached record was found so the device code flow should not be triggered
		result.Should().BeTrue();
		_mockDeviceCodeCredentialProvider.Verify(
			provider => provider.AuthenticateAsync(It.IsAny<DeviceCodeCredentialOptions>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task AuthenticateAsync_WhenNoCachedRecord_InvokesDeviceCodeFlow()
	{
		// Arrange
		var account = new Account("personal-account", "user@hotmail.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetAccountAsync("personal-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.GetSecretAsync("auth-record-personal-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var fakeRecord = BuildFakeAuthenticationRecord();
		_mockDeviceCodeCredentialProvider
			.Setup(provider => provider.AuthenticateAsync(It.IsAny<DeviceCodeCredentialOptions>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeRecord);

		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		var authenticationService = CreateAuthenticationService();

		// Act
		var result = await authenticationService.AuthenticateAsync("personal-account");

		// Assert
		result.Should().BeTrue();
		_mockDeviceCodeCredentialProvider.Verify(
			provider => provider.AuthenticateAsync(
				It.Is<DeviceCodeCredentialOptions>(options =>
					options.ClientId == "test-personal-client-id" &&
					options.TenantId == "common"),
				It.IsAny<string[]>(),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task AuthenticateAsync_WhenWorkAccount_UsesWorkTenantId()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetAccountAsync("work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.GetSecretAsync("auth-record-work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var fakeRecord = BuildFakeAuthenticationRecord();
		_mockDeviceCodeCredentialProvider
			.Setup(provider => provider.AuthenticateAsync(It.IsAny<DeviceCodeCredentialOptions>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeRecord);

		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		var authenticationService = CreateAuthenticationService();

		// Act
		var result = await authenticationService.AuthenticateAsync("work-account");

		// Assert — work accounts should use the organisation TenantId, not "common"
		result.Should().BeTrue();
		_mockDeviceCodeCredentialProvider.Verify(
			provider => provider.AuthenticateAsync(
				It.Is<DeviceCodeCredentialOptions>(options =>
					options.TenantId == "test-work-tenant-id"),
				It.IsAny<string[]>(),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task AuthenticateAsync_WhenPersonalAccount_UsesPersonalTenantId()
	{
		// Arrange
		var account = new Account("personal-account", "user@hotmail.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetAccountAsync("personal-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.GetSecretAsync("auth-record-personal-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var fakeRecord = BuildFakeAuthenticationRecord();
		_mockDeviceCodeCredentialProvider
			.Setup(provider => provider.AuthenticateAsync(It.IsAny<DeviceCodeCredentialOptions>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeRecord);

		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		var authenticationService = CreateAuthenticationService();

		// Act
		var result = await authenticationService.AuthenticateAsync("personal-account");

		// Assert
		result.Should().BeTrue();
		_mockDeviceCodeCredentialProvider.Verify(
			provider => provider.AuthenticateAsync(
				It.Is<DeviceCodeCredentialOptions>(options =>
					options.TenantId == "common"),
				It.IsAny<string[]>(),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task AuthenticateAsync_AfterSuccessfulAuth_StoresRecordInKeyVault()
	{
		// Arrange
		var account = new Account("personal-account", "user@hotmail.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetAccountAsync("personal-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.GetSecretAsync("auth-record-personal-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var fakeRecord = BuildFakeAuthenticationRecord();
		_mockDeviceCodeCredentialProvider
			.Setup(provider => provider.AuthenticateAsync(It.IsAny<DeviceCodeCredentialOptions>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeRecord);

		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		var authenticationService = CreateAuthenticationService();

		// Act
		var result = await authenticationService.AuthenticateAsync("personal-account");

		// Assert — the authentication record is persisted under the correct secret name
		result.Should().BeTrue();
		_mockKeyVaultService.Verify(
			keyVaultService => keyVaultService.SetSecretAsync(
				"auth-record-personal-account",
				It.IsAny<string>(),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task AuthenticateAsync_ConfiguresTokenCachePersistenceWithUnencryptedStorageAllowed()
	{
		// Arrange
		var account = new Account("personal-account", "user@hotmail.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetAccountAsync("personal-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.GetSecretAsync("auth-record-personal-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var fakeRecord = BuildFakeAuthenticationRecord();
		_mockDeviceCodeCredentialProvider
			.Setup(provider => provider.AuthenticateAsync(It.IsAny<DeviceCodeCredentialOptions>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeRecord);

		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		var authenticationService = CreateAuthenticationService();

		// Act
		await authenticationService.AuthenticateAsync("personal-account");

		// Assert — TokenCachePersistenceOptions must be set with UnsafeAllowUnencryptedStorage = true
		// to prevent crashes on Linux systems where libsecret is unavailable (e.g., Ubuntu 24.04).
		_mockDeviceCodeCredentialProvider.Verify(
			provider => provider.AuthenticateAsync(
				It.Is<DeviceCodeCredentialOptions>(options =>
					options.TokenCachePersistenceOptions != null &&
					options.TokenCachePersistenceOptions.UnsafeAllowUnencryptedStorage),
				It.IsAny<string[]>(),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public async Task AuthenticateAsync_WhenPersonalAccountTenantIdMissingOrWhitespace_UsesConsumersDefault(string? tenantId)
	{
		// Arrange
		var account = new Account("personal-account", "user@hotmail.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetAccountAsync("personal-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.GetSecretAsync("hotmail-tenant-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync(tenantId);

		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.GetSecretAsync("auth-record-personal-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var fakeRecord = BuildFakeAuthenticationRecord();
		_mockDeviceCodeCredentialProvider
			.Setup(provider => provider.AuthenticateAsync(It.IsAny<DeviceCodeCredentialOptions>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeRecord);

		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		var authenticationService = CreateAuthenticationService();

		// Act
		var result = await authenticationService.AuthenticateAsync("personal-account");

		// Assert — personal accounts fall back to 'consumers' when tenant ID is not set in Key Vault
		result.Should().BeTrue();
		_mockDeviceCodeCredentialProvider.Verify(
			provider => provider.AuthenticateAsync(
				It.Is<DeviceCodeCredentialOptions>(options => options.TenantId == "consumers"),
				It.IsAny<string[]>(),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public async Task AuthenticateAsync_WhenWorkAccountTenantIdMissingOrWhitespace_UsesOrganizationsDefault(string? tenantId)
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetAccountAsync("work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.GetSecretAsync("exchange-tenant-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync(tenantId);

		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.GetSecretAsync("auth-record-work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var fakeRecord = BuildFakeAuthenticationRecord();
		_mockDeviceCodeCredentialProvider
			.Setup(provider => provider.AuthenticateAsync(It.IsAny<DeviceCodeCredentialOptions>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeRecord);

		_mockKeyVaultService
			.Setup(keyVaultService => keyVaultService.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		var authenticationService = CreateAuthenticationService();

		// Act
		var result = await authenticationService.AuthenticateAsync("work-account");

		// Assert — work accounts fall back to 'organizations' when tenant ID is not set in Key Vault
		result.Should().BeTrue();
		_mockDeviceCodeCredentialProvider.Verify(
			provider => provider.AuthenticateAsync(
				It.Is<DeviceCodeCredentialOptions>(options => options.TenantId == "organizations"),
				It.IsAny<string[]>(),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	/// <summary>
	/// Creates a minimal valid <see cref="AuthenticationRecord"/> that can be serialized and
	/// used as a return value from a mocked <see cref="IDeviceCodeCredentialProvider"/>.
	/// </summary>
	private static AuthenticationRecord BuildFakeAuthenticationRecord()
	{
		// Serialize a valid-looking AuthenticationRecord JSON and deserialize it back.
		// This avoids constructor reflection hacks while keeping the test self-contained.
		var json = """
			{
				"username": "user@example.com",
				"authority": "https://login.microsoftonline.com/common",
				"homeAccountId": "00000000-0000-0000-0000-000000000001.00000000-0000-0000-0000-000000000002",
				"tenantId": "common",
				"clientId": "test-client-id",
				"version": "1.0"
			}
			""";
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
		return AuthenticationRecord.DeserializeAsync(stream).GetAwaiter().GetResult();
	}
}
