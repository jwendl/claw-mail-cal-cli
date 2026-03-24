using ClawMailCalCli.Logging;
using Microsoft.Extensions.Logging;

namespace ClawMailCalCli.Tests.Logging;

/// <summary>
/// Unit tests for <see cref="StderrLoggerProvider"/> and <see cref="StderrLogger"/>.
/// </summary>
[Trait("Category", "Unit")]
public class StderrLoggerProviderTests
{
	[Fact]
	public void CreateLogger_ReturnsSomeLogger()
	{
		// Arrange
		using var stderrLoggerProvider = new StderrLoggerProvider(LogLevel.Debug);

		// Act
		var logger = stderrLoggerProvider.CreateLogger("TestCategory");

		// Assert
		logger.Should().NotBeNull();
	}

	[Theory]
	[InlineData(LogLevel.Debug, LogLevel.Debug, true)]
	[InlineData(LogLevel.Debug, LogLevel.Warning, true)]
	[InlineData(LogLevel.Warning, LogLevel.Warning, true)]
	[InlineData(LogLevel.Warning, LogLevel.Debug, false)]
	[InlineData(LogLevel.Error, LogLevel.Error, true)]
	[InlineData(LogLevel.Error, LogLevel.Warning, false)]
	[InlineData(LogLevel.Debug, LogLevel.None, false)]
	public void IsEnabled_WithVariousLevels_ReturnsExpected(LogLevel minimumLevel, LogLevel queryLevel, bool expected)
	{
		// Arrange
		using var stderrLoggerProvider = new StderrLoggerProvider(minimumLevel);
		var logger = stderrLoggerProvider.CreateLogger("TestCategory");

		// Act
		var result = logger.IsEnabled(queryLevel);

		// Assert
		result.Should().Be(expected);
	}

	[Fact]
	public void IsEnabled_WhenMinimumLevelIsDebug_EnablesAllLogLevels()
	{
		// Arrange
		using var stderrLoggerProvider = new StderrLoggerProvider(LogLevel.Debug);
		var logger = stderrLoggerProvider.CreateLogger("TestCategory");

		// Act & Assert
		logger.IsEnabled(LogLevel.Debug).Should().BeTrue();
		logger.IsEnabled(LogLevel.Information).Should().BeTrue();
		logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
		logger.IsEnabled(LogLevel.Error).Should().BeTrue();
		logger.IsEnabled(LogLevel.Critical).Should().BeTrue();
	}

	[Fact]
	public void IsEnabled_WhenMinimumLevelIsError_DisablesWarningAndBelow()
	{
		// Arrange
		using var stderrLoggerProvider = new StderrLoggerProvider(LogLevel.Error);
		var logger = stderrLoggerProvider.CreateLogger("TestCategory");

		// Act & Assert
		logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
		logger.IsEnabled(LogLevel.Information).Should().BeFalse();
		logger.IsEnabled(LogLevel.Warning).Should().BeFalse();
		logger.IsEnabled(LogLevel.Error).Should().BeTrue();
		logger.IsEnabled(LogLevel.Critical).Should().BeTrue();
	}

	[Fact]
	public void BeginScope_ReturnsNonNullDisposable()
	{
		// Arrange
		using var stderrLoggerProvider = new StderrLoggerProvider(LogLevel.Debug);
		var logger = stderrLoggerProvider.CreateLogger("TestCategory");

		// Act
		var scope = logger.BeginScope("some-state");

		// Assert
		scope.Should().NotBeNull();
		scope!.Dispose(); // Should not throw
	}

	[Fact]
	public void Log_WhenLevelBelowMinimum_DoesNotThrow()
	{
		// Arrange
		using var stderrLoggerProvider = new StderrLoggerProvider(LogLevel.Error);
		var logger = stderrLoggerProvider.CreateLogger("TestCategory");

		// Act
		var act = () => logger.Log(LogLevel.Debug, "This should be suppressed");

		// Assert — should not throw even when level is below minimum
		act.Should().NotThrow();
	}

	[Fact]
	public void Log_WhenLevelAtMinimum_DoesNotThrow()
	{
		// Arrange
		using var stderrLoggerProvider = new StderrLoggerProvider(LogLevel.Warning);
		var logger = stderrLoggerProvider.CreateLogger("TestCategory");

		// Act
		var act = () => logger.Log(LogLevel.Warning, "Warning message");

		// Assert
		act.Should().NotThrow();
	}

	[Fact]
	public void Log_WhenExceptionProvided_DoesNotThrow()
	{
		// Arrange
		using var stderrLoggerProvider = new StderrLoggerProvider(LogLevel.Error);
		var logger = stderrLoggerProvider.CreateLogger("TestCategory");
		var exception = new InvalidOperationException("test exception");

		// Act
		var act = () => logger.Log(LogLevel.Error, exception, "Error occurred");

		// Assert
		act.Should().NotThrow();
	}

	[Fact]
	public void Dispose_DoesNotThrow()
	{
		// Arrange
		var stderrLoggerProvider = new StderrLoggerProvider(LogLevel.Debug);

		// Act
		var act = () => stderrLoggerProvider.Dispose();

		// Assert
		act.Should().NotThrow();
	}
}
