using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
using Microsoft.Extensions.Logging;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="EmailService"/>.
/// </summary>
[Trait("Category", "Unit")]
public class EmailServiceTests
{
	private readonly Mock<IAccountService> _mockAccountService;
	private readonly Mock<IGraphClientService> _mockGraphClientService;
	private readonly ILogger<EmailService> _logger;

	/// <summary>
	/// Initializes a new instance of <see cref="EmailServiceTests"/>.
	/// </summary>
	public EmailServiceTests()
	{
		_mockAccountService = new Mock<IAccountService>();
		_mockGraphClientService = new Mock<IGraphClientService>();
		_logger = new NullLogger<EmailService>();
	}

	private EmailService CreateEmailService() =>
		new EmailService(_mockAccountService.Object, _mockGraphClientService.Object, _logger);

	[Fact]
	public async Task GetEmailsAsync_WhenNoDefaultAccount_ReturnsEmptyList()
	{
		// Arrange
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync((Account?)null);

		var emailService = CreateEmailService();

		// Act
		var result = await emailService.GetEmailsAsync();

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetEmailsAsync_WhenNoDefaultAccount_DoesNotCallGraphClientService()
	{
		// Arrange
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync((Account?)null);

		var emailService = CreateEmailService();

		// Act
		await emailService.GetEmailsAsync();

		// Assert
		_mockGraphClientService.Verify(
			graphClientService => graphClientService.GetInboxMessagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
			Times.Never);

		_mockGraphClientService.Verify(
			graphClientService => graphClientService.GetFolderMessagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task GetEmailsAsync_WithNoFolderName_CallsGetInboxMessages()
	{
		// Arrange
		var defaultAccount = new Account("myaccount", "user@example.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(defaultAccount);

		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetInboxMessagesAsync("myaccount", 20, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var emailService = CreateEmailService();

		// Act
		await emailService.GetEmailsAsync(folderName: null);

		// Assert
		_mockGraphClientService.Verify(
			graphClientService => graphClientService.GetInboxMessagesAsync("myaccount", 20, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task GetEmailsAsync_WithEmptyFolderName_CallsGetInboxMessages()
	{
		// Arrange
		var defaultAccount = new Account("myaccount", "user@example.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(defaultAccount);

		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetInboxMessagesAsync("myaccount", 20, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var emailService = CreateEmailService();

		// Act
		await emailService.GetEmailsAsync(folderName: "   ");

		// Assert
		_mockGraphClientService.Verify(
			graphClientService => graphClientService.GetInboxMessagesAsync("myaccount", 20, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task GetEmailsAsync_WithFolderName_CallsGetFolderMessages()
	{
		// Arrange
		var defaultAccount = new Account("myaccount", "user@example.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(defaultAccount);

		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetFolderMessagesAsync("myaccount", "sentitems", 20, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var emailService = CreateEmailService();

		// Act
		await emailService.GetEmailsAsync(folderName: "sentitems");

		// Assert
		_mockGraphClientService.Verify(
			graphClientService => graphClientService.GetFolderMessagesAsync("myaccount", "sentitems", 20, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task GetEmailsAsync_WithNoFolderName_DoesNotCallGetFolderMessages()
	{
		// Arrange
		var defaultAccount = new Account("myaccount", "user@example.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(defaultAccount);

		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetInboxMessagesAsync("myaccount", 20, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var emailService = CreateEmailService();

		// Act
		await emailService.GetEmailsAsync(folderName: null);

		// Assert
		_mockGraphClientService.Verify(
			graphClientService => graphClientService.GetFolderMessagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task GetEmailsAsync_WithFolderName_DoesNotCallGetInboxMessages()
	{
		// Arrange
		var defaultAccount = new Account("myaccount", "user@example.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(defaultAccount);

		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetFolderMessagesAsync("myaccount", "drafts", 20, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var emailService = CreateEmailService();

		// Act
		await emailService.GetEmailsAsync(folderName: "drafts");

		// Assert
		_mockGraphClientService.Verify(
			graphClientService => graphClientService.GetInboxMessagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task GetEmailsAsync_WithInboxMessages_ReturnsEmailSummaries()
	{
		// Arrange
		var defaultAccount = new Account("myaccount", "user@example.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(defaultAccount);

		var expectedEmails = new List<EmailSummary>
		{
			new("sender@example.com", "Hello World", new DateTimeOffset(2026, 3, 21, 10, 0, 0, TimeSpan.Zero), true),
			new("boss@company.com", "Meeting tomorrow", new DateTimeOffset(2026, 3, 20, 14, 30, 0, TimeSpan.Zero), false),
		};

		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetInboxMessagesAsync("myaccount", 20, It.IsAny<CancellationToken>()))
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
		var defaultAccount = new Account("myaccount", "user@example.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(defaultAccount);

		var expectedEmails = new List<EmailSummary>
		{
			new("noreply@service.com", "Your invoice", new DateTimeOffset(2026, 3, 19, 9, 0, 0, TimeSpan.Zero), true),
		};

		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetFolderMessagesAsync("myaccount", "inbox", 20, It.IsAny<CancellationToken>()))
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
		var defaultAccount = new Account("myaccount", "user@example.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(defaultAccount);

		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetFolderMessagesAsync("myaccount", "nonexistent", 20, It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("Folder 'nonexistent' was not found."));

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
		var defaultAccount = new Account("myaccount", "user@example.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(defaultAccount);

		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetInboxMessagesAsync("myaccount", 20, It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("Account 'myaccount' has no cached authentication record. Please run 'login myaccount' first."));

		var emailService = CreateEmailService();

		// Act
		var result = await emailService.GetEmailsAsync(folderName: null);

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetEmailsAsync_WithDefaultAccount_UsesAccountName()
	{
		// Arrange
		var defaultAccount = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(defaultAccount);

		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetInboxMessagesAsync("work-account", 20, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var emailService = CreateEmailService();

		// Act
		await emailService.GetEmailsAsync();

		// Assert
		_mockGraphClientService.Verify(
			graphClientService => graphClientService.GetInboxMessagesAsync("work-account", 20, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task GetEmailsAsync_WithInboxEmptyList_ReturnsEmptyList()
	{
		// Arrange
		var defaultAccount = new Account("myaccount", "user@example.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(defaultAccount);

		_mockGraphClientService
			.Setup(graphClientService => graphClientService.GetInboxMessagesAsync("myaccount", 20, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var emailService = CreateEmailService();

		// Act
		var result = await emailService.GetEmailsAsync();

		// Assert
		result.Should().BeEmpty();
	}
}
