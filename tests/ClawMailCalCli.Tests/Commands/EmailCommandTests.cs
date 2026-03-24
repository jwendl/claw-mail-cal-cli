using System.Reflection;
using ClawMailCalCli.Commands.Email;
using ClawMailCalCli.Commands.Settings;
using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
using Spectre.Console.Cli;

namespace ClawMailCalCli.Tests.Commands;

/// <summary>
/// Unit tests for email-related commands.
/// </summary>
[Trait("Category", "Unit")]
public class EmailCommandTests
{
	private readonly Mock<IEmailService> _mockEmailService;
	private readonly Mock<IOutputService> _mockOutputService;

	public EmailCommandTests()
	{
		_mockEmailService = new Mock<IEmailService>();
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
			.Setup(service => service.GetEmailsAsync(null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(emails);

		var command = new ListEmailCommand(_mockEmailService.Object, _mockOutputService.Object);
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
			.Setup(service => service.GetEmailsAsync(null, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var command = new ListEmailCommand(_mockEmailService.Object, _mockOutputService.Object);
		var settings = new ListEmailSettings();
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
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

		_mockEmailService
			.Setup(service => service.ReadEmailAsync("work-account", "Test Subject", It.IsAny<CancellationToken>()))
			.ReturnsAsync(emailMessage);

		var command = new ReadEmailCommand(_mockEmailService.Object, _mockOutputService.Object);
		var settings = new ReadEmailSettings { AccountName = "work-account", SubjectOrId = "Test Subject" };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
	}

	[Fact]
	public async Task ReadEmailCommand_WhenMessageNotFound_ReturnsOne()
	{
		// Arrange
		_mockEmailService
			.Setup(service => service.ReadEmailAsync("work-account", "nonexistent", It.IsAny<CancellationToken>()))
			.ReturnsAsync((EmailMessage?)null);

		var command = new ReadEmailCommand(_mockEmailService.Object, _mockOutputService.Object);
		var settings = new ReadEmailSettings { AccountName = "work-account", SubjectOrId = "nonexistent" };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
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

		_mockEmailService
			.Setup(service => service.ReadEmailAsync("work-account", "No Date", It.IsAny<CancellationToken>()))
			.ReturnsAsync(emailMessage);

		var command = new ReadEmailCommand(_mockEmailService.Object, _mockOutputService.Object);
		var settings = new ReadEmailSettings { AccountName = "work-account", SubjectOrId = "No Date" };
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
			.Setup(service => service.SendEmailAsync("to@example.com", "Subject", "Content", It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var command = new SendEmailCommand(_mockEmailService.Object);
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
			.Setup(service => service.SendEmailAsync("to@example.com", "Subject", "Content", It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		var command = new SendEmailCommand(_mockEmailService.Object);
		var settings = new SendEmailSettings { To = "to@example.com", Subject = "Subject", Content = "Content" };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
	}
}
