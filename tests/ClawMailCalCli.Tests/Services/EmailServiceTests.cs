using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="EmailService"/>.
/// </summary>
[Trait("Category", "Unit")]
public class EmailServiceTests
{
	private readonly Mock<IGraphClientService> _mockGraphClientService;
	private readonly ILogger<EmailService> _logger;

	/// <summary>
	/// Initializes a new instance of <see cref="EmailServiceTests"/>.
	/// </summary>
	public EmailServiceTests()
	{
		_mockGraphClientService = new Mock<IGraphClientService>();
		_logger = new NullLogger<EmailService>();
	}

	private EmailService CreateEmailService() =>
		new EmailService(_mockGraphClientService.Object, _logger);

	[Fact]
	public async Task GetEmailsAsync_WhenNoDefaultAccount_ReturnsEmptyList()
	{
		// Arrange
		_mockGraphClientService
			.Setup(service => service.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<IReadOnlyList<EmailSummary>>>>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("No default account configured."));

		var emailService = CreateEmailService();

		// Act
		var result = await emailService.GetEmailsAsync();

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetEmailsAsync_WhenAccountNotAuthenticated_ReturnsEmptyList()
	{
		// Arrange
		_mockGraphClientService
			.Setup(service => service.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<IReadOnlyList<EmailSummary>>>>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("Account 'myaccount' is not authenticated."));

		var emailService = CreateEmailService();

		// Act
		var result = await emailService.GetEmailsAsync();

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetEmailsAsync_WithNoFolderName_CallsExecuteWithRetryAsync()
	{
		// Arrange
		_mockGraphClientService
			.Setup(service => service.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<IReadOnlyList<EmailSummary>>>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var emailService = CreateEmailService();

		// Act
		await emailService.GetEmailsAsync(folderName: null);

		// Assert
		_mockGraphClientService.Verify(
			service => service.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<IReadOnlyList<EmailSummary>>>>(), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task GetEmailsAsync_WithEmptyFolderName_CallsExecuteWithRetryAsync()
	{
		// Arrange
		_mockGraphClientService
			.Setup(service => service.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<IReadOnlyList<EmailSummary>>>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var emailService = CreateEmailService();

		// Act
		await emailService.GetEmailsAsync(folderName: "   ");

		// Assert
		_mockGraphClientService.Verify(
			service => service.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<IReadOnlyList<EmailSummary>>>>(), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task GetEmailsAsync_WithFolderName_CallsExecuteWithRetryAsync()
	{
		// Arrange
		_mockGraphClientService
			.Setup(service => service.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<IReadOnlyList<EmailSummary>>>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var emailService = CreateEmailService();

		// Act
		await emailService.GetEmailsAsync(folderName: "sentitems");

		// Assert
		_mockGraphClientService.Verify(
			service => service.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<IReadOnlyList<EmailSummary>>>>(), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task GetEmailsAsync_WithInboxMessages_ReturnsEmailSummaries()
	{
		// Arrange
		var expectedEmails = new List<EmailSummary>
		{
			new("sender@example.com", "Hello World", new DateTimeOffset(2026, 3, 21, 10, 0, 0, TimeSpan.Zero), true),
			new("boss@company.com", "Meeting tomorrow", new DateTimeOffset(2026, 3, 20, 14, 30, 0, TimeSpan.Zero), false),
		};

		_mockGraphClientService
			.Setup(service => service.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<IReadOnlyList<EmailSummary>>>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedEmails);

		var emailService = CreateEmailService();

		// Act
		var result = await emailService.GetEmailsAsync();

		// Assert
		result.Should().HaveCount(2);
		result[0].From.Should().Be("sender@example.com");
		result[0].Subject.Should().Be("Hello World");
		result[0].IsRead.Should().BeTrue();
		result[1].From.Should().Be("boss@company.com");
		result[1].IsRead.Should().BeFalse();
	}

	[Fact]
	public async Task GetEmailsAsync_WithFolderMessages_ReturnsEmailSummaries()
	{
		// Arrange
		var expectedEmails = new List<EmailSummary>
		{
			new("noreply@service.com", "Your invoice", new DateTimeOffset(2026, 3, 19, 9, 0, 0, TimeSpan.Zero), true),
		};

		_mockGraphClientService
			.Setup(service => service.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<IReadOnlyList<EmailSummary>>>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedEmails);

		var emailService = CreateEmailService();

		// Act
		var result = await emailService.GetEmailsAsync(folderName: "inbox");

		// Assert
		result.Should().HaveCount(1);
		result[0].From.Should().Be("noreply@service.com");
		result[0].Subject.Should().Be("Your invoice");
	}

	[Fact]
	public async Task GetEmailsAsync_WhenFolderNotFound_ReturnsEmptyList()
	{
		// Arrange
		_mockGraphClientService
			.Setup(service => service.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<IReadOnlyList<EmailSummary>>>>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new ODataError { ResponseStatusCode = 404 });

		var emailService = CreateEmailService();

		// Act
		var result = await emailService.GetEmailsAsync(folderName: "nonexistent");

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetEmailsAsync_WhenInboxThrowsInvalidOperation_ReturnsEmptyList()
	{
		// Arrange
		_mockGraphClientService
			.Setup(service => service.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<IReadOnlyList<EmailSummary>>>>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("Account 'myaccount' has no cached authentication record."));

		var emailService = CreateEmailService();

		// Act
		var result = await emailService.GetEmailsAsync(folderName: null);

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetEmailsAsync_WithInboxEmptyList_ReturnsEmptyList()
	{
		// Arrange
		_mockGraphClientService
			.Setup(service => service.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<IReadOnlyList<EmailSummary>>>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var emailService = CreateEmailService();

		// Act
		var result = await emailService.GetEmailsAsync();

		// Assert
		result.Should().BeEmpty();
	}
}
