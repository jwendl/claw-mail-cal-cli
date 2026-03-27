using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
using ClawMailCalCli.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="CalendarService"/>.
/// </summary>
[Trait("Category", "Unit")]
public class CalendarServiceTests
{
	private readonly Mock<ICalendarGraphService> _mockCalendarGraphService;
	private readonly Mock<IGraphClientService> _mockGraphClientService;
	private readonly CalendarService _calendarService;

	/// <summary>
	/// Initializes a new instance of <see cref="CalendarServiceTests"/>.
	/// </summary>
	public CalendarServiceTests()
	{
		_mockCalendarGraphService = new Mock<ICalendarGraphService>();
		_mockGraphClientService = new Mock<IGraphClientService>();
		_calendarService = new CalendarService(_mockCalendarGraphService.Object, _mockGraphClientService.Object, Mock.Of<ILogger<CalendarService>>(), Mock.Of<IOutputService>());
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

	[Fact]
	public async Task GetUpcomingEventsAsync_WhenNoDefaultAccount_ReturnsNull()
	{
		// Arrange
		_mockGraphClientService
			.Setup(graphClientService => graphClientService.ExecuteWithRetryAsync(
				It.IsAny<Func<GraphServiceClient, Task<EventCollectionResponse?>>>(),
				It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("No default account configured."));

		// Act
		var result = await _calendarService.GetUpcomingEventsAsync();

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetUpcomingEventsAsync_WhenResponseHasNullValue_ReturnsEmptyList()
	{
		// Arrange
		_mockGraphClientService
			.Setup(graphClientService => graphClientService.ExecuteWithRetryAsync(
				It.IsAny<Func<GraphServiceClient, Task<EventCollectionResponse?>>>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(new EventCollectionResponse { Value = null });

		// Act
		var result = await _calendarService.GetUpcomingEventsAsync();

		// Assert
		result.Should().NotBeNull();
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetUpcomingEventsAsync_WhenNoEvents_ReturnsEmptyList()
	{
		// Arrange
		_mockGraphClientService
			.Setup(graphClientService => graphClientService.ExecuteWithRetryAsync(
				It.IsAny<Func<GraphServiceClient, Task<EventCollectionResponse?>>>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(new EventCollectionResponse { Value = [] });

		// Act
		var result = await _calendarService.GetUpcomingEventsAsync();

		// Assert
		result.Should().NotBeNull();
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetUpcomingEventsAsync_WithSingleEvent_MapsSubjectToTitle()
	{
		// Arrange
		var graphEvent = BuildGraphEvent("Team Standup", "2025-03-22T09:00:00Z", "2025-03-22T09:30:00Z");
		SetupGraphResponse([graphEvent]);

		// Act
		var result = await _calendarService.GetUpcomingEventsAsync();

		// Assert
		result.Should().HaveCount(1);
		result![0].Title.Should().Be("Team Standup");
	}

	[Fact]
	public async Task GetUpcomingEventsAsync_WithNullSubject_UsesDefaultTitle()
	{
		// Arrange
		var graphEvent = BuildGraphEvent(null, "2025-03-22T09:00:00Z", "2025-03-22T09:30:00Z");
		SetupGraphResponse([graphEvent]);

		// Act
		var result = await _calendarService.GetUpcomingEventsAsync();

		// Assert
		result.Should().HaveCount(1);
		result![0].Title.Should().Be("(No Title)");
	}

	[Fact]
	public async Task GetUpcomingEventsAsync_WithAllDayEvent_SetsIsAllDayTrue()
	{
		// Arrange
		var graphEvent = BuildGraphEvent("Holiday", "2025-03-22T00:00:00Z", "2025-03-23T00:00:00Z", isAllDay: true);
		SetupGraphResponse([graphEvent]);

		// Act
		var result = await _calendarService.GetUpcomingEventsAsync();

		// Assert
		result.Should().HaveCount(1);
		result![0].IsAllDay.Should().BeTrue();
	}

	[Fact]
	public async Task GetUpcomingEventsAsync_WithRegularEvent_SetsIsAllDayFalse()
	{
		// Arrange
		var graphEvent = BuildGraphEvent("Meeting", "2025-03-22T10:00:00Z", "2025-03-22T11:00:00Z", isAllDay: false);
		SetupGraphResponse([graphEvent]);

		// Act
		var result = await _calendarService.GetUpcomingEventsAsync();

		// Assert
		result.Should().HaveCount(1);
		result![0].IsAllDay.Should().BeFalse();
	}

	[Fact]
	public async Task GetUpcomingEventsAsync_WithLocation_MapsLocationDisplayName()
	{
		// Arrange
		var graphEvent = BuildGraphEvent("Team Standup", "2025-03-22T09:00:00Z", "2025-03-22T09:30:00Z", location: "Teams");
		SetupGraphResponse([graphEvent]);

		// Act
		var result = await _calendarService.GetUpcomingEventsAsync();

		// Assert
		result.Should().HaveCount(1);
		result![0].Location.Should().Be("Teams");
	}

	[Fact]
	public async Task GetUpcomingEventsAsync_WithNullLocation_ReturnsNullLocation()
	{
		// Arrange
		var graphEvent = BuildGraphEvent("Team Standup", "2025-03-22T09:00:00Z", "2025-03-22T09:30:00Z", location: null);
		SetupGraphResponse([graphEvent]);

		// Act
		var result = await _calendarService.GetUpcomingEventsAsync();

		// Assert
		result.Should().HaveCount(1);
		result![0].Location.Should().BeNull();
	}

	[Fact]
	public async Task GetUpcomingEventsAsync_WithMultipleEvents_ReturnsSortedByStartAscending()
	{
		// Arrange
		var laterEvent = BuildGraphEvent("Later Meeting", "2025-03-22T14:00:00Z", "2025-03-22T15:00:00Z");
		var earlierEvent = BuildGraphEvent("Morning Standup", "2025-03-22T09:00:00Z", "2025-03-22T09:30:00Z");
		var middleEvent = BuildGraphEvent("Lunch Break", "2025-03-22T12:00:00Z", "2025-03-22T13:00:00Z");
		SetupGraphResponse([laterEvent, earlierEvent, middleEvent]);

		// Act
		var result = await _calendarService.GetUpcomingEventsAsync();

		// Assert
		result.Should().HaveCount(3);
		result![0].Title.Should().Be("Morning Standup");
		result[1].Title.Should().Be("Lunch Break");
		result[2].Title.Should().Be("Later Meeting");
	}

	[Fact]
	public async Task GetUpcomingEventsAsync_CallsExecuteWithRetryAsync()
	{
		// Arrange
		SetupGraphResponse([]);

		// Act
		await _calendarService.GetUpcomingEventsAsync();

		// Assert
		_mockGraphClientService.Verify(
			graphClientService => graphClientService.ExecuteWithRetryAsync(
				It.IsAny<Func<GraphServiceClient, Task<EventCollectionResponse?>>>(),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task GetUpcomingEventsAsync_WithValidStartDateTime_ParsesCorrectly()
	{
		// Arrange
		var graphEvent = BuildGraphEvent("Meeting", "2025-03-22T09:00:00Z", "2025-03-22T10:00:00Z");
		SetupGraphResponse([graphEvent]);

		// Act
		var result = await _calendarService.GetUpcomingEventsAsync();

		// Assert
		result.Should().HaveCount(1);
		result![0].Start.Year.Should().Be(2025);
		result[0].Start.Month.Should().Be(3);
		result[0].Start.Day.Should().Be(22);
		result[0].Start.Hour.Should().Be(9);
		result[0].Start.Minute.Should().Be(0);
	}

	[Fact]
	public async Task GetUpcomingEventsAsync_WithValidEndDateTime_ParsesCorrectly()
	{
		// Arrange
		var graphEvent = BuildGraphEvent("Meeting", "2025-03-22T09:00:00Z", "2025-03-22T09:30:00Z");
		SetupGraphResponse([graphEvent]);

		// Act
		var result = await _calendarService.GetUpcomingEventsAsync();

		// Assert
		result.Should().HaveCount(1);
		result![0].End.Hour.Should().Be(9);
		result[0].End.Minute.Should().Be(30);
	}

	[Fact]
	public async Task GetUpcomingEventsAsync_WithNullStartDateTime_UsesMinValue()
	{
		// Arrange
		var graphEvent = new Event
		{
			Subject = "Mystery Meeting",
			IsAllDay = false,
			Start = null,
			End = new DateTimeTimeZone { DateTime = "2025-03-22T09:30:00Z", TimeZone = "UTC" },
			Location = null,
		};
		SetupGraphResponse([graphEvent]);

		// Act
		var result = await _calendarService.GetUpcomingEventsAsync();

		// Assert
		result.Should().HaveCount(1);
		result![0].Start.Should().Be(DateTimeOffset.MinValue);
	}

	[Fact]
	public async Task GetUpcomingEventsAsync_WithMultipleEvents_ReturnsAllEvents()
	{
		// Arrange
		var events = Enumerable.Range(1, 5)
			.Select(index => BuildGraphEvent($"Event {index}", $"2025-03-{20 + index}T09:00:00Z", $"2025-03-{20 + index}T10:00:00Z"))
			.ToList();
		SetupGraphResponse(events);

		// Act
		var result = await _calendarService.GetUpcomingEventsAsync();

		// Assert
		result.Should().HaveCount(5);
	}

	[Fact]
	public async Task GetUpcomingEventsAsync_WithNonUtcTimezone_AppliesTimezoneOffset()
	{
		// Arrange — "Eastern Standard Time" on Jan 15 (outside DST) is UTC-5
		var graphEvent = new Event
		{
			Subject = "EST Meeting",
			IsAllDay = false,
			Start = new DateTimeTimeZone { DateTime = "2025-01-15T09:00:00", TimeZone = "Eastern Standard Time" },
			End = new DateTimeTimeZone { DateTime = "2025-01-15T10:00:00", TimeZone = "Eastern Standard Time" },
			Location = null,
		};
		SetupGraphResponse([graphEvent]);

		// Act
		var result = await _calendarService.GetUpcomingEventsAsync();

		// Assert — offset should be -5:00 and the local hour should be 9
		result.Should().HaveCount(1);
		result![0].Start.Hour.Should().Be(9);
		result[0].Start.Offset.Should().Be(TimeSpan.FromHours(-5));
	}

	[Fact]
	public async Task GetUpcomingEventsAsync_WithUnrecognizedTimezone_TreatsAsUtc()
	{
		// Arrange — unknown timezone should fall back to UTC
		var graphEvent = new Event
		{
			Subject = "Unknown TZ Meeting",
			IsAllDay = false,
			Start = new DateTimeTimeZone { DateTime = "2025-03-22T09:00:00", TimeZone = "Imaginary/Timezone" },
			End = new DateTimeTimeZone { DateTime = "2025-03-22T10:00:00", TimeZone = "Imaginary/Timezone" },
			Location = null,
		};
		SetupGraphResponse([graphEvent]);

		// Act
		var result = await _calendarService.GetUpcomingEventsAsync();

		// Assert — falls back to UTC offset of 0
		result.Should().HaveCount(1);
		result![0].Start.Offset.Should().Be(TimeSpan.Zero);
		result[0].Start.Hour.Should().Be(9);
	}

	[Fact]
	public async Task CreateEventAsync_WhenGraphReturnsEvent_ReturnsEventId()
	{
		// Arrange
		var createdEvent = new Event { Id = "new-event-id-12345", Subject = "Team Meeting" };
		_mockGraphClientService
			.Setup(graphClientService => graphClientService.ExecuteWithRetryAsync(
				It.IsAny<Func<GraphServiceClient, Task<Event?>>>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(createdEvent);

		// Act
		var result = await _calendarService.CreateEventAsync("Team Meeting", new DateTimeOffset(2026, 3, 25, 9, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 25, 9, 30, 0, TimeSpan.Zero), "Weekly team sync");

		// Assert
		result.Should().Be("new-event-id-12345");
	}

	[Fact]
	public async Task CreateEventAsync_WhenGraphReturnsNull_ReturnsNull()
	{
		// Arrange
		_mockGraphClientService
			.Setup(graphClientService => graphClientService.ExecuteWithRetryAsync(
				It.IsAny<Func<GraphServiceClient, Task<Event?>>>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync((Event?)null);

		// Act
		var result = await _calendarService.CreateEventAsync("Team Meeting", new DateTimeOffset(2026, 3, 25, 9, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 25, 9, 30, 0, TimeSpan.Zero), "Weekly team sync");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task CreateEventAsync_WhenInvalidOperationExceptionThrown_ReturnsNull()
	{
		// Arrange
		_mockGraphClientService
			.Setup(graphClientService => graphClientService.ExecuteWithRetryAsync(
				It.IsAny<Func<GraphServiceClient, Task<Event?>>>(),
				It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("No default account configured."));

		// Act
		var result = await _calendarService.CreateEventAsync("Team Meeting", new DateTimeOffset(2026, 3, 25, 9, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 25, 9, 30, 0, TimeSpan.Zero), "Weekly team sync");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task CreateEventAsync_WhenODataErrorThrown_ReturnsNull()
	{
		// Arrange
		_mockGraphClientService
			.Setup(graphClientService => graphClientService.ExecuteWithRetryAsync(
				It.IsAny<Func<GraphServiceClient, Task<Event?>>>(),
				It.IsAny<CancellationToken>()))
			.ThrowsAsync(new ODataError { ResponseStatusCode = 400 });

		// Act
		var result = await _calendarService.CreateEventAsync("Team Meeting", new DateTimeOffset(2026, 3, 25, 9, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 25, 9, 30, 0, TimeSpan.Zero), "Weekly team sync");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task CreateEventAsync_CallsExecuteWithRetryAsyncOnce()
	{
		// Arrange
		var createdEvent = new Event { Id = "event-abc" };
		_mockGraphClientService
			.Setup(graphClientService => graphClientService.ExecuteWithRetryAsync(
				It.IsAny<Func<GraphServiceClient, Task<Event?>>>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(createdEvent);

		// Act
		await _calendarService.CreateEventAsync("Budget Review", new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 4, 1, 11, 0, 0, TimeSpan.Zero), "Q2 budget sync");

		// Assert
		_mockGraphClientService.Verify(
			graphClientService => graphClientService.ExecuteWithRetryAsync(
				It.IsAny<Func<GraphServiceClient, Task<Event?>>>(),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task CreateEventAsync_WhenEventHasNullId_ReturnsNull()
	{
		// Arrange
		var createdEvent = new Event { Id = null, Subject = "Team Meeting" };
		_mockGraphClientService
			.Setup(graphClientService => graphClientService.ExecuteWithRetryAsync(
				It.IsAny<Func<GraphServiceClient, Task<Event?>>>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(createdEvent);

		// Act
		var result = await _calendarService.CreateEventAsync("Team Meeting", new DateTimeOffset(2026, 3, 25, 9, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 25, 9, 30, 0, TimeSpan.Zero), "Weekly team sync");

		// Assert
		result.Should().BeNull();
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

	private void SetupGraphResponse(List<Event> events)
	{
		_mockGraphClientService
			.Setup(graphClientService => graphClientService.ExecuteWithRetryAsync(
				It.IsAny<Func<GraphServiceClient, Task<EventCollectionResponse?>>>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(new EventCollectionResponse { Value = events });
	}

	private static Event BuildGraphEvent(string? subject, string startDateTime, string endDateTime, bool isAllDay = false, string? location = null)
	{
		return new Event
		{
			Subject = subject,
			IsAllDay = isAllDay,
			Start = new DateTimeTimeZone { DateTime = startDateTime, TimeZone = "UTC" },
			End = new DateTimeTimeZone { DateTime = endDateTime, TimeZone = "UTC" },
			Location = location is not null ? new Location { DisplayName = location } : null,
		};
	}
}
