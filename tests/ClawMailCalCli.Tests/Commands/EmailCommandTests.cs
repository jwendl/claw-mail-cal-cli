using System.Reflection;
using ClawMailCalCli.Commands.Email;
using ClawMailCalCli.Commands.Settings;
using ClawMailCalCli.Models;
using ClawMailCalCli.Services.Interfaces;
using Spectre.Console.Cli;

namespace ClawMailCalCli.Tests.Commands;

/// <summary>
/// Unit tests for email-related commands.
/// </summary>
[Trait("Category", "Unit")]
public class EmailCommandTests
{
	private readonly Mock<IEmailService> _mockEmailService;
	private readonly Mock<IAccountService> _mockAccountService;
	private readonly Mock<IOutputService> _mockOutputService;

	public EmailCommandTests()
	{
		_mockEmailService = new Mock<IEmailService>();
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
			args: [Array.Empty<string>(), remainingArguments, "email", null],
			culture: null)!;
	}

	[Fact]
	public async Task ListEmailCommand_WhenEmailsRetrieved_ReturnsZero()
	{
		// Arrange
		IReadOnlyList<EmailSummary> emails =
		[
			new EmailSummary("sender@example.com", "Hello", DateTimeOffset.UtcNow, true),
			new EmailSummary("other@example.com", "World", DateTimeOffset.UtcNow, false),
		];

		_mockEmailService
			.Setup(service => service.GetEmailsAsync(null, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(emails);

		var command = new ListEmailCommand(_mockEmailService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var settings = new ListEmailSettings();
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
	}

	[Fact]
	public async Task ListEmailCommand_WhenNoEmails_ReturnsZero()
	{
		// Arrange
		_mockEmailService
			.Setup(service => service.GetEmailsAsync(null, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var command = new ListEmailCommand(_mockEmailService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var settings = new ListEmailSettings();
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
	}

	[Fact]
	public async Task ListEmailCommand_WithAccountFlag_PassesAccountNameToService()
	{
		// Arrange
		var accountName = "work-account";
		var account = new Account(accountName, "user@contoso.com", AccountType.Work);

		_mockAccountService
			.Setup(service => service.GetAccountAsync(accountName, It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockEmailService
			.Setup(service => service.GetEmailsAsync(null, accountName, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var command = new ListEmailCommand(_mockEmailService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var settings = new ListEmailSettings { AccountName = accountName };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
		_mockEmailService.Verify(service => service.GetEmailsAsync(null, accountName, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ListEmailCommand_WithNonExistentAccount_ReturnsOne()
	{
		// Arrange
		var accountName = "nonexistent-account";

		_mockAccountService
			.Setup(service => service.GetAccountAsync(accountName, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Account?)null);

		var command = new ListEmailCommand(_mockEmailService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var settings = new ListEmailSettings { AccountName = accountName };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
		_mockEmailService.Verify(service => service.GetEmailsAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task ReadEmailCommand_WhenMessageFound_ReturnsZero()
	{
		// Arrange
		var emailMessage = new EmailMessage(
			Id: "msg-id-1",
			Subject: "Test Subject",
			From: "sender@example.com",
			To: "recipient@example.com",
			ReceivedDateTime: DateTimeOffset.UtcNow,
			Body: "Test body content");

		var defaultAccount = new Account("default-account", "default@example.com", AccountType.Personal);

		_mockAccountService
			.Setup(service => service.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(defaultAccount);

		_mockEmailService
			.Setup(service => service.ReadEmailAsync("default-account", "Test Subject", It.IsAny<CancellationToken>()))
			.ReturnsAsync(emailMessage);

		var command = new ReadEmailCommand(_mockEmailService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var settings = new ReadEmailSettings { SubjectOrId = "Test Subject" };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
	}

	[Fact]
	public async Task ReadEmailCommand_WithAccountFlag_UsesSpecifiedAccount()
	{
		// Arrange
		var accountName = "work-account";
		var account = new Account(accountName, "user@contoso.com", AccountType.Work);

		var emailMessage = new EmailMessage(
			Id: "msg-id-1",
			Subject: "Test Subject",
			From: "sender@example.com",
			To: "recipient@example.com",
			ReceivedDateTime: DateTimeOffset.UtcNow,
			Body: "Test body content");

		_mockAccountService
			.Setup(service => service.GetAccountAsync(accountName, It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockEmailService
			.Setup(service => service.ReadEmailAsync(accountName, "Test Subject", It.IsAny<CancellationToken>()))
			.ReturnsAsync(emailMessage);

		var command = new ReadEmailCommand(_mockEmailService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var settings = new ReadEmailSettings { SubjectOrId = "Test Subject", AccountName = accountName };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
		_mockEmailService.Verify(service => service.ReadEmailAsync(accountName, "Test Subject", It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ReadEmailCommand_WhenMessageNotFound_ReturnsOne()
	{
		// Arrange
		var defaultAccount = new Account("default-account", "default@example.com", AccountType.Personal);

		_mockAccountService
			.Setup(service => service.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(defaultAccount);

		_mockEmailService
			.Setup(service => service.ReadEmailAsync("default-account", "nonexistent", It.IsAny<CancellationToken>()))
			.ReturnsAsync((EmailMessage?)null);

		var command = new ReadEmailCommand(_mockEmailService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var settings = new ReadEmailSettings { SubjectOrId = "nonexistent" };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
	}

	[Fact]
	public async Task ReadEmailCommand_WhenNoDefaultAccount_ReturnsOne()
	{
		// Arrange
		_mockAccountService
			.Setup(service => service.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync((Account?)null);

		var command = new ReadEmailCommand(_mockEmailService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var settings = new ReadEmailSettings { SubjectOrId = "Test Subject" };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
		_mockEmailService.Verify(service => service.ReadEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task ReadEmailCommand_WithNonExistentAccount_ReturnsOne()
	{
		// Arrange
		var accountName = "nonexistent-account";

		_mockAccountService
			.Setup(service => service.GetAccountAsync(accountName, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Account?)null);

		var command = new ReadEmailCommand(_mockEmailService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var settings = new ReadEmailSettings { SubjectOrId = "Test Subject", AccountName = accountName };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
		_mockEmailService.Verify(service => service.ReadEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task ReadEmailCommand_WhenMessageFoundWithNullReceivedDateTime_ReturnsZero()
	{
		// Arrange
		var emailMessage = new EmailMessage(
			Id: "msg-id-2",
			Subject: "No Date",
			From: "sender@example.com",
			To: "recipient@example.com",
			ReceivedDateTime: null,
			Body: "Body without date");

		var defaultAccount = new Account("default-account", "default@example.com", AccountType.Personal);

		_mockAccountService
			.Setup(service => service.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(defaultAccount);

		_mockEmailService
			.Setup(service => service.ReadEmailAsync("default-account", "No Date", It.IsAny<CancellationToken>()))
			.ReturnsAsync(emailMessage);

		var command = new ReadEmailCommand(_mockEmailService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var settings = new ReadEmailSettings { SubjectOrId = "No Date" };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
	}

	[Fact]
	public async Task SendEmailCommand_WhenEmailSentSuccessfully_ReturnsZero()
	{
		// Arrange
		_mockEmailService
			.Setup(service => service.SendEmailAsync("to@example.com", "Subject", "Content", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var command = new SendEmailCommand(_mockEmailService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var settings = new SendEmailSettings { To = "to@example.com", Subject = "Subject", Content = "Content" };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
	}

	[Fact]
	public async Task SendEmailCommand_WhenEmailSendFails_ReturnsOne()
	{
		// Arrange
		_mockEmailService
			.Setup(service => service.SendEmailAsync("to@example.com", "Subject", "Content", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		var command = new SendEmailCommand(_mockEmailService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var settings = new SendEmailSettings { To = "to@example.com", Subject = "Subject", Content = "Content" };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
	}

	[Fact]
	public async Task SendEmailCommand_WithAccountFlag_PassesAccountNameToService()
	{
		// Arrange
		var accountName = "work-account";
		var account = new Account(accountName, "user@contoso.com", AccountType.Work);

		_mockAccountService
			.Setup(service => service.GetAccountAsync(accountName, It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockEmailService
			.Setup(service => service.SendEmailAsync("to@example.com", "Subject", "Content", accountName, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var command = new SendEmailCommand(_mockEmailService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var settings = new SendEmailSettings { To = "to@example.com", Subject = "Subject", Content = "Content", AccountName = accountName };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
		_mockEmailService.Verify(service => service.SendEmailAsync("to@example.com", "Subject", "Content", accountName, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task SendEmailCommand_WithNonExistentAccount_ReturnsOne()
	{
		// Arrange
		var accountName = "nonexistent-account";

		_mockAccountService
			.Setup(service => service.GetAccountAsync(accountName, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Account?)null);

		var command = new SendEmailCommand(_mockEmailService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var settings = new SendEmailSettings { To = "to@example.com", Subject = "Subject", Content = "Content", AccountName = accountName };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
		_mockEmailService.Verify(service => service.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task SendEmailCommand_WhenJsonAndSuccess_WritesJsonResult()
	{
		// Arrange
		_mockEmailService
			.Setup(service => service.SendEmailAsync("to@example.com", "Subject", "Content", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var command = new SendEmailCommand(_mockEmailService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var settings = new SendEmailSettings { To = "to@example.com", Subject = "Subject", Content = "Content", Json = true };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
		_mockOutputService.Verify(service => service.WriteJson(It.Is<CommandResult>(commandResult => commandResult.Success)), Times.Once);
	}

	[Fact]
	public async Task SendEmailCommand_WhenJsonAndFailure_WritesJsonError()
	{
		// Arrange
		_mockEmailService
			.Setup(service => service.SendEmailAsync("to@example.com", "Subject", "Content", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		var command = new SendEmailCommand(_mockEmailService.Object, _mockAccountService.Object, _mockOutputService.Object);
		var settings = new SendEmailSettings { To = "to@example.com", Subject = "Subject", Content = "Content", Json = true };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
		_mockOutputService.Verify(service => service.WriteJsonError(It.IsAny<string>()), Times.Once);
	}
}
