using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Commands;

/// <summary>
/// Checks the developer environment for required prerequisites and reports
/// a pass/fail status for each check.
/// </summary>
internal sealed class DoctorCommand(IDoctorService doctorService, IOutputService outputService)
	: AsyncCommand<DoctorCommand.Settings>
{
	/// <summary>
	/// Settings for the <see cref="DoctorCommand"/>. No additional arguments are required.
	/// </summary>
	internal sealed class Settings
		: JsonOutputSettings
	{
	}

	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
	{
		var results = await doctorService.RunAllChecksAsync(cancellationToken);

		if (settings.Json)
		{
			outputService.WriteJson(results);
			return results.All(checkResult => checkResult.Passed) ? 0 : 1;
		}

		AnsiConsole.MarkupLine("Checking environment...");
		AnsiConsole.WriteLine();

		var allPassed = true;
		foreach (var result in results)
		{
			if (result.Passed)
			{
				AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(result.CheckName)} ({Markup.Escape(result.Message)})");
			}
			else
			{
				allPassed = false;
				AnsiConsole.MarkupLine($"[red]✗[/] {Markup.Escape(result.CheckName)}: {Markup.Escape(result.Message)}");
				if (result.FixHint is not null)
				{
					AnsiConsole.MarkupLine($"  [grey]Fix: {Markup.Escape(result.FixHint)}[/]");
				}
			}
		}

		AnsiConsole.WriteLine();
		if (allPassed)
		{
			AnsiConsole.MarkupLine("[green]All checks passed.[/]");
		}
		else
		{
			AnsiConsole.MarkupLine("[red]One or more checks failed. See fix hints above.[/]");
		}

		return allPassed ? 0 : 1;
	}
}
