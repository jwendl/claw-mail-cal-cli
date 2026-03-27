namespace ClawMailCalCli.Services.Interfaces;

/// <summary>
/// Provides formatted and structured output capabilities for commands.
/// </summary>
public interface IOutputService
{
	/// <summary>Writes a formatted <see cref="Table"/> to the console using AnsiConsole.</summary>
	/// <param name="table">The table to render.</param>
	void WriteTable(Table table);

	/// <summary>Serializes <paramref name="data"/> to JSON and writes it to standard output.</summary>
	/// <typeparam name="T">The type of data to serialize.</typeparam>
	/// <param name="data">The data to serialize.</param>
	void WriteJson<T>(T data);

	/// <summary>Writes a diagnostic message (errors, warnings, or other non-fatal diagnostics) to standard error.</summary>
	/// <param name="message">The plain-text diagnostic message.</param>
	void WriteError(string message);

	/// <summary>Writes a success message prefixed with a green check mark (<c>[green]✓[/]</c>).</summary>
	/// <param name="message">The success message markup string (dynamic content must be escaped by the caller).</param>
	void WriteSuccess(string message);

	/// <summary>Writes a plain-text warning message rendered in yellow.</summary>
	/// <param name="message">The plain-text warning message.</param>
	void WriteWarning(string message);

	/// <summary>Writes an arbitrary Spectre Console markup line to the console.</summary>
	/// <param name="markup">The Spectre Console markup string to write.</param>
	void WriteMarkup(string markup);

	/// <summary>Writes a blank line to the console.</summary>
	void WriteLine();

	/// <summary>
	/// Serializes an error response as <c>{ "error": "..." }</c> and writes it to standard error.
	/// Use this instead of <see cref="WriteError"/> when the caller has requested JSON output.
	/// </summary>
	/// <param name="message">The error message to include in the JSON payload.</param>
	void WriteJsonError(string message);
}
