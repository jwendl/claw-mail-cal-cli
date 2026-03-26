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

	/// <summary>
	/// Regression test for the work-account credential caching bug.
	/// When a work account is first authenticated with the generic "organizations" endpoint,
	/// MSAL resolves and caches the token under the real tenant GUID.  On subsequent calls the
	/// credential must be created with that same tenant GUID — not "organizations" — so that
	/// MSAL can locate the cached token and silently refresh it without prompting device code flow.
	/// </summary>
	[Fact]
	public async Task BuildAsync_WhenWorkAccountAuthRecordHasSpecificTenant_ReturnsNonNullClient()
	{
		// Arrange — simulate a work account whose auth record was produced by MSAL after
		// the generic "organizations" authority resolved to the real tenant during device code flow.
		const string specificTenantGuid = "72f988bf-86f1-41af-91ab-2d7cd011db47";
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		var workAuthRecord = BuildFakeWorkAuthenticationRecordBase64(specificTenantGuid);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(workAuthRecord);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("exchange-client-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync("test-work-client-id");

		// No exchange-tenant-id set in Key Vault — the default "organizations" endpoint would be used
		// without the fix, causing silent auth to fail on every request.
		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("exchange-tenant-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var graphServiceClientBuilder = CreateGraphServiceClientBuilder();

		// Act
		var result = await graphServiceClientBuilder.BuildAsync(account);

		// Assert — a non-null client must be returned and the credential should use the specific
		// tenant GUID from the auth record so that MSAL can find the cached token.
		result.Should().NotBeNull();
	}

	/// <summary>
	/// Regression test: when Key Vault has no tenant ID for a work account but the
	/// AuthenticationRecord carries the real tenant GUID, <see cref="GraphServiceClientBuilder"/>
	/// must still return a valid <see cref="GraphServiceClient"/>.
	/// </summary>
	[Fact]
	public async Task BuildAsync_WhenWorkAccountTenantIdNullAndAuthRecordHasSpecificTenant_ReturnsNonNullClient()
	{
		// Arrange
		const string specificTenantGuid = "contoso-tenant-id-guid";
		var account = new Account("contoso-work", "user@contoso.com", AccountType.Work);
		var workAuthRecord = BuildFakeWorkAuthenticationRecordBase64(specificTenantGuid);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-contoso-work", It.IsAny<CancellationToken>()))
			.ReturnsAsync(workAuthRecord);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("exchange-client-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync("test-client-id");

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("exchange-tenant-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

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

	/// <summary>
	/// Builds a minimal valid <see cref="AuthenticationRecord"/> for a work account that has a
	/// specific tenant GUID — as MSAL produces after resolving the generic "organizations"
	/// endpoint during the initial device code flow.
	/// Returns the record as a Base64-encoded string suitable for storing in Key Vault.
	/// </summary>
	private static string BuildFakeWorkAuthenticationRecordBase64(string specificTenantGuid)
	{
		var json = $$"""
			{
				"username": "user@contoso.com",
				"authority": "https://login.microsoftonline.com/{{specificTenantGuid}}",
				"homeAccountId": "00000000-0000-0000-0000-000000000001.00000000-0000-0000-0000-000000000002",
				"tenantId": "{{specificTenantGuid}}",
				"clientId": "test-work-client-id",
				"version": "1.0"
			}
			""";
		using var readStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
		var authRecord = Azure.Identity.AuthenticationRecord.DeserializeAsync(readStream).GetAwaiter().GetResult();
		using var writeStream = new MemoryStream();
		authRecord.SerializeAsync(writeStream).GetAwaiter().GetResult();
		return Convert.ToBase64String(writeStream.ToArray());
	}
}
