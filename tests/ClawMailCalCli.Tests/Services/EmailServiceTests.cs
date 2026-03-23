using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="EmailService"/>.
/// </summary>
[Trait("Category", "Unit")]
public class EmailServiceTests
{
	[Theory]
	[InlineData("AAMkAGVmMDEzMTM4LTZmYWUtNDdkNC1hMDZjLWM2Y2I0MThlYjAyNwBGAAAAAAD=", true)]
	[InlineData("AAAA=", true)]
	[InlineData("x=y", true)]
	[InlineData("a-very-long-string-that-is-one-hundred-characters-or-more-to-trigger-the-message-id-heuristic-here!!", true)]
	[InlineData("Meeting notes", false)]
	[InlineData("Hello world", false)]
	[InlineData("short", false)]
	public void LooksLikeMessageId_WithInput_ReturnsExpected(string input, bool expected)
	{
		// Act
		var result = EmailService.LooksLikeMessageId(input);

		// Assert
		result.Should().Be(expected);
	}

	[Fact]
	public void StripHtml_WithHtmlContent_ReturnsPlainText()
	{
		// Arrange
		const string htmlContent = "<html><body><p>Hello, <b>World</b>!</p></body></html>";

		// Act
		var result = EmailService.StripHtml(htmlContent);

		// Assert
		result.Should().Contain("Hello, World!");
		result.Should().NotContain("<");
		result.Should().NotContain(">");
	}

	[Fact]
	public void StripHtml_WithNestedTags_ReturnsDecodedText()
	{
		// Arrange
		const string htmlContent = "<p>Hello &amp; goodbye &lt;world&gt;</p>";

		// Act
		var result = EmailService.StripHtml(htmlContent);

		// Assert
		result.Should().Contain("Hello & goodbye <world>");
	}

