using Microsoft.Extensions.Logging;

namespace ClawMailCalCli.Logging;

/// <summary>
/// An <see cref="ILoggerProvider"/> that creates <see cref="StderrLogger"/> instances
/// writing structured log output to stderr.
/// </summary>
public sealed class StderrLoggerProvider(LogLevel minimumLevel)
	: ILoggerProvider
{
	/// <inheritdoc />
	public ILogger CreateLogger(string categoryName) =>
		new StderrLogger(categoryName, minimumLevel);

	/// <inheritdoc />
	public void Dispose() { }
}
