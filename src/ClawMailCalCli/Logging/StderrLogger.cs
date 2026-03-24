using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace ClawMailCalCli.Logging;

/// <summary>
/// An <see cref="ILogger"/> implementation that writes colour-coded log messages to stderr
/// via <see cref="IAnsiConsole"/>, using the format <c>[DBG]</c>, <c>[WRN]</c>, <c>[ERR]</c>, etc.
/// </summary>
internal sealed class StderrLogger(string categoryName, LogLevel minimumLevel, IAnsiConsole ansiConsole)
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

		var levelMarkup = logLevel switch
		{
			LogLevel.Trace => "[grey][[TRC]][/]",
			LogLevel.Debug => "[grey][[DBG]][/]",
			LogLevel.Information => "[blue][[INF]][/]",
			LogLevel.Warning => "[yellow][[WRN]][/]",
			LogLevel.Error => "[red][[ERR]][/]",
			LogLevel.Critical => "[bold red][[CRT]][/]",
			_ => "[grey][[???]][/]",
		};

		var shortCategory = categoryName.Contains('.')
			? categoryName[(categoryName.LastIndexOf('.') + 1)..]
			: categoryName;

		try
		{
			var message = formatter(state, exception);
			ansiConsole.MarkupLine($"{levelMarkup} [{Markup.Escape(shortCategory)}] {Markup.Escape(message)}");

			if (exception is not null)
			{
				ansiConsole.MarkupLine($"[grey]{Markup.Escape(exception.ToString())}[/]");
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
