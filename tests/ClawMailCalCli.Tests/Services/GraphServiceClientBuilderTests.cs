using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
using ClawMailCalCli.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="GraphServiceClientBuilder"/>.
/// </summary>
[Trait("Category", "Unit")]
public class GraphServiceClientBuilderTests
{
	private readonly Mock<IKeyVaultService> _mockKeyVaultService;
	private readonly ILogger<GraphServiceClientBuilder> _logger;

	public GraphServiceClientBuilderTests()
	{
		_mockKeyVaultService = new Mock<IKeyVaultService>();
		_logger = new NullLogger<GraphServiceClientBuilder>();
	}

	private GraphServiceClientBuilder CreateGraphServiceClientBuilder() =>
		new GraphServiceClientBuilder(_mockKeyVaultService.Object, _logger);

	[Fact]
	public async Task BuildAsync_WhenAuthRecordSecretIsNull_ReturnsNull()
	{
		// Arrange
		var account = new Account("test-account", "test@example.com", AccountType.Work);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-test-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var graphServiceClientBuilder = CreateGraphServiceClientBuilder();

		// Act
		var result = await graphServiceClientBuilder.BuildAsync(account);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task BuildAsync_WhenAuthRecordSecretIsEmpty_ReturnsNull()
	{
		// Arrange
		var account = new Account("test-account", "test@example.com", AccountType.Work);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-test-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(string.Empty);

		var graphServiceClientBuilder = CreateGraphServiceClientBuilder();

		// Act
		var result = await graphServiceClientBuilder.BuildAsync(account);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task BuildAsync_WhenAuthRecordIsInvalidBase64_ReturnsNull()
	{
		// Arrange
		var account = new Account("test-account", "test@example.com", AccountType.Work);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-test-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync("this-is-not-valid-base64!!!");

		var graphServiceClientBuilder = CreateGraphServiceClientBuilder();

		// Act
		var result = await graphServiceClientBuilder.BuildAsync(account);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task BuildAsync_WhenAuthRecordIsValidBase64ButNotAuthRecord_ReturnsNull()
	{
		// Arrange
		var account = new Account("test-account", "test@example.com", AccountType.Work);
		var invalidRecordBase64 = Convert.ToBase64String("not-a-valid-auth-record"u8.ToArray());

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-test-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(invalidRecordBase64);

		var graphServiceClientBuilder = CreateGraphServiceClientBuilder();

		// Act
		var result = await graphServiceClientBuilder.BuildAsync(account);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task BuildAsync_WhenAuthRecordDeserializationFails_DoesNotFetchClientIdSecret()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		var validBase64NotAuthRecord = Convert.ToBase64String("not-a-valid-auth-record-json"u8.ToArray());

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(validBase64NotAuthRecord);

		var graphServiceClientBuilder = CreateGraphServiceClientBuilder();

		// Act
		var result = await graphServiceClientBuilder.BuildAsync(account);

		// Assert — deserialization of the auth record fails, so the method returns null before ever fetching client-id
		result.Should().BeNull();
		_mockKeyVaultService.Verify(
			service => service.GetSecretAsync("exchange-client-id", It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Theory]
	[InlineData(AccountType.Personal, "hotmail")]
	[InlineData(AccountType.Work, "exchange")]
	public async Task BuildAsync_WhenAuthRecordSecretIsNull_DoesNotFetchClientIdSecret(AccountType accountType, string expectedPrefix)
	{
		// Arrange
		var account = new Account("test-account", "test@example.com", accountType);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-test-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var graphServiceClientBuilder = CreateGraphServiceClientBuilder();

		// Act
		await graphServiceClientBuilder.BuildAsync(account);

		// Assert — client-id secret should not be fetched when auth record is missing
		_mockKeyVaultService.Verify(
			service => service.GetSecretAsync($"{expectedPrefix}-client-id", It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Theory]
	[InlineData(AccountType.Personal, "hotmail", null)]
	[InlineData(AccountType.Personal, "hotmail", "")]
	[InlineData(AccountType.Personal, "hotmail", "   ")]
	[InlineData(AccountType.Work, "exchange", null)]
	[InlineData(AccountType.Work, "exchange", "")]
	[InlineData(AccountType.Work, "exchange", "   ")]
	public async Task BuildAsync_WhenTenantIdMissingOrWhitespace_ReturnsNonNullClient(AccountType accountType, string prefix, string? tenantId)
	{
		// Arrange
		var account = new Account("test-account", "test@example.com", accountType);
		var serializedRecord = BuildFakeAuthenticationRecordBase64();

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-test-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(serializedRecord);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync($"{prefix}-client-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync("test-client-id");

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync($"{prefix}-tenant-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync(tenantId);

		var graphServiceClientBuilder = CreateGraphServiceClientBuilder();

		// Act
		var result = await graphServiceClientBuilder.BuildAsync(account);

		// Assert — a valid GraphServiceClient is returned even when tenant-id is not set
		result.Should().NotBeNull();
	}

	[Theory]
	[InlineData(AccountType.Personal, "hotmail", "consumers")]
	[InlineData(AccountType.Work, "exchange", "test-tenant-id")]
	public async Task BuildAsync_WhenValidInputsProvided_ReturnsNonNullClient(AccountType accountType, string prefix, string tenantId)
	{
		// Arrange — regression test: BuildAsync must not throw when libsecret is unavailable on Linux.
		// UnsafeAllowUnencryptedStorage = true ensures token cache falls back to plaintext when
		// the OS secure storage (libsecret on Ubuntu 24.04) encounters an error.
		var account = new Account("test-account", "test@example.com", accountType);
		var serializedRecord = BuildFakeAuthenticationRecordBase64();

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-test-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(serializedRecord);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync($"{prefix}-client-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync("test-client-id");

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync($"{prefix}-tenant-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync(tenantId);

		var graphServiceClientBuilder = CreateGraphServiceClientBuilder();

		// Act
		var result = await graphServiceClientBuilder.BuildAsync(account);

		// Assert
		result.Should().NotBeNull();
	}

	/// <summary>
	/// Builds a minimal valid <see cref="AuthenticationRecord"/> and returns it as a Base64-encoded string,
	/// matching the format stored in Key Vault by <see cref="AuthenticationService"/>.
	/// </summary>
	private static string BuildFakeAuthenticationRecordBase64()
	{
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
		// Synchronous deserialization and serialization are acceptable in this static test helper
		// because it runs outside of any synchronization context that could deadlock.
		using var readStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
		var authRecord = Azure.Identity.AuthenticationRecord.DeserializeAsync(readStream).GetAwaiter().GetResult();
		using var writeStream = new MemoryStream();
		authRecord.SerializeAsync(writeStream).GetAwaiter().GetResult();
		return Convert.ToBase64String(writeStream.ToArray());
	}
}
