using ClawMailCalCli.Services.Interfaces;

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

		outputService.WriteMarkup($"[bold]From:[/]    {Markup.Escape(message.From)}");
		outputService.WriteMarkup($"[bold]To:[/]      {Markup.Escape(message.To)}");
		outputService.WriteMarkup($"[bold]Subject:[/] {Markup.Escape(message.Subject)}");
		outputService.WriteMarkup($"[bold]Date:[/]    {receivedDisplay}");
		outputService.WriteLine();
		outputService.WriteMarkup(Markup.Escape(message.Body));

		return 0;
	}
}
