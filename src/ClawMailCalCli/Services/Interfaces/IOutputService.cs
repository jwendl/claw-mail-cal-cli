namespace ClawMailCalCli.Services.Interfaces;

/// <summary>
/// Provides formatted and structured output capabilities for commands.
/// </summary>
internal interface IOutputService
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

	/// <summary>
	/// Serializes a structured JSON error object to standard error.
	/// Use this method instead of <see cref="WriteError"/> when <c>--json</c> mode is active so that
	/// callers can programmatically detect the failure reason.
	/// </summary>
	/// <param name="message">A human-readable description of the error.</param>
	/// <param name="code">
	/// A machine-readable error code string.  Use one of the constants defined in
	/// <see cref="ClawMailCalCli.Models.ErrorCodes"/>.
	/// </param>
	void WriteJsonError(string message, string code);
}
