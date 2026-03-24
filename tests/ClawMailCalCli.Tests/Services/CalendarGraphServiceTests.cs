using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
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
}
