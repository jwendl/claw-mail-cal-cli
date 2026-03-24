using ClawMailCalCli.Services;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ProcessRunner"/>.
/// </summary>
[Trait("Category", "Unit")]
public class ProcessRunnerTests
{
	private readonly ProcessRunner _processRunner;

	public ProcessRunnerTests()
	{
		_processRunner = new ProcessRunner();
	}

	[Fact]
	public async Task RunAsync_WhenCommandSucceeds_ReturnsZeroExitCode()
	{
		// Arrange & Act
		var result = await _processRunner.RunAsync("echo", "hello");

		// Assert
		result.ExitCode.Should().Be(0);
	}

	[Fact]
	public async Task RunAsync_WhenCommandSucceeds_ReturnsOutputInStandardOutput()
	{
		// Arrange & Act
		var result = await _processRunner.RunAsync("echo", "hello");

		// Assert
		result.StandardOutput.Should().Contain("hello");
	}

	[Fact]
	public async Task RunAsync_WhenCommandSucceeds_ReturnsEmptyStandardError()
	{
		// Arrange & Act
		var result = await _processRunner.RunAsync("echo", "hello");

		// Assert
		result.StandardError.Should().BeEmpty();
	}

	[Fact]
	public async Task RunAsync_WhenCommandDoesNotExist_ReturnsNegativeExitCode()
	{
		// Arrange & Act
		var result = await _processRunner.RunAsync("this-command-does-not-exist-anywhere", "");

		// Assert
		result.ExitCode.Should().Be(-1);
	}

	[Fact]
	public async Task RunAsync_WhenCommandDoesNotExist_ReturnsErrorMessage()
	{
		// Arrange & Act
		var result = await _processRunner.RunAsync("this-command-does-not-exist-anywhere", "");

		// Assert
		result.StandardError.Should().NotBeEmpty();
	}

	[Fact]
	public async Task RunAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
	{
		// Arrange
		using var cancellationTokenSource = new CancellationTokenSource();
		cancellationTokenSource.Cancel();

		// Act
		var act = async () => await _processRunner.RunAsync("echo", "hello", cancellationTokenSource.Token);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}
}
