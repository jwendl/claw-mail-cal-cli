using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
using Microsoft.Extensions.Logging;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="CalendarService"/>.
/// </summary>
[Trait("Category", "Unit")]
public class CalendarServiceTests
{
	private readonly Mock<ICalendarGraphService> _mockCalendarGraphService;
	private readonly CalendarService _calendarService;

	/// <summary>
	/// Initializes a new instance of <see cref="CalendarServiceTests"/>.
	/// </summary>
	public CalendarServiceTests()
	{
		_mockCalendarGraphService = new Mock<ICalendarGraphService>();
		_calendarService = new CalendarService(_mockCalendarGraphService.Object, Mock.Of<ILogger<CalendarService>>());
	}

	[Fact]
	public async Task ReadEventAsync_WithEmptyQuery_ReturnsNull()
	{
		// Arrange (no setup needed)

		// Act
		var result = await _calendarService.ReadEventAsync(string.Empty, "myaccount");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task ReadEventAsync_WithWhitespaceQuery_ReturnsNull()
	{
		// Arrange (no setup needed)

		// Act
		var result = await _calendarService.ReadEventAsync("   ", "myaccount");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task ReadEventAsync_WithEmptyQuery_DoesNotCallGraphService()
	{
		// Arrange (no setup needed)

		// Act
		await _calendarService.ReadEventAsync(string.Empty, "myaccount");

		// Assert
		_mockCalendarGraphService.Verify(
			graphService => graphService.GetEventByIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Never);
		_mockCalendarGraphService.Verify(
			graphService => graphService.GetEventsBySubjectFilterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ReadEventAsync_WithShortQuery_CallsGetEventsBySubjectFilter()
	{
		// Arrange
		_mockCalendarGraphService
			.Setup(graphService => graphService.GetEventsBySubjectFilterAsync("myaccount", "Team Meeting", It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		// Act
		await _calendarService.ReadEventAsync("Team Meeting", "myaccount");

		// Assert
		_mockCalendarGraphService.Verify(
			graphService => graphService.GetEventsBySubjectFilterAsync("myaccount", "Team Meeting", It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ReadEventAsync_WithShortQuery_DoesNotCallGetEventById()
	{
		// Arrange
		_mockCalendarGraphService
			.Setup(graphService => graphService.GetEventsBySubjectFilterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		// Act
		await _calendarService.ReadEventAsync("Team Meeting", "myaccount");

		// Assert
		_mockCalendarGraphService.Verify(
			graphService => graphService.GetEventByIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ReadEventAsync_WithLongQuery_CallsGetEventById()
	{
		// Arrange
		var eventId = new string('A', 50);
		_mockCalendarGraphService
			.Setup(graphService => graphService.GetEventByIdAsync("myaccount", eventId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((CalendarEvent?)null);

		// Act
		await _calendarService.ReadEventAsync(eventId, "myaccount");

		// Assert
		_mockCalendarGraphService.Verify(
			graphService => graphService.GetEventByIdAsync("myaccount", eventId, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ReadEventAsync_WithLongQuery_DoesNotCallGetEventsBySubjectFilter()
	{
		// Arrange
		var eventId = new string('A', 50);
		_mockCalendarGraphService
			.Setup(graphService => graphService.GetEventByIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((CalendarEvent?)null);

		// Act
		await _calendarService.ReadEventAsync(eventId, "myaccount");

		// Assert
		_mockCalendarGraphService.Verify(
			graphService => graphService.GetEventsBySubjectFilterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ReadEventAsync_WhenTitleSearchReturnsNoResults_ReturnsNull()
	{
		// Arrange
		_mockCalendarGraphService
			.Setup(graphService => graphService.GetEventsBySubjectFilterAsync("myaccount", "Budget Review", It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		// Act
		var result = await _calendarService.ReadEventAsync("Budget Review", "myaccount");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task ReadEventAsync_WhenTitleSearchReturnsResults_ReturnsFirstEvent()
	{
		// Arrange
		var firstEvent = CreateTestCalendarEvent("event-1", "Team Meeting");
		var secondEvent = CreateTestCalendarEvent("event-2", "Team Meeting Follow-up");

		_mockCalendarGraphService
			.Setup(graphService => graphService.GetEventsBySubjectFilterAsync("myaccount", "Team Meeting", It.IsAny<CancellationToken>()))
			.ReturnsAsync([firstEvent, secondEvent]);

		// Act
		var result = await _calendarService.ReadEventAsync("Team Meeting", "myaccount");

		// Assert
		result.Should().NotBeNull();
		result!.Id.Should().Be("event-1");
		result.Subject.Should().Be("Team Meeting");
	}

	[Fact]
	public async Task ReadEventAsync_WhenIdSearchReturnsNull_ReturnsNull()
	{
		// Arrange
		var eventId = new string('A', 50);
		_mockCalendarGraphService
			.Setup(graphService => graphService.GetEventByIdAsync("myaccount", eventId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((CalendarEvent?)null);

		// Act
		var result = await _calendarService.ReadEventAsync(eventId, "myaccount");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task ReadEventAsync_WhenIdSearchReturnsEvent_ReturnsEvent()
	{
		// Arrange
		var eventId = new string('A', 50);
		var calendarEvent = CreateTestCalendarEvent(eventId, "All-Hands Meeting");

		_mockCalendarGraphService
			.Setup(graphService => graphService.GetEventByIdAsync("myaccount", eventId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(calendarEvent);

		// Act
		var result = await _calendarService.ReadEventAsync(eventId, "myaccount");

		// Assert
		result.Should().NotBeNull();
		result!.Id.Should().Be(eventId);
		result.Subject.Should().Be("All-Hands Meeting");
	}

	[Fact]
	public async Task ReadEventAsync_WithQueryOf49Chars_UsesSubjectSearch()
	{
		// Arrange
		var shortishQuery = new string('x', 49);
		_mockCalendarGraphService
			.Setup(graphService => graphService.GetEventsBySubjectFilterAsync("myaccount", shortishQuery, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		// Act
		await _calendarService.ReadEventAsync(shortishQuery, "myaccount");

		// Assert
		_mockCalendarGraphService.Verify(
			graphService => graphService.GetEventsBySubjectFilterAsync("myaccount", shortishQuery, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ReadEventAsync_WithQueryOfExactly50Chars_UsesIdSearch()
	{
		// Arrange
		var borderlineId = new string('x', 50);
		_mockCalendarGraphService
			.Setup(graphService => graphService.GetEventByIdAsync("myaccount", borderlineId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((CalendarEvent?)null);

		// Act
		await _calendarService.ReadEventAsync(borderlineId, "myaccount");

		// Assert
		_mockCalendarGraphService.Verify(
			graphService => graphService.GetEventByIdAsync("myaccount", borderlineId, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ReadEventAsync_PassesCorrectAccountNameToGraphService()
	{
		// Arrange
		_mockCalendarGraphService
			.Setup(graphService => graphService.GetEventsBySubjectFilterAsync("work-account", "Sprint Review", It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		// Act
		await _calendarService.ReadEventAsync("Sprint Review", "work-account");

		// Assert
		_mockCalendarGraphService.Verify(
			graphService => graphService.GetEventsBySubjectFilterAsync("work-account", "Sprint Review", It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ReadEventAsync_ReturnsEventWithAllFields_WhenEventFound()
	{
		// Arrange
		var expectedEvent = new CalendarEvent(
			Id: "event-abc",
			Subject: "Quarterly Review",
			Start: "2024-06-01T09:00:00",
			End: "2024-06-01T10:00:00",
			Location: "Board Room",
			Organizer: "Alice Smith",
			Attendees: ["Bob Jones", "Carol White"],
			Body: "Please review the attached documents."
		);

		_mockCalendarGraphService
			.Setup(graphService => graphService.GetEventsBySubjectFilterAsync("myaccount", "Quarterly Review", It.IsAny<CancellationToken>()))
			.ReturnsAsync([expectedEvent]);

		// Act
		var result = await _calendarService.ReadEventAsync("Quarterly Review", "myaccount");

		// Assert
		result.Should().NotBeNull();
		result!.Id.Should().Be("event-abc");
		result.Subject.Should().Be("Quarterly Review");
		result.Start.Should().Be("2024-06-01T09:00:00");
		result.End.Should().Be("2024-06-01T10:00:00");
		result.Location.Should().Be("Board Room");
		result.Organizer.Should().Be("Alice Smith");
		result.Attendees.Should().BeEquivalentTo(["Bob Jones", "Carol White"]);
		result.Body.Should().Be("Please review the attached documents.");
	}

	private static CalendarEvent CreateTestCalendarEvent(string id, string subject) =>
		new CalendarEvent(
			Id: id,
			Subject: subject,
			Start: "2024-01-15T10:00:00",
			End: "2024-01-15T11:00:00",
			Location: "Conference Room A",
			Organizer: "Jane Doe",
			Attendees: ["John Smith"],
			Body: "Meeting agenda."
		);
}
