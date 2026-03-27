using System.Text.Json;
using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Services;

/// <summary>
/// Default implementation of <see cref="IOutputService"/> that writes tables via
/// <see cref="IAnsiConsole"/> and JSON to <see cref="Console.Out"/>.
/// </summary>
internal sealed class OutputService(IAnsiConsole? ansiConsole = null)
	: IOutputService
{
	private readonly IAnsiConsole _console = ansiConsole ?? AnsiConsole.Console;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	/// <inheritdoc />
	public void WriteTable(Table table)
	{
		_console.Write(table);
	}

	/// <inheritdoc />
	public void WriteJson<T>(T data)
	{
		var json = JsonSerializer.Serialize(data, JsonOptions);
		Console.Out.WriteLine(json);
	}

	/// <inheritdoc />
	public void WriteError(string message)
	{
		Console.Error.WriteLine(message);
	}

	/// <inheritdoc />
	public void WriteSuccess(string message)
	{
		_console.MarkupLine($"[green]✓[/] {message}");
	}

	/// <inheritdoc />
	public void WriteWarning(string message)
	{
		_console.MarkupLine($"[yellow]{Markup.Escape(message)}[/]");
	}

	/// <inheritdoc />
	public void WriteMarkup(string markup)
	{
		_console.MarkupLine(markup);
	}

	/// <inheritdoc />
	public void WriteLine()
	{
		_console.WriteLine();
	}
}
