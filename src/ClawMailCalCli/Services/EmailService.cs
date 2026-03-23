using ClawMailCalCli.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models.ODataErrors;

namespace ClawMailCalCli.Services;

/// <summary>
/// Retrieves email summaries for the current default account via <see cref="IGraphClientService"/>.
/// </summary>
public class EmailService(IGraphClientService graphClientService, ILogger<EmailService> logger)
	: IEmailService
{
	private const int DefaultMessageCount = 20;

	private static readonly string[] MessageSelectFields =
	[
		"id",
		"subject",
		"from",
		"receivedDateTime",
		"isRead",
	];

	/// <inheritdoc />
	public async Task<IReadOnlyList<EmailSummary>> GetEmailsAsync(string? folderName = null, CancellationToken cancellationToken = default)
	{
		var normalizedFolderName = folderName?.Trim();

		try
		{
			return await graphClientService.ExecuteWithRetryAsync(async graphClient =>
			{
				Microsoft.Graph.Models.MessageCollectionResponse? messageCollection;

				if (string.IsNullOrWhiteSpace(normalizedFolderName))
				{
					messageCollection = await graphClient.Me.Messages.GetAsync(config =>
					{
						config.QueryParameters.Top = DefaultMessageCount;
						config.QueryParameters.Select = MessageSelectFields;
						config.QueryParameters.Orderby = ["receivedDateTime desc"];
					}, cancellationToken);
				}
				else
				{
					messageCollection = await graphClient.Me.MailFolders[normalizedFolderName].Messages.GetAsync(config =>
					{
						config.QueryParameters.Top = DefaultMessageCount;
						config.QueryParameters.Select = MessageSelectFields;
						config.QueryParameters.Orderby = ["receivedDateTime desc"];
					}, cancellationToken);
				}

				return MapMessages(messageCollection?.Value);
			}, cancellationToken);
		}
		catch (InvalidOperationException invalidOperationException)
		{
			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogDebug(invalidOperationException, "Unable to retrieve emails.");
			}

			return [];
		}
		catch (ODataError odataError) when (odataError.ResponseStatusCode == 404)
		{
			AnsiConsole.MarkupLine($"[red]Error:[/] Folder '[yellow]{Markup.Escape(normalizedFolderName!)}[/]' was not found.");

			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogDebug(odataError, "Folder '{FolderName}' was not found.", normalizedFolderName);
			}

			return [];
		}
	}

	private static IReadOnlyList<EmailSummary> MapMessages(IList<Microsoft.Graph.Models.Message>? messages)
	{
		if (messages is null)
		{
			return [];
		}

		return messages
			.Select(message => new EmailSummary(
				message.From?.EmailAddress?.Address ?? "(unknown)",
				message.Subject ?? "(no subject)",
				message.ReceivedDateTime ?? DateTimeOffset.MinValue,
				message.IsRead ?? false))
			.ToList();
	}
}