	[Fact]
	public void StripHtml_WithEmptyString_ReturnsEmpty()
	{
		// Act
		var result = EmailService.StripHtml(string.Empty);

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public void StripHtml_WithNullLikeWhitespace_ReturnsEmpty()
	{
		// Act
		var result = EmailService.StripHtml("   ");

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public void StripHtml_WithPlainText_ReturnsSameText()
	{
		// Arrange
		const string plainText = "This is plain text without any HTML tags.";

		// Act
		var result = EmailService.StripHtml(plainText);

		// Assert
		result.Should().Be(plainText);
	}

	[Fact]
	public async Task ReadEmailAsync_WithMessageId_QueriesById()
	{
		// Arrange
		var messageId = "AAAA=";
		var mockGraphClientService = new Mock<IGraphClientService>();

		// EmailService depends on IGraphClientService; since GraphServiceClient is not
		// easily mockable, we verify the behavior by confirming that LooksLikeMessageId
		// returns true for message-ID-shaped input (tested above) and that the correct
		// exception propagates when the Graph client is not available.
		var emailService = new EmailService(mockGraphClientService.Object, Mock.Of<ILogger<EmailService>>());
		mockGraphClientService
			.Setup(s => s.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<EmailMessage?>>>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("No account found."));

		// Act
		var act = async () => await emailService.ReadEmailAsync("myaccount", messageId);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("No account found.");
	}

	[Fact]
	public async Task ReadEmailAsync_WhenGraphClientThrows_PropagatesException()
	{
		// Arrange
		var mockGraphClientService = new Mock<IGraphClientService>();
		var emailService = new EmailService(mockGraphClientService.Object, Mock.Of<ILogger<EmailService>>());
		mockGraphClientService
			.Setup(s => s.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<EmailMessage?>>>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("Account not found."));

		// Act
		var act = async () => await emailService.ReadEmailAsync("nonexistent", "Hello world");

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>();
	}

	[Theory]
	[InlineData("contains('=', subject)", true)]   // has = sign
	[InlineData("a_very_long_id_that_has_over_one_hundred_characters_to_trigger_the_heuristic_in_LooksLikeMessageId_xxxxxxxxx", true)]
	[InlineData("short subject without equals", false)]
	public void LooksLikeMessageId_EdgeCases_ReturnsExpected(string input, bool expected)
	{
		// Act
		var result = EmailService.LooksLikeMessageId(input);

		// Assert
		result.Should().Be(expected);
	}

	[Fact]
	public async Task ReadEmailAsync_ById_ReturnsEmailMessage()
	{
		// Arrange
		var messageId = "AAAA=";
		var testMessage = new Message
		{
			Id = messageId,
			Subject = "Test Subject",
			From = new Recipient { EmailAddress = new EmailAddress { Address = "from@example.com" } },
			ToRecipients = [new Recipient { EmailAddress = new EmailAddress { Address = "to@example.com" } }],
			ReceivedDateTime = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero),
			Body = new ItemBody { ContentType = BodyType.Text, Content = "Test body content" },
		};

		var mockAdapter = new Mock<IRequestAdapter>();
		mockAdapter.SetupGet(a => a.BaseUrl).Returns("https://graph.microsoft.com/v1.0");
		mockAdapter.Setup(a => a.SendAsync<Message>(
			It.IsAny<RequestInformation>(),
			It.IsAny<ParsableFactory<Message>>(),
			It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(testMessage);
		var graphClient = new GraphServiceClient(mockAdapter.Object);
		var mockGraphClientService = new Mock<IGraphClientService>();
		mockGraphClientService
			.Setup(s => s.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<EmailMessage?>>>(), It.IsAny<CancellationToken>()))
			.Returns<Func<GraphServiceClient, Task<EmailMessage?>>, CancellationToken>((operation, cancellationToken) => operation(graphClient));
		var emailService = new EmailService(mockGraphClientService.Object, Mock.Of<ILogger<EmailService>>());

		// Act
		var result = await emailService.ReadEmailAsync("myaccount", messageId);

		// Assert
		result.Should().NotBeNull();
		result!.Id.Should().Be(messageId);
		result.Subject.Should().Be("Test Subject");
		result.From.Should().Be("from@example.com");
		result.To.Should().Be("to@example.com");
		result.Body.Should().Be("Test body content");
	}

	[Fact]
	public async Task ReadEmailAsync_ById_WhenNotFound_ReturnsNull()
	{
		// Arrange
		var messageId = "AAAA=";
		var mockAdapter = new Mock<IRequestAdapter>();
		mockAdapter.SetupGet(a => a.BaseUrl).Returns("https://graph.microsoft.com/v1.0");
		mockAdapter.Setup(a => a.SendAsync<Message>(
			It.IsAny<RequestInformation>(),
			It.IsAny<ParsableFactory<Message>>(),
			It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
			It.IsAny<CancellationToken>()))
			.ThrowsAsync(new ODataError { ResponseStatusCode = 404 });
		var graphClient = new GraphServiceClient(mockAdapter.Object);
		var mockGraphClientService = new Mock<IGraphClientService>();
		mockGraphClientService
			.Setup(s => s.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<EmailMessage?>>>(), It.IsAny<CancellationToken>()))
			.Returns<Func<GraphServiceClient, Task<EmailMessage?>>, CancellationToken>((operation, cancellationToken) => operation(graphClient));
		var emailService = new EmailService(mockGraphClientService.Object, Mock.Of<ILogger<EmailService>>());

		// Act
		var result = await emailService.ReadEmailAsync("myaccount", messageId);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task ReadEmailAsync_BySubject_ReturnsEmailMessage()
	{
		// Arrange
		const string subject = "Meeting notes";
		var testMessage = new Message
		{
			Id = "some-message-id",
			Subject = subject,
			From = new Recipient { EmailAddress = new EmailAddress { Address = "from@example.com" } },
			ToRecipients = [new Recipient { EmailAddress = new EmailAddress { Address = "to@example.com" } }],
			ReceivedDateTime = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero),
			Body = new ItemBody { ContentType = BodyType.Text, Content = "Meeting agenda..." },
		};

		var mockAdapter = new Mock<IRequestAdapter>();
		mockAdapter.SetupGet(a => a.BaseUrl).Returns("https://graph.microsoft.com/v1.0");
		mockAdapter.Setup(a => a.SendAsync<MessageCollectionResponse>(
			It.IsAny<RequestInformation>(),
			It.IsAny<ParsableFactory<MessageCollectionResponse>>(),
			It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(new MessageCollectionResponse { Value = [testMessage] });
		var graphClient = new GraphServiceClient(mockAdapter.Object);
		var mockGraphClientService = new Mock<IGraphClientService>();
		mockGraphClientService
			.Setup(s => s.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<EmailMessage?>>>(), It.IsAny<CancellationToken>()))
			.Returns<Func<GraphServiceClient, Task<EmailMessage?>>, CancellationToken>((operation, cancellationToken) => operation(graphClient));
		var emailService = new EmailService(mockGraphClientService.Object, Mock.Of<ILogger<EmailService>>());

		// Act
		var result = await emailService.ReadEmailAsync("myaccount", subject);

		// Assert
		result.Should().NotBeNull();
		result!.Subject.Should().Be(subject);
		result.From.Should().Be("from@example.com");
		result.Body.Should().Be("Meeting agenda...");
	}

	[Fact]
	public async Task ReadEmailAsync_BySubject_WithNoResults_ReturnsNull()
	{
		// Arrange
		var mockAdapter = new Mock<IRequestAdapter>();
		mockAdapter.SetupGet(a => a.BaseUrl).Returns("https://graph.microsoft.com/v1.0");
		mockAdapter.Setup(a => a.SendAsync<MessageCollectionResponse>(
			It.IsAny<RequestInformation>(),
			It.IsAny<ParsableFactory<MessageCollectionResponse>>(),
			It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(new MessageCollectionResponse { Value = [] });
		var graphClient = new GraphServiceClient(mockAdapter.Object);
		var mockGraphClientService = new Mock<IGraphClientService>();
		mockGraphClientService
			.Setup(s => s.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<EmailMessage?>>>(), It.IsAny<CancellationToken>()))
			.Returns<Func<GraphServiceClient, Task<EmailMessage?>>, CancellationToken>((operation, cancellationToken) => operation(graphClient));
		var emailService = new EmailService(mockGraphClientService.Object, Mock.Of<ILogger<EmailService>>());

		// Act
		var result = await emailService.ReadEmailAsync("myaccount", "nonexistent subject");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task ReadEmailAsync_WithHtmlBody_StripsHtmlInResult()
	{
		// Arrange
		var messageId = "AAAA=";
		var testMessage = new Message
		{
			Id = messageId,
			Subject = "HTML Email",
			From = new Recipient { EmailAddress = new EmailAddress { Address = "sender@example.com" } },
			ToRecipients = [],
			ReceivedDateTime = DateTimeOffset.UtcNow,
			Body = new ItemBody { ContentType = BodyType.Html, Content = "<p>Hello <b>World</b></p>" },
		};

		var mockAdapter = new Mock<IRequestAdapter>();
		mockAdapter.SetupGet(a => a.BaseUrl).Returns("https://graph.microsoft.com/v1.0");
		mockAdapter.Setup(a => a.SendAsync<Message>(
			It.IsAny<RequestInformation>(),
			It.IsAny<ParsableFactory<Message>>(),
			It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(testMessage);
		var graphClient = new GraphServiceClient(mockAdapter.Object);
		var mockGraphClientService = new Mock<IGraphClientService>();
		mockGraphClientService
			.Setup(s => s.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<EmailMessage?>>>(), It.IsAny<CancellationToken>()))
			.Returns<Func<GraphServiceClient, Task<EmailMessage?>>, CancellationToken>((operation, cancellationToken) => operation(graphClient));
		var emailService = new EmailService(mockGraphClientService.Object, Mock.Of<ILogger<EmailService>>());

		// Act
		var result = await emailService.ReadEmailAsync("myaccount", messageId);

		// Assert
		result.Should().NotBeNull();
		result!.Body.Should().Contain("Hello World");
		result.Body.Should().NotContain("<p>");
		result.Body.Should().NotContain("<b>");
	}

	[Fact]
	public async Task ReadEmailAsync_WithEmptyBodyAndBodyPreview_FallsBackToPreview()
	{
		// Arrange
		var messageId = "AAAA=";
		var testMessage = new Message
		{
			Id = messageId,
			Subject = "Preview Email",
			From = new Recipient { EmailAddress = new EmailAddress { Address = "sender@example.com" } },
			ToRecipients = [],
			ReceivedDateTime = DateTimeOffset.UtcNow,
			Body = new ItemBody { ContentType = BodyType.Text, Content = "" },
			BodyPreview = "This is the preview text",
		};

		var mockAdapter = new Mock<IRequestAdapter>();
		mockAdapter.SetupGet(a => a.BaseUrl).Returns("https://graph.microsoft.com/v1.0");
		mockAdapter.Setup(a => a.SendAsync<Message>(
			It.IsAny<RequestInformation>(),
			It.IsAny<ParsableFactory<Message>>(),
			It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(testMessage);
		var graphClient = new GraphServiceClient(mockAdapter.Object);
		var mockGraphClientService = new Mock<IGraphClientService>();
		mockGraphClientService
			.Setup(s => s.ExecuteWithRetryAsync(It.IsAny<Func<GraphServiceClient, Task<EmailMessage?>>>(), It.IsAny<CancellationToken>()))
			.Returns<Func<GraphServiceClient, Task<EmailMessage?>>, CancellationToken>((operation, cancellationToken) => operation(graphClient));
		var emailService = new EmailService(mockGraphClientService.Object, Mock.Of<ILogger<EmailService>>());

		// Act
		var result = await emailService.ReadEmailAsync("myaccount", messageId);

		// Assert
		result.Should().NotBeNull();
		result!.Body.Should().Be("This is the preview text");
	}
}
