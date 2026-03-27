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

		outputService.WriteMarkup("Checking environment...");
		outputService.WriteLine();

		var allPassed = true;
		foreach (var result in results)
		{
			if (result.Passed)
			{
				outputService.WriteSuccess($"{Markup.Escape(result.CheckName)} ({Markup.Escape(result.Message)})");
			}
			else
			{
				allPassed = false;
				outputService.WriteMarkup($"[red]✗[/] {Markup.Escape(result.CheckName)}: {Markup.Escape(result.Message)}");
				if (result.FixHint is not null)
				{
					outputService.WriteMarkup($"  [grey]Fix: {Markup.Escape(result.FixHint)}[/]");
				}
			}
		}

		outputService.WriteLine();
		if (allPassed)
		{
			outputService.WriteMarkup("[green]All checks passed.[/]");
		}
		else
		{
			outputService.WriteMarkup("[red]One or more checks failed. See fix hints above.[/]");
		}

		return allPassed ? 0 : 1;
	}
}
