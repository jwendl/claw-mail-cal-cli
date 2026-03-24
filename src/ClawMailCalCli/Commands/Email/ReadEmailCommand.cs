using ClawMailCalCli.Commands.Settings;
using ClawMailCalCli.Services;

namespace ClawMailCalCli.Commands.Email;

/// <summary>
/// Reads a single email message by subject or Graph message ID.
/// Usage: <c>claw-mail-cal-cli email read <account-name> <subject-or-id></c>
/// </summary>
internal sealed class ReadEmailCommand(IEmailService emailService, IOutputService outputService)
	: AsyncCommand<ReadEmailSettings>
{
	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, ReadEmailSettings settings, CancellationToken cancellationToken)
	{
		var message = await emailService.ReadEmailAsync(settings.AccountName, settings.SubjectOrId, cancellationToken);

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
