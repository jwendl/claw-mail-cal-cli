using System.Reflection;
using ClawMailCalCli.Commands.Calendar;
using ClawMailCalCli.Commands.Settings;
using ClawMailCalCli.Configuration;
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
	private readonly Mock<IOutputService> _mockOutputService;

	public CalendarCommandTests()
	{
		_mockCalendarService = new Mock<ICalendarService>();
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
			.Setup(service => service.GetUpcomingEventsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(events);

		var command = new ListCalendarCommand(_mockCalendarService.Object, _mockOutputService.Object);
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
			.Setup(service => service.GetUpcomingEventsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var command = new ListCalendarCommand(_mockCalendarService.Object, _mockOutputService.Object);
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
			.Setup(service => service.GetUpcomingEventsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync((IReadOnlyList<CalendarEventSummary>?)null);

		var command = new ListCalendarCommand(_mockCalendarService.Object, _mockOutputService.Object);
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
			.Setup(service => service.GetUpcomingEventsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(events);

		var command = new ListCalendarCommand(_mockCalendarService.Object, _mockOutputService.Object);
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, new ListCalendarSettings(), CancellationToken.None);

		// Assert
		result.Should().Be(0);
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
				It.IsAny<CancellationToken>()))
			.ReturnsAsync("event-id-123");

		var command = new CreateCalendarCommand(_mockCalendarService.Object);
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
		var command = new CreateCalendarCommand(_mockCalendarService.Object);
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
		var command = new CreateCalendarCommand(_mockCalendarService.Object);
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
		var command = new CreateCalendarCommand(_mockCalendarService.Object);
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
				It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var command = new CreateCalendarCommand(_mockCalendarService.Object);
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
	public async Task DeleteCalendarCommand_WhenConfirmFlagSetAndDeleteSucceeds_ReturnsZero()
	{
		// Arrange
		var mockConfigurationService = new Mock<IConfigurationService>();

		_mockCalendarService
			.Setup(service => service.DeleteEventAsync("Team Meeting", "my-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var command = new DeleteCalendarCommand(_mockCalendarService.Object, mockConfigurationService.Object, _mockOutputService.Object);
		var settings = new DeleteCalendarSettings { Query = "Team Meeting", AccountName = "my-account", Confirm = true };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
	}

	[Fact]
	public async Task DeleteCalendarCommand_WhenConfirmFlagSetAndDeleteFails_ReturnsOne()
	{
		// Arrange
		var mockConfigurationService = new Mock<IConfigurationService>();

		_mockCalendarService
			.Setup(service => service.DeleteEventAsync("Nonexistent", "my-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		var command = new DeleteCalendarCommand(_mockCalendarService.Object, mockConfigurationService.Object, _mockOutputService.Object);
		var settings = new DeleteCalendarSettings { Query = "Nonexistent", AccountName = "my-account", Confirm = true };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
	}

	[Fact]
	public async Task DeleteCalendarCommand_WhenNoAccountSpecified_ReturnsOne()
	{
		// Arrange
		var mockConfigurationService = new Mock<IConfigurationService>();
		mockConfigurationService
			.Setup(service => service.ReadConfigurationAsync())
			.ThrowsAsync(new InvalidOperationException("Config not found."));

		var command = new DeleteCalendarCommand(_mockCalendarService.Object, mockConfigurationService.Object, _mockOutputService.Object);
		var settings = new DeleteCalendarSettings { Query = "Team Meeting", AccountName = null, Confirm = true };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
	}

	[Fact]
	public async Task DeleteCalendarCommand_WhenConfirmFlagSetAndDeleteSucceeds_WritesJsonOutput()
	{
		// Arrange
		var mockConfigurationService = new Mock<IConfigurationService>();

		_mockCalendarService
			.Setup(service => service.DeleteEventAsync("Team Meeting", "my-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var command = new DeleteCalendarCommand(_mockCalendarService.Object, mockConfigurationService.Object, _mockOutputService.Object);
		var settings = new DeleteCalendarSettings { Query = "Team Meeting", AccountName = "my-account", Confirm = true, Json = true };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
		_mockOutputService.Verify(service => service.WriteJson(It.IsAny<object>()), Times.Once);
	}

	[Fact]
	public async Task DeleteCalendarCommand_WhenConfirmFlagSetAndDeleteFailsWithJson_WritesJsonAndReturnsOne()
	{
		// Arrange
		var mockConfigurationService = new Mock<IConfigurationService>();

		_mockCalendarService
			.Setup(service => service.DeleteEventAsync("Nonexistent", "my-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		var command = new DeleteCalendarCommand(_mockCalendarService.Object, mockConfigurationService.Object, _mockOutputService.Object);
		var settings = new DeleteCalendarSettings { Query = "Nonexistent", AccountName = "my-account", Confirm = true, Json = true };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
		_mockOutputService.Verify(service => service.WriteJson(It.IsAny<object>()), Times.Once);
	}

	[Fact]
	public async Task DeleteCalendarCommand_WhenDefaultAccountUsedFromConfig_CallsDeleteWithDefaultAccount()
	{
		// Arrange
		var mockConfigurationService = new Mock<IConfigurationService>();
		mockConfigurationService
			.Setup(service => service.ReadConfigurationAsync())
			.ReturnsAsync(new ClawConfiguration("https://myvault.vault.azure.net/", "default-account"));

		_mockCalendarService
			.Setup(service => service.DeleteEventAsync("Team Meeting", "default-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var command = new DeleteCalendarCommand(_mockCalendarService.Object, mockConfigurationService.Object, _mockOutputService.Object);
		var settings = new DeleteCalendarSettings { Query = "Team Meeting", AccountName = null, Confirm = true };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
		_mockCalendarService.Verify(
			service => service.DeleteEventAsync("Team Meeting", "default-account", It.IsAny<CancellationToken>()),
			Times.Once);
	}
}
