using System.Reflection;
using ClawMailCalCli.Commands.Calendar;
using ClawMailCalCli.Commands.Settings;
using ClawMailCalCli.Models;
using ClawMailCalCli.Services.Interfaces;
using Spectre.Console.Cli;

namespace ClawMailCalCli.Tests.Commands;

/// <summary>
/// Unit tests for calendar-related commands.
/// </summary>
[Trait("Category", "Unit")]
public class CalendarCommandTests
{
	private readonly Mock<ICalendarService> _mockCalendarService;
	private readonly Mock<IAccountService> _mockAccountService;
	private readonly Mock<IOutputService> _mockOutputService;

	public CalendarCommandTests()
	{
		_mockCalendarService = new Mock<ICalendarService>();
		_mockAccountService = new Mock<IAccountService>();
		_mockOutputService = new Mock<IOutputService>();
	}

	private static CommandContext CreateCommandContext()
	{
		var remainingArguments = Mock.Of<IRemainingArguments>();
		return (CommandContext)Activator.CreateInstance(
			typeof(CommandContext),
			BindingFlags.Instance | BindingFlags.Public,
			binder: null,
			args: [Array.Empty<string>(), remainingArguments, "calendar", null],
			culture: null)!;
	}

	[Fact]
	public async Task ListCalendarCommand_WhenEventsFound_ReturnsZero()
	{
		// Arrange
		IReadOnlyList<CalendarEventSummary> events =
		[
			new CalendarEventSummary("Team Standup", DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddHours(1), false, "Conference Room A"),
		];

		_mockCalendarService
			.Setup(service => service.GetUpcomingEventsAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(events);

		var command = new ListCalendarCommand(_mockCalendarService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, new ListCalendarSettings(), CancellationToken.None);

		// Assert
		result.Should().Be(0);
	}

	[Fact]
	public async Task ListCalendarCommand_WhenNoEvents_ReturnsZero()
	{
		// Arrange
		_mockCalendarService
			.Setup(service => service.GetUpcomingEventsAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var command = new ListCalendarCommand(_mockCalendarService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, new ListCalendarSettings(), CancellationToken.None);

		// Assert
		result.Should().Be(0);
	}

	[Fact]
	public async Task ListCalendarCommand_WhenServiceReturnsNull_ReturnsOne()
	{
		// Arrange
		_mockCalendarService
			.Setup(service => service.GetUpcomingEventsAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((IReadOnlyList<CalendarEventSummary>?)null);

		var command = new ListCalendarCommand(_mockCalendarService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, new ListCalendarSettings(), CancellationToken.None);

		// Assert
		result.Should().Be(1);
	}

	[Fact]
	public async Task ListCalendarCommand_WhenAllDayEvent_ReturnsZero()
	{
		// Arrange
		IReadOnlyList<CalendarEventSummary> events =
		[
			new CalendarEventSummary("Company Holiday", DateTimeOffset.UtcNow.AddDays(2), DateTimeOffset.UtcNow.AddDays(3), true, null),
		];

		_mockCalendarService
			.Setup(service => service.GetUpcomingEventsAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(events);

		var command = new ListCalendarCommand(_mockCalendarService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, new ListCalendarSettings(), CancellationToken.None);

		// Assert
		result.Should().Be(0);
	}

	[Fact]
	public async Task ListCalendarCommand_WithAccountFlag_PassesAccountNameToService()
	{
		// Arrange
		var accountName = "work-account";
		var account = new Account(accountName, "user@contoso.com", AccountType.Work);

		_mockAccountService
			.Setup(service => service.GetAccountAsync(accountName, It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockCalendarService
			.Setup(service => service.GetUpcomingEventsAsync(accountName, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var command = new ListCalendarCommand(_mockCalendarService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var settings = new ListCalendarSettings { AccountName = accountName };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
		_mockCalendarService.Verify(service => service.GetUpcomingEventsAsync(accountName, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ListCalendarCommand_WithNonExistentAccount_ReturnsOne()
	{
		// Arrange
		var accountName = "nonexistent-account";

		_mockAccountService
			.Setup(service => service.GetAccountAsync(accountName, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Account?)null);

		var command = new ListCalendarCommand(_mockCalendarService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var settings = new ListCalendarSettings { AccountName = accountName };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
		_mockCalendarService.Verify(service => service.GetUpcomingEventsAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task CreateCalendarCommand_WhenEventCreatedSuccessfully_ReturnsZero()
	{
		// Arrange
		_mockCalendarService
			.Setup(service => service.CreateEventAsync(
				"Team Meeting",
				It.IsAny<DateTimeOffset>(),
				It.IsAny<DateTimeOffset>(),
				"Agenda: discuss Q2 goals",
				It.IsAny<string?>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync("event-id-123");

		var command = new CreateCalendarCommand(_mockCalendarService.Object, _mockAccountService.Object);
		var settings = new CreateCalendarSettings
		{
			Title = "Team Meeting",
			StartDateTime = "2026-03-25T09:00:00",
			EndDateTime = "2026-03-25T10:00:00",
			Content = "Agenda: discuss Q2 goals",
		};
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
	}

	[Fact]
	public async Task CreateCalendarCommand_WhenStartDateIsInvalid_ReturnsOne()
	{
		// Arrange
		var command = new CreateCalendarCommand(_mockCalendarService.Object, _mockAccountService.Object);
		var settings = new CreateCalendarSettings
		{
			Title = "Meeting",
			StartDateTime = "not-a-date",
			EndDateTime = "2026-03-25T10:00:00",
			Content = "Content",
		};
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
	}

	[Fact]
	public async Task CreateCalendarCommand_WhenEndDateIsInvalid_ReturnsOne()
	{
		// Arrange
		var command = new CreateCalendarCommand(_mockCalendarService.Object, _mockAccountService.Object);
		var settings = new CreateCalendarSettings
		{
			Title = "Meeting",
			StartDateTime = "2026-03-25T09:00:00",
			EndDateTime = "not-a-date",
			Content = "Content",
		};
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
	}

	[Fact]
	public async Task CreateCalendarCommand_WhenEndDateBeforeStartDate_ReturnsOne()
	{
		// Arrange
		var command = new CreateCalendarCommand(_mockCalendarService.Object, _mockAccountService.Object);
		var settings = new CreateCalendarSettings
		{
			Title = "Meeting",
			StartDateTime = "2026-03-25T10:00:00",
			EndDateTime = "2026-03-25T09:00:00",
			Content = "Content",
		};
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
	}

	[Fact]
	public async Task CreateCalendarCommand_WhenServiceReturnsNull_ReturnsOne()
	{
		// Arrange
		_mockCalendarService
			.Setup(service => service.CreateEventAsync(
				It.IsAny<string>(),
				It.IsAny<DateTimeOffset>(),
				It.IsAny<DateTimeOffset>(),
				It.IsAny<string>(),
				It.IsAny<string?>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var command = new CreateCalendarCommand(_mockCalendarService.Object, _mockAccountService.Object);
		var settings = new CreateCalendarSettings
		{
			Title = "Meeting",
			StartDateTime = "2026-03-25T09:00:00",
			EndDateTime = "2026-03-25T10:00:00",
			Content = "Content",
		};
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
	}

	[Fact]
	public async Task CreateCalendarCommand_WithAccountFlag_PassesAccountNameToService()
	{
		// Arrange
		var accountName = "work-account";
		var account = new Account(accountName, "user@contoso.com", AccountType.Work);

		_mockAccountService
			.Setup(service => service.GetAccountAsync(accountName, It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockCalendarService
			.Setup(service => service.CreateEventAsync(
				It.IsAny<string>(),
				It.IsAny<DateTimeOffset>(),
				It.IsAny<DateTimeOffset>(),
				It.IsAny<string>(),
				accountName,
				It.IsAny<CancellationToken>()))
			.ReturnsAsync("event-id-456");

		var command = new CreateCalendarCommand(_mockCalendarService.Object, _mockAccountService.Object);
		var settings = new CreateCalendarSettings
		{
			Title = "Meeting",
			StartDateTime = "2026-03-25T09:00:00",
			EndDateTime = "2026-03-25T10:00:00",
			Content = "Content",
			AccountName = accountName,
		};
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
		_mockCalendarService.Verify(
			service => service.CreateEventAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>(), accountName, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task CreateCalendarCommand_WithNonExistentAccount_ReturnsOne()
	{
		// Arrange
		var accountName = "nonexistent-account";

		_mockAccountService
			.Setup(service => service.GetAccountAsync(accountName, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Account?)null);

		var command = new CreateCalendarCommand(_mockCalendarService.Object, _mockAccountService.Object);
		var settings = new CreateCalendarSettings
		{
			Title = "Meeting",
			StartDateTime = "2026-03-25T09:00:00",
			EndDateTime = "2026-03-25T10:00:00",
			Content = "Content",
			AccountName = accountName,
		};
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
		_mockCalendarService.Verify(
			service => service.CreateEventAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}
}
