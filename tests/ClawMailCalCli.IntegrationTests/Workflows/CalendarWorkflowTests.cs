using Microsoft.Graph.Models;

namespace ClawMailCalCli.IntegrationTests.Workflows;

/// <summary>
/// Integration tests for the calendar command workflows.
/// Validates the full service pipeline: account management, simulated login, and calendar operations.
/// </summary>
[Trait("Category", "Integration")]
public sealed class CalendarWorkflowTests : IAsyncLifetime
{
	private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
	private readonly AccountService _accountService;

	/// <summary>
	/// Initializes a new instance of <see cref="CalendarWorkflowTests"/> with an isolated in-memory database.
	/// </summary>
	public CalendarWorkflowTests()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		_dbContextFactory = new TestDbContextFactory(options);
		_accountService = new AccountService(_dbContextFactory, new NullLogger<AccountService>());
	}

	/// <inheritdoc />
	public async Task InitializeAsync()
	{
		await using var context = await _dbContextFactory.CreateDbContextAsync();
		await context.Database.EnsureCreatedAsync();
	}

	/// <inheritdoc />
	public async Task DisposeAsync()
	{
		await using var context = await _dbContextFactory.CreateDbContextAsync();
		await context.Database.EnsureDeletedAsync();
	}

	[Fact]
	public async Task CalendarListWorkflow_AccountAddLoginCalendarList_ReturnsSeededEvents()
	{
		// Arrange — account add
		var accountAdded = await _accountService.AddAccountAsync("alice", "alice@example.com", AccountType.Personal);
		await _accountService.SetDefaultAccountAsync("alice");
		accountAdded.Should().BeTrue();

		// Arrange — simulated login (no real credentials required)
		var mockAuthenticationService = new Mock<IAuthenticationService>();
		mockAuthenticationService
			.Setup(authService => authService.AuthenticateAsync("alice", It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		await mockAuthenticationService.Object.AuthenticateAsync("alice");

		// Arrange — seeded Graph API calendar response with real Event objects for CalendarService to map
		var seedCalendarResponse = new EventCollectionResponse
		{
			Value =
			[
				new Event
				{
					Subject = "Team Meeting",
					Start = new DateTimeTimeZone { DateTime = "2025-06-01T09:00:00", TimeZone = "UTC" },
					End = new DateTimeTimeZone { DateTime = "2025-06-01T10:00:00", TimeZone = "UTC" },
					IsAllDay = false,
					Location = new Location { DisplayName = "Conference Room A" },
				},
				new Event
				{
					Subject = "Quarterly Review",
					Start = new DateTimeTimeZone { DateTime = "2025-06-03T14:00:00", TimeZone = "UTC" },
					End = new DateTimeTimeZone { DateTime = "2025-06-03T16:00:00", TimeZone = "UTC" },
					IsAllDay = false,
					Location = new Location { DisplayName = "Main Boardroom" },
				},
			],
		};

		var fakeGraphClientService = new FakeGraphClientService()
			.Seed<EventCollectionResponse?>(seedCalendarResponse);

		var calendarService = new CalendarService(
			Mock.Of<ICalendarGraphService>(),
			fakeGraphClientService,
			new NullLogger<CalendarService>());

		// Act
		var result = await calendarService.GetUpcomingEventsAsync();

		// Assert
		result.Should().NotBeNull();
		result!.Should().HaveCount(2);
		result[0].Title.Should().Be("Team Meeting");
		result[0].Location.Should().Be("Conference Room A");
		result[1].Title.Should().Be("Quarterly Review");
		result[1].Location.Should().Be("Main Boardroom");
		mockAuthenticationService.Verify(
			authService => authService.AuthenticateAsync("alice", It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task CalendarCreateWorkflow_AccountAddLoginCalendarCreate_ReturnsCreatedEventId()
	{
		// Arrange — account add
		await _accountService.AddAccountAsync("bob", "bob@example.com", AccountType.Work);
		await _accountService.SetDefaultAccountAsync("bob");

		// Arrange — simulated login
		var mockAuthenticationService = new Mock<IAuthenticationService>();
		mockAuthenticationService
			.Setup(authService => authService.AuthenticateAsync("bob", It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		await mockAuthenticationService.Object.AuthenticateAsync("bob");

		// Arrange — fake graph client that returns a new Event with a Graph-assigned ID
		var createdGraphEvent = new Event { Id = "created-event-id-abc123" };
		var fakeGraphClientService = new FakeGraphClientService()
			.Seed<Event?>(createdGraphEvent);

		var calendarService = new CalendarService(
			Mock.Of<ICalendarGraphService>(),
			fakeGraphClientService,
			new NullLogger<CalendarService>());

		var startDateTime = new DateTimeOffset(2025, 6, 15, 14, 0, 0, TimeSpan.Zero);
		var endDateTime = new DateTimeOffset(2025, 6, 15, 15, 0, 0, TimeSpan.Zero);

		// Act
		var result = await calendarService.CreateEventAsync("Sprint Planning", startDateTime, endDateTime, "Agenda: sprint goals");

		// Assert
		result.Should().NotBeNull();
		result.Should().Be("created-event-id-abc123");
		mockAuthenticationService.Verify(
			authService => authService.AuthenticateAsync("bob", It.IsAny<CancellationToken>()),
			Times.Once);
	}
}
