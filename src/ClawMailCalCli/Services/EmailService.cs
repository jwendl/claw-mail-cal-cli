using Microsoft.Graph;
using Microsoft.Graph.Me.SendMail;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace ClawMailCalCli.Services;

/// <summary>
/// Sends emails via Microsoft Graph using the default authenticated account.
/// </summary>
public class EmailService(IGraphClientService graphClientService, ILogger<EmailService> logger)
	: IEmailService
{
	/// <inheritdoc />
	public async Task<bool> SendEmailAsync(string to, string subject, string content, CancellationToken cancellationToken = default)
	{
		var graphClient = await graphClientService.GetClientForDefaultAccountAsync(cancellationToken);
		if (graphClient is null)
		{
			AnsiConsole.MarkupLine("[red]✗[/] Failed to send email: no default account configured or account not authenticated. Run 'account set <name>' and 'login <name>' first.");
			return false;
		}

		try
		{
			await SendViaGraphClientAsync(graphClient, to, subject, content, cancellationToken);
			return true;
		}
		catch (ODataError oDataError)
		{
			var reason = oDataError.Error?.Message ?? "unknown Graph API error";
			AnsiConsole.MarkupLine($"[red]✗[/] Failed to send email: {Markup.Escape(reason)}");
			if (logger.IsEnabled(LogLevel.Error))
			{
				logger.LogError(oDataError, "Graph API error sending email to '{To}'.", to);
			}

			return false;
		}
		catch (Exception exception)
		{
			AnsiConsole.MarkupLine($"[red]✗[/] Failed to send email: {Markup.Escape(exception.Message)}");
			if (logger.IsEnabled(LogLevel.Error))
			{
				logger.LogError(exception, "Unexpected error sending email to '{To}'.", to);
			}

			return false;
		}
	}

	/// <summary>
	/// Performs the actual Microsoft Graph API call to send the email.
	/// Virtual to allow test overrides without requiring full Graph SDK mocking.
	/// </summary>
	protected virtual async Task SendViaGraphClientAsync(GraphServiceClient graphClient, string to, string subject, string content, CancellationToken cancellationToken)
	{
		await graphClient.Me.SendMail.PostAsync(new SendMailPostRequestBody
		{
			Message = new Message
			{
				Subject = subject,
				Body = new ItemBody { ContentType = BodyType.Text, Content = content },
				ToRecipients =
				[
					new Recipient { EmailAddress = new EmailAddress { Address = to } },
				],
			},
		}, cancellationToken: cancellationToken);
	}
}
