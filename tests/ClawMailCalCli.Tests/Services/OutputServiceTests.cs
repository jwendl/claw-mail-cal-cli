using System.Text.Json;
using ClawMailCalCli.Models;
using ClawMailCalCli.Services;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="OutputService"/>.
/// </summary>
[Trait("Category", "Unit")]
[Collection(NonParallelCollection.Name)]
public class OutputServiceTests
{
	private readonly OutputService _outputService;

	/// <summary>
	/// Initializes a new instance of <see cref="OutputServiceTests"/>.
	/// </summary>
	public OutputServiceTests()
	{
		_outputService = new OutputService();
	}

	[Fact]
	public void WriteJson_WithEmailSummaryList_WritesValidJsonToStdout()
	{
		// Arrange
		var emails = new List<EmailSummary>
		{
			new("user@example.com", "Hello World", DateTimeOffset.Parse("2026-03-21T14:30:00Z"), true),
			new("other@example.com", "Meeting Notes", DateTimeOffset.Parse("2026-03-20T10:00:00Z"), false),
		};

		var originalOut = Console.Out;
		using var stringWriter = new StringWriter();
		Console.SetOut(stringWriter);

		try
		{
			// Act
			_outputService.WriteJson(emails);

			// Assert
			var output = stringWriter.ToString();
			var document = JsonDocument.Parse(output);
			document.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
			document.RootElement.GetArrayLength().Should().Be(2);

			var first = document.RootElement[0];
			first.GetProperty("from").GetString().Should().Be("user@example.com");
			first.GetProperty("subject").GetString().Should().Be("Hello World");
			first.GetProperty("isRead").GetBoolean().Should().BeTrue();
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}

	[Fact]
	public void WriteJson_WithEmailMessage_WritesValidJsonToStdout()
	{
		// Arrange
		var message = new EmailMessage(
			"AAMk123",
			"Hello World",
			"sender@example.com",
			"recipient@example.com",
			DateTimeOffset.Parse("2026-03-21T14:30:00Z"),
			"This is the email body.");

		var originalOut = Console.Out;
		using var stringWriter = new StringWriter();
		Console.SetOut(stringWriter);

		try
		{
			// Act
			_outputService.WriteJson(message);

			// Assert
			var output = stringWriter.ToString();
			var document = JsonDocument.Parse(output);
			document.RootElement.ValueKind.Should().Be(JsonValueKind.Object);

			document.RootElement.GetProperty("id").GetString().Should().Be("AAMk123");
			document.RootElement.GetProperty("subject").GetString().Should().Be("Hello World");
			document.RootElement.GetProperty("from").GetString().Should().Be("sender@example.com");
			document.RootElement.GetProperty("to").GetString().Should().Be("recipient@example.com");
			document.RootElement.GetProperty("body").GetString().Should().Be("This is the email body.");
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}

	[Fact]
	public void WriteJson_WithCalendarEventSummaryList_WritesValidJsonToStdout()
	{
		// Arrange
		var events = new List<CalendarEventSummary>
		{
			new("Team Meeting", DateTimeOffset.Parse("2026-03-25T09:00:00Z"), DateTimeOffset.Parse("2026-03-25T09:30:00Z"), false, "Conference Room A"),
		};

		var originalOut = Console.Out;
		using var stringWriter = new StringWriter();
		Console.SetOut(stringWriter);

		try
		{
			// Act
			_outputService.WriteJson(events);

			// Assert
			var output = stringWriter.ToString();
			var document = JsonDocument.Parse(output);
			document.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
			document.RootElement.GetArrayLength().Should().Be(1);

			var first = document.RootElement[0];
			first.GetProperty("title").GetString().Should().Be("Team Meeting");
			first.GetProperty("isAllDay").GetBoolean().Should().BeFalse();
			first.GetProperty("location").GetString().Should().Be("Conference Room A");
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}

	[Fact]
	public void WriteJson_WithCalendarEvent_WritesValidJsonToStdout()
	{
		// Arrange
		var calendarEvent = new CalendarEvent(
			"evt-001",
			"Quarterly Review",
			"2026-03-25T09:00:00Z",
			"2026-03-25T10:00:00Z",
			"Board Room",
			"Jane Doe",
			["Alice", "Bob"],
			"Please review the Q1 numbers before joining.");

		var originalOut = Console.Out;
		using var stringWriter = new StringWriter();
		Console.SetOut(stringWriter);

		try
		{
			// Act
			_outputService.WriteJson(calendarEvent);

			// Assert
			var output = stringWriter.ToString();
			var document = JsonDocument.Parse(output);
			document.RootElement.ValueKind.Should().Be(JsonValueKind.Object);

			document.RootElement.GetProperty("id").GetString().Should().Be("evt-001");
			document.RootElement.GetProperty("subject").GetString().Should().Be("Quarterly Review");
			document.RootElement.GetProperty("location").GetString().Should().Be("Board Room");
			document.RootElement.GetProperty("organizer").GetString().Should().Be("Jane Doe");

			var attendees = document.RootElement.GetProperty("attendees");
			attendees.ValueKind.Should().Be(JsonValueKind.Array);
			attendees.GetArrayLength().Should().Be(2);
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}

	[Fact]
	public void WriteJson_UsesCamelCasePropertyNames()
	{
		// Arrange
		var summary = new EmailSummary("a@b.com", "Subject", DateTimeOffset.UtcNow, true);

		var originalOut = Console.Out;
		using var stringWriter = new StringWriter();
		Console.SetOut(stringWriter);

		try
		{
			// Act
			_outputService.WriteJson(summary);

			// Assert
			var output = stringWriter.ToString();
			output.Should().Contain("\"from\"");
			output.Should().Contain("\"subject\"");
			output.Should().Contain("\"isRead\"");
			output.Should().Contain("\"receivedDateTime\"");
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}

	[Fact]
	public void WriteError_WritesMessageToStderr()
	{
		// Arrange
		var originalError = Console.Error;
		using var stringWriter = new StringWriter();
		Console.SetError(stringWriter);

		try
		{
			// Act
			_outputService.WriteError("Something went wrong");

			// Assert
			var output = stringWriter.ToString();
			output.Should().Contain("Something went wrong");
		}
		finally
		{
			Console.SetError(originalError);
		}
	}

	[Fact]
	public void WriteError_DoesNotWriteToStdout()
	{
		// Arrange
		var originalOut = Console.Out;
		var originalError = Console.Error;
		using var stdoutWriter = new StringWriter();
		using var stderrWriter = new StringWriter();
		Console.SetOut(stdoutWriter);
		Console.SetError(stderrWriter);

		try
		{
			// Act
			_outputService.WriteError("Error message");

			// Assert
			stdoutWriter.ToString().Should().BeEmpty();
			stderrWriter.ToString().Should().Contain("Error message");
		}
		finally
		{
			Console.SetOut(originalOut);
			Console.SetError(originalError);
		}
	}

	[Fact]
	public void WriteJson_OutputIsValidJson_WithEmptyArray()
	{
		// Arrange
		var emptyList = Array.Empty<EmailSummary>();

		var originalOut = Console.Out;
		using var stringWriter = new StringWriter();
		Console.SetOut(stringWriter);

		try
		{
			// Act
			_outputService.WriteJson(emptyList);

			// Assert
			var output = stringWriter.ToString();
			var document = JsonDocument.Parse(output);
			document.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
			document.RootElement.GetArrayLength().Should().Be(0);
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}

	[Fact]
	public void WriteJson_WithNullableFields_SerializesNullsCorrectly()
	{
		// Arrange
		var calendarEvent = new CalendarEvent(
			"evt-002",
			"No Location Event",
			null,
			null,
			null,
			null,
			[],
			null);

		var originalOut = Console.Out;
		using var stringWriter = new StringWriter();
		Console.SetOut(stringWriter);

		try
		{
			// Act
			_outputService.WriteJson(calendarEvent);

			// Assert
			var output = stringWriter.ToString();
			var document = JsonDocument.Parse(output);
			document.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
			document.RootElement.GetProperty("id").GetString().Should().Be("evt-002");
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}

	[Fact]
	public void WriteJsonError_WritesJsonErrorObjectToStderr()
	{
		// Arrange
		var originalError = Console.Error;
		using var stringWriter = new StringWriter();
		Console.SetError(stringWriter);

		try
		{
			// Act
			_outputService.WriteJsonError("Something went wrong.");

			// Assert
			var output = stringWriter.ToString();
			var document = JsonDocument.Parse(output);
			document.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
			document.RootElement.GetProperty("error").GetString().Should().Be("Something went wrong.");
		}
		finally
		{
			Console.SetError(originalError);
		}
	}

	[Fact]
	public void WriteJsonError_UsesCamelCasePropertyName()
	{
		// Arrange
		var originalError = Console.Error;
		using var stringWriter = new StringWriter();
		Console.SetError(stringWriter);

		try
		{
			// Act
			_outputService.WriteJsonError("Account not found.");

			// Assert
			var output = stringWriter.ToString();
			output.Should().Contain("\"error\"");
			output.Should().NotContain("\"Error\"");
		}
		finally
		{
			Console.SetError(originalError);
		}
	}
}
