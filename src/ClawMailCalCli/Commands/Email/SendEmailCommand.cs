using ClawMailCalCli.Commands.Settings;
using ClawMailCalCli.Services;

namespace ClawMailCalCli.Commands.Email;

/// <summary>
/// Sends an email to a recipient using the default authenticated account.
/// Usage: <c>claw-mail-cal-cli email send &lt;to&gt; &lt;subject&gt; &lt;content&gt;</c>
/// </summary>
internal sealed class SendEmailCommand(IEmailService emailService)
	: AsyncCommand<SendEmailSettings>
{
	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, SendEmailSettings settings, CancellationToken cancellationToken)
	{
		var sent = await emailService.SendEmailAsync(settings.To, settings.Subject, settings.Content, cancellationToken);
		if (!sent)
		{
			return 1;
		}

		AnsiConsole.MarkupLine($"[green]✓[/] Email sent to {Markup.Escape(settings.To)}");
		return 0;
	}
}
