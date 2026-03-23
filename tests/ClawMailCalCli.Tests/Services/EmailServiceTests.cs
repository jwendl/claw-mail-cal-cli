using ClawMailCalCli.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;

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
	/// Initializes test dependencies.
	/// </summary>
	public EmailServiceTests()
	{
		_mockGraphClientService = new Mock<IGraphClientService>();
		_logger = NullLogger<EmailService>.Instance;
	}

	[Fact]
	public async Task SendEmailAsync_WhenNoDefaultAccount_ReturnsFalse()
	{
		// Arrange
		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetClientForDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync((GraphServiceClient?)null);

		var emailService = new TestableEmailService(_mockGraphClientService.Object, _logger);

		// Act
		var result = await emailService.SendEmailAsync("to@example.com", "Subject", "Body");

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task SendEmailAsync_WhenNoDefaultAccount_DoesNotCallSendViaGraph()
	{
		// Arrange
		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetClientForDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync((GraphServiceClient?)null);

		var emailService = new TestableEmailService(_mockGraphClientService.Object, _logger);

		// Act
		await emailService.SendEmailAsync("to@example.com", "Subject", "Body");

		// Assert
		emailService.SendViaGraphCallCount.Should().Be(0);
	}

	[Fact]
	public async Task SendEmailAsync_WhenGraphCallSucceeds_ReturnsTrue()
	{
		// Arrange
		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetClientForDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateFakeGraphClient());

		var emailService = new TestableEmailService(_mockGraphClientService.Object, _logger, sendOverride: (_, _, _) => Task.CompletedTask);

		// Act
		var result = await emailService.SendEmailAsync("to@example.com", "Subject", "Body");

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public async Task SendEmailAsync_WhenGraphCallSucceeds_InvokesSendOnce()
	{
		// Arrange
		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetClientForDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateFakeGraphClient());

		var emailService = new TestableEmailService(_mockGraphClientService.Object, _logger, sendOverride: (_, _, _) => Task.CompletedTask);

		// Act
		await emailService.SendEmailAsync("to@example.com", "Subject", "Body");

		// Assert
		emailService.SendViaGraphCallCount.Should().Be(1);
	}

	[Fact]
	public async Task SendEmailAsync_WhenODataErrorThrown_ReturnsFalse()
	{
		// Arrange
		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetClientForDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateFakeGraphClient());

		var oDataError = new ODataError { Error = new MainError { Message = "Mailbox unavailable" } };
		var emailService = new TestableEmailService(_mockGraphClientService.Object, _logger, sendOverride: (_, _, _) => throw oDataError);

		// Act
		var result = await emailService.SendEmailAsync("to@example.com", "Subject", "Body");

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task SendEmailAsync_WhenGenericExceptionThrown_ReturnsFalse()
	{
		// Arrange
		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetClientForDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateFakeGraphClient());

		var emailService = new TestableEmailService(_mockGraphClientService.Object, _logger, sendOverride: (_, _, _) => throw new InvalidOperationException("Network error"));

		// Act
		var result = await emailService.SendEmailAsync("to@example.com", "Subject", "Body");

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task SendEmailAsync_ForwardsRecipientToGraphCall()
	{
		// Arrange
		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetClientForDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateFakeGraphClient());

		string? capturedTo = null;
		var emailService = new TestableEmailService(_mockGraphClientService.Object, _logger, sendOverride: (to, _, _) =>
		{
			capturedTo = to;
			return Task.CompletedTask;
		});

		// Act
		await emailService.SendEmailAsync("recipient@example.com", "Subject", "Body");

		// Assert
		capturedTo.Should().Be("recipient@example.com");
	}

	[Fact]
	public async Task SendEmailAsync_ForwardsSubjectToGraphCall()
	{
		// Arrange
		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetClientForDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateFakeGraphClient());

		string? capturedSubject = null;
		var emailService = new TestableEmailService(_mockGraphClientService.Object, _logger, sendOverride: (_, subject, _) =>
		{
			capturedSubject = subject;
			return Task.CompletedTask;
		});

		// Act
		await emailService.SendEmailAsync("recipient@example.com", "Hello World", "Body");

		// Assert
		capturedSubject.Should().Be("Hello World");
	}

	[Fact]
	public async Task SendEmailAsync_ForwardsContentToGraphCall()
	{
		// Arrange
		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetClientForDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateFakeGraphClient());

		string? capturedContent = null;
		var emailService = new TestableEmailService(_mockGraphClientService.Object, _logger, sendOverride: (_, _, content) =>
		{
			capturedContent = content;
			return Task.CompletedTask;
		});

		// Act
		await emailService.SendEmailAsync("recipient@example.com", "Subject", "This is the body.");

		// Assert
		capturedContent.Should().Be("This is the body.");
	}

	/// <summary>
	/// Creates a minimal <see cref="GraphServiceClient"/> backed by a mock adapter.
	/// The adapter is never called because <see cref="TestableEmailService"/> overrides
	/// <c>SendViaGraphClientAsync</c> before any Graph SDK code runs.
	/// </summary>
	private static GraphServiceClient CreateFakeGraphClient()
	{
		var mockAdapter = new Mock<IRequestAdapter>();
		mockAdapter.SetupProperty(adapter => adapter.BaseUrl, "https://graph.microsoft.com/v1.0");
		return new GraphServiceClient(mockAdapter.Object);
	}

	/// <summary>
	/// A testable subclass of <see cref="EmailService"/> that replaces the actual Graph API
	/// call with a controllable <paramref name="sendOverride"/> delegate.
	/// </summary>
	private sealed class TestableEmailService(IGraphClientService graphClientService, ILogger<EmailService> logger, Func<string, string, string, Task>? sendOverride = null)
		: EmailService(graphClientService, logger)
	{
		public int SendViaGraphCallCount { get; private set; }

		protected override Task SendViaGraphClientAsync(GraphServiceClient graphClient, string to, string subject, string content, CancellationToken cancellationToken)
		{
			SendViaGraphCallCount++;
			return sendOverride is not null
				? sendOverride(to, subject, content)
				: Task.CompletedTask;
		}
	}
}
