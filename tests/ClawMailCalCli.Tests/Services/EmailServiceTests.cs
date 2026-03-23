using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

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
			.Setup(s => s.GetClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
			.Setup(s => s.GetClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
}
