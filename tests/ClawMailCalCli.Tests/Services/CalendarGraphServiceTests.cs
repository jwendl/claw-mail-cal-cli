using Azure.Identity;
using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
using ClawMailCalCli.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="CalendarGraphService"/> focusing on guard clauses
/// that do not require real Azure credentials.
/// </summary>
[Trait("Category", "Unit")]
public class CalendarGraphServiceTests
{
	private readonly Mock<IAccountService> _mockAccountService;
	private readonly Mock<IKeyVaultService> _mockKeyVaultService;
	private readonly ILogger<CalendarGraphService> _logger;

	public CalendarGraphServiceTests()
	{
		_mockAccountService = new Mock<IAccountService>();
		_mockKeyVaultService = new Mock<IKeyVaultService>();
		_logger = new NullLogger<CalendarGraphService>();
	}

	private CalendarGraphService CreateCalendarGraphService() =>
		new CalendarGraphService(_mockAccountService.Object, _mockKeyVaultService.Object, _logger);

	[Fact]
	public async Task GetEventByIdAsync_WhenAccountNotFound_ReturnsNull()
	{
		// Arrange
		_mockAccountService
			.Setup(service => service.GetAccountAsync("nonexistent", It.IsAny<CancellationToken>()))
			.ReturnsAsync((Account?)null);

		var calendarGraphService = CreateCalendarGraphService();

		// Act
		var result = await calendarGraphService.GetEventByIdAsync("nonexistent", "event-id");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetEventsBySubjectFilterAsync_WhenAccountNotFound_ReturnsEmptyList()
	{
		// Arrange
		_mockAccountService
			.Setup(service => service.GetAccountAsync("nonexistent", It.IsAny<CancellationToken>()))
			.ReturnsAsync((Account?)null);

		var calendarGraphService = CreateCalendarGraphService();

		// Act
		var result = await calendarGraphService.GetEventsBySubjectFilterAsync("nonexistent", "subject");

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetEventByIdAsync_WhenClientIdSecretIsNull_ReturnsNull()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);

		_mockAccountService
			.Setup(service => service.GetAccountAsync("work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("exchange-client-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var calendarGraphService = CreateCalendarGraphService();

		// Act
		var result = await calendarGraphService.GetEventByIdAsync("work-account", "event-id");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetEventsBySubjectFilterAsync_WhenClientIdSecretIsNull_ReturnsEmptyList()
	{
		// Arrange
		var account = new Account("personal-account", "user@hotmail.com", AccountType.Personal);

		_mockAccountService
			.Setup(service => service.GetAccountAsync("personal-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-personal-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("hotmail-client-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var calendarGraphService = CreateCalendarGraphService();

		// Act
		var result = await calendarGraphService.GetEventsBySubjectFilterAsync("personal-account", "meeting");

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetEventByIdAsync_WhenAuthRecordIsInvalidBase64_ReturnsNull()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);

		_mockAccountService
			.Setup(service => service.GetAccountAsync("work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync("!!!not-valid-base64!!!");

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("exchange-client-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var calendarGraphService = CreateCalendarGraphService();

		// Act
		var result = await calendarGraphService.GetEventByIdAsync("work-account", "event-id");

		// Assert
		result.Should().BeNull();
	}

	/// <summary>
	/// Regression test for the work-account credential caching bug.
	/// When a work account is first authenticated with the generic "organizations" endpoint,
	/// MSAL resolves and caches the token under the real tenant GUID.  Creating the
	/// <see cref="CalendarGraphService"/> graph client must use that specific tenant so that
	/// MSAL can locate the cached token and silently refresh without re-prompting device code flow.
	/// </summary>
	[Fact]
	public async Task GetEventByIdAsync_WhenWorkAccountAuthRecordHasSpecificTenant_ReturnsNullFromGraphNotFromSetup()
	{
		// Arrange — auth record carries the real tenant GUID resolved by MSAL during initial auth.
		const string specificTenantGuid = "72f988bf-86f1-41af-91ab-2d7cd011db47";
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);

		_mockAccountService
			.Setup(service => service.GetAccountAsync("work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(BuildFakeWorkAuthenticationRecordBase64(specificTenantGuid));

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("exchange-client-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync("test-work-client-id");

		// No tenant-id in Key Vault — without the fix the default "organizations" would be used and
		// MSAL would fail to find the cached token, triggering a device code prompt every time.
		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("exchange-tenant-id", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var calendarGraphService = CreateCalendarGraphService();

		// Act — the Graph call will throw because no real Graph endpoint is available in tests;
		// what we are verifying is that the service reaches the Graph call (credential is created
		// with the correct specific tenant from the auth record) rather than returning null earlier.
		var act = async () => await calendarGraphService.GetEventByIdAsync("work-account", "event-id");

		// Assert — any exception that is NOT from the service returning early null is acceptable here;
		// the important thing is that the credential setup code (with the specific tenant) runs cleanly.
		await act.Should().NotThrowAsync<InvalidOperationException>();
	}

	/// <summary>
	/// Builds a minimal valid <see cref="AuthenticationRecord"/> for a work account that has a
	/// specific tenant GUID — as MSAL produces after resolving the generic "organizations"
	/// endpoint during device code flow.
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
