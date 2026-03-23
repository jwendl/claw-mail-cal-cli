using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="CalendarService"/>.
/// </summary>
[Trait("Category", "Unit")]
public class CalendarServiceTests
{
	private readonly Mock<IGraphClientService> _mockGraphClientService;
	private readonly CalendarService _calendarService;

	/// <summary>
	/// Initializes a new instance of <see cref="CalendarServiceTests"/>.
	/// </summary>
	public CalendarServiceTests()
	{
		_mockGraphClientService = new Mock<IGraphClientService>();
		_calendarService = new CalendarService(_mockGraphClientService.Object);
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
