using Microsoft.Extensions.Logging;

namespace ClawMailCalCli.Logging;

/// <summary>
/// An <see cref="ILogger"/> implementation that writes log messages to stderr
/// using the format <c>[DBG] message</c>, <c>[WRN] message</c>, etc.
/// </summary>
internal sealed class StderrLogger(string categoryName, LogLevel minimumLevel)
	: ILogger
{
	/// <inheritdoc />
	public IDisposable? BeginScope<TState>(TState state)
		where TState : notnull => NullScope.Instance;

	/// <inheritdoc />
	public bool IsEnabled(LogLevel logLevel) =>
		logLevel != LogLevel.None && logLevel >= minimumLevel;

	/// <inheritdoc />
	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		if (!IsEnabled(logLevel))
		{
			return;
		}

		var levelPrefix = logLevel switch
		{
			LogLevel.Trace => "[TRC]",
			LogLevel.Debug => "[DBG]",
			LogLevel.Information => "[INF]",
			LogLevel.Warning => "[WRN]",
			LogLevel.Error => "[ERR]",
			LogLevel.Critical => "[CRT]",
			_ => "[???]",
		};

		var shortCategory = categoryName.Contains('.')
			? categoryName[(categoryName.LastIndexOf('.') + 1)..]
			: categoryName;

		try
		{
			var message = formatter(state, exception);
			Console.Error.WriteLine($"{levelPrefix} [{shortCategory}] {message}");

			if (exception is not null)
			{
				Console.Error.WriteLine(exception.ToString());
			}
		}
		catch (Exception)
		{
			// Swallow all exceptions from logging to avoid crashing the application.
		}
	}

	private sealed class NullScope
		: IDisposable
	{
		public static NullScope Instance { get; } = new();

		public void Dispose() { }
	}
}
