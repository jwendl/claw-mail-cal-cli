using ClawMailCalCli.Models;
using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Commands.Email;

/// <summary>
/// Reads a single email message by subject or Graph message ID.
/// Usage: <c>claw-mail-cal-cli email read &lt;subject-or-id&gt; [--account &lt;name&gt;]</c>
/// </summary>
internal sealed class ReadEmailCommand(IEmailService emailService, IAccountService accountService, IOutputService outputService)
	: AsyncCommand<ReadEmailSettings>
{
	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, ReadEmailSettings settings, CancellationToken cancellationToken)
	{
		var accountName = settings.AccountName;

		if (!string.IsNullOrWhiteSpace(accountName))
		{
			var account = await accountService.GetAccountAsync(accountName, cancellationToken);
			if (account is null)
			{
				outputService.WriteError($"Error: Account '{accountName}' does not exist.");
				return 1;
			}
		}
		else
		{
			var defaultAccount = await accountService.GetDefaultAccountAsync(cancellationToken);
			if (defaultAccount is null)
			{
				outputService.WriteError("Error: No account specified. Use --account to specify one or set a default with 'account set'.");
				return 1;
			}

			accountName = defaultAccount.Name;
		}

		EmailMessage? message;

		try
		{
			message = await emailService.ReadEmailAsync(accountName, settings.SubjectOrId, cancellationToken);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception exception)
		{
			outputService.WriteError($"Error: Failed to read email — {exception.Message}");
			return 1;
		}

		if (message is null)
		{
			outputService.WriteError($"No message found matching: {settings.SubjectOrId}");
			return 1;
		}

		if (settings.Json)
		{
			outputService.WriteJson(message);
			return 0;
		}

		var receivedDisplay = message.ReceivedDateTime.HasValue
			? message.ReceivedDateTime.Value.ToLocalTime().ToString("f")
			: "Unknown";

		AnsiConsole.MarkupLine($"[bold]From:[/]    {Markup.Escape(message.From)}");
		AnsiConsole.MarkupLine($"[bold]To:[/]      {Markup.Escape(message.To)}");
		AnsiConsole.MarkupLine($"[bold]Subject:[/] {Markup.Escape(message.Subject)}");
		AnsiConsole.MarkupLine($"[bold]Date:[/]    {receivedDisplay}");
		AnsiConsole.WriteLine();
		AnsiConsole.WriteLine(message.Body);

		return 0;
	}
}
