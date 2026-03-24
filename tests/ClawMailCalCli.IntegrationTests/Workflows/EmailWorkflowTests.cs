using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;

namespace ClawMailCalCli.IntegrationTests.Workflows;

/// <summary>
/// Integration tests for the email command workflows.
/// Validates the full service pipeline: account management, simulated login, and email operations.
/// </summary>
[Trait("Category", "Integration")]
public sealed class EmailWorkflowTests : IAsyncLifetime
{
	private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
	private readonly AccountService _accountService;

	/// <summary>
	/// Initializes a new instance of <see cref="EmailWorkflowTests"/> with an isolated in-memory database.
	/// </summary>
	public EmailWorkflowTests()
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
	public async Task EmailListWorkflow_AccountAddLoginEmailList_ReturnsSeededEmails()
	{
		// Arrange — account add
		var accountAdded = await _accountService.AddAccountAsync("alice", "alice@example.com", AccountType.Personal);
		await _accountService.SetDefaultAccountAsync("alice");
		accountAdded.Should().BeTrue();

		// Arrange — seeded email list (login is a prerequisite handled by the real GraphClientService;
		// here the FakeGraphClientService stands in for the authenticated Graph layer)
		IReadOnlyList<EmailSummary> seededEmails =
		[
			new EmailSummary("sender@example.com", "Hello World", DateTimeOffset.UtcNow, false),
			new EmailSummary("boss@example.com", "Meeting Tomorrow", DateTimeOffset.UtcNow.AddHours(-2), true),
		];

		var fakeGraphClientService = new FakeGraphClientService()
			.Seed<IReadOnlyList<EmailSummary>>(seededEmails);

		var emailService = new EmailService(fakeGraphClientService, new NullLogger<EmailService>());

		// Act
		var result = await emailService.GetEmailsAsync();

		// Assert
		result.Should().HaveCount(2);
		result[0].Subject.Should().Be("Hello World");
		result[1].Subject.Should().Be("Meeting Tomorrow");
	}

	[Fact]
	public async Task EmailReadWorkflow_AccountAddLoginEmailReadBySubject_ReturnsSeededEmail()
	{
		// Arrange — account add
		await _accountService.AddAccountAsync("bob", "bob@example.com", AccountType.Work);
		await _accountService.SetDefaultAccountAsync("bob");

		// Arrange — seeded email message (login is a prerequisite handled by the real GraphClientService;
		// here the FakeGraphClientService stands in for the authenticated Graph layer)
		var seededEmailMessage = new EmailMessage(
			Id: "msg-001",
			Subject: "Project Update",
			From: "manager@example.com",
			To: "bob@example.com",
			ReceivedDateTime: DateTimeOffset.UtcNow.AddHours(-1),
			Body: "The project is on track.");

		var fakeGraphClientService = new FakeGraphClientService()
			.Seed<EmailMessage?>(seededEmailMessage);

		var emailService = new EmailService(fakeGraphClientService, new NullLogger<EmailService>());

		// Act
		var result = await emailService.ReadEmailAsync("bob", "Project Update");

		// Assert
		result.Should().NotBeNull();
		result!.Subject.Should().Be("Project Update");
		result.From.Should().Be("manager@example.com");
		result.Body.Should().Be("The project is on track.");
	}

	[Fact]
	public async Task EmailSendWorkflow_AccountAddLoginEmailSend_ReturnsTrue()
	{
		// Arrange — account add
		await _accountService.AddAccountAsync("alice", "alice@example.com", AccountType.Personal);
		await _accountService.SetDefaultAccountAsync("alice");

		// Arrange — fake graph client that returns a successful send result
		// (login is a prerequisite handled by the real GraphClientService;
		// here the FakeGraphClientService stands in for the authenticated Graph layer)
		var fakeGraphClientService = new FakeGraphClientService()
			.Seed<bool>(true);

		var emailService = new EmailService(fakeGraphClientService, new NullLogger<EmailService>());

		// Act
		var result = await emailService.SendEmailAsync("recipient@example.com", "Test Subject", "Test Body");

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public async Task EmailListWorkflow_WhenGraphClientReturns401_TriggersReAuthentication()
	{
		// Arrange — add account via real AccountService backed by in-memory database
		await _accountService.AddAccountAsync("work-user", "work@contoso.com", AccountType.Work);
		await _accountService.SetDefaultAccountAsync("work-user");

		// Arrange — mock authentication service that records calls
		var mockAuthenticationService = new Mock<IAuthenticationService>();
		mockAuthenticationService
			.Setup(authService => authService.AuthenticateAsync("work-user", It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		// Arrange — mock GraphServiceClientBuilder that returns a usable fake client on every call
		var mockGraphServiceClientBuilder = new Mock<IGraphServiceClientBuilder>();
		var fakeGraphClient = BuildFakeGraphServiceClient();
		mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeGraphClient);

		// Arrange — real GraphClientService using the real AccountService for account resolution
		var graphClientService = new GraphClientService(
			_accountService,
			mockGraphServiceClientBuilder.Object,
			mockAuthenticationService.Object,
			new NullLogger<GraphClientService>());

		// The operation throws 401 on the first call and succeeds on the second (simulating token expiry)
		var callCount = 0;
		Func<GraphServiceClient, Task<string>> operation = _ =>
		{
			callCount++;
			if (callCount == 1)
			{
				throw new ODataError { ResponseStatusCode = 401 };
			}

			return Task.FromResult("retry-success");
		};

		// Act
		var result = await graphClientService.ExecuteWithRetryAsync(operation);

		// Assert
		result.Should().Be("retry-success");
		callCount.Should().Be(2, because: "the operation is called once initially and once after re-authentication");
		mockAuthenticationService.Verify(
			authService => authService.AuthenticateAsync("work-user", It.IsAny<CancellationToken>()),
			Times.Once);
	}

	/// <summary>
	/// Builds a <see cref="GraphServiceClient"/> backed by a mock request adapter for use in tests
	/// where the client is passed to an operation but no real Graph API calls are made.
	/// </summary>
	private static GraphServiceClient BuildFakeGraphServiceClient()
	{
		var mockRequestAdapter = new Mock<IRequestAdapter>();
		mockRequestAdapter.SetupGet(adapter => adapter.BaseUrl).Returns("https://graph.microsoft.com/v1.0");
		return new GraphServiceClient(mockRequestAdapter.Object);
	}
}
