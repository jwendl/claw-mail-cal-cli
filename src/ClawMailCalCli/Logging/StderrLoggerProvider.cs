namespace ClawMailCalCli.Logging;

/// <summary>
/// An <see cref="ILoggerProvider"/> that creates <see cref="StderrLogger"/> instances
/// writing structured, colour-coded log output to stderr via <see cref="IAnsiConsole"/>.
/// </summary>
public sealed class StderrLoggerProvider(LogLevel minimumLevel)
	: ILoggerProvider
{
	private readonly IAnsiConsole _stderrAnsiConsole = AnsiConsole.Create(new AnsiConsoleSettings
	{
		Out = new AnsiConsoleOutput(Console.Error),
	});

	/// <inheritdoc />
	public ILogger CreateLogger(string categoryName) =>
		new StderrLogger(categoryName, minimumLevel, _stderrAnsiConsole);

	/// <inheritdoc />
	public void Dispose() { }
}
