using System.Text.Json;
using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Services;

/// <summary>
/// Default implementation of <see cref="IOutputService"/> that writes tables via
/// <see cref="AnsiConsole"/> and JSON to <see cref="Console.Out"/>.
/// </summary>
internal sealed class OutputService
	: IOutputService
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	/// <inheritdoc />
	public void WriteTable(Table table)
	{
		AnsiConsole.Write(table);
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
}
