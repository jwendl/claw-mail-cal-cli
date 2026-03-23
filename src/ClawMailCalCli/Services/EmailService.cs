using ClawMailCalCli.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace ClawMailCalCli.Services;

/// <summary>
/// Implements email operations (list and read) using the Microsoft Graph API.
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

	private static readonly string[] MessageSelect =
	[
		"id",
		"subject",
		"from",
		"toRecipients",
		"receivedDateTime",
		"body",
		"bodyPreview",
	];

	/// <inheritdoc />
	public async Task<IReadOnlyList<EmailSummary>> GetEmailsAsync(string? folderName = null, CancellationToken cancellationToken = default)
	{
		var normalizedFolderName = folderName?.Trim();

		try
		{
			return await graphClientService.ExecuteWithRetryAsync(async graphClient =>
			{
				MessageCollectionResponse? messageCollection;

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

	/// <inheritdoc />
	public async Task<EmailMessage?> ReadEmailAsync(string accountName, string subjectOrId, CancellationToken cancellationToken = default)
	{
		return await graphClientService.ExecuteWithRetryAsync(async graphClient =>
		{
			if (LooksLikeMessageId(subjectOrId))
			{
				if (logger.IsEnabled(LogLevel.Debug))
				{
					logger.LogDebug("Fetching email by ID for account '{AccountName}'.", accountName);
				}

				try
				{
					var message = await graphClient.Me.Messages[subjectOrId].GetAsync(config =>
					{
						config.QueryParameters.Select = MessageSelect;
					}, cancellationToken);

					return message is null ? null : MapToEmailMessage(message);
				}
				catch (ODataError odataError) when (odataError.ResponseStatusCode == 404)
				{
					if (logger.IsEnabled(LogLevel.Debug))
					{
						logger.LogDebug("Message with ID '{MessageId}' not found for account '{AccountName}'.", subjectOrId, accountName);
					}

					return null;
				}
			}
			else
			{
				if (logger.IsEnabled(LogLevel.Debug))
				{
					logger.LogDebug("Searching email by subject for account '{AccountName}'.", accountName);
				}

				var escapedSubject = subjectOrId.Replace("'", "''");
				var response = await graphClient.Me.Messages.GetAsync(config =>
				{
					config.QueryParameters.Filter = $"contains(subject, '{escapedSubject}')";
					config.QueryParameters.Select = MessageSelect;
					config.QueryParameters.Top = 1;
				}, cancellationToken);

				var firstMessage = response?.Value?.FirstOrDefault();
				return firstMessage is null ? null : MapToEmailMessage(firstMessage);
			}
		}, cancellationToken);
	}

	private static IReadOnlyList<EmailSummary> MapMessages(IList<Message>? messages)
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

	/// <summary>
	/// Determines whether the given string looks like a Graph message ID rather than a subject.
	/// Graph message IDs are long Base64-encoded strings that contain '=' padding characters.
	/// </summary>
	internal static bool LooksLikeMessageId(string value) =>
		value.Contains('=') || value.Length >= MessageIdMinLength;

	/// <summary>
	/// Minimum length of a string to be considered a Graph message ID.
	/// Graph message IDs are long Base64-encoded strings, typically over 100 characters.
	/// </summary>
	private const int MessageIdMinLength = 100;

	/// <summary>
	/// Strips HTML tags from the given HTML content and returns plain text.
	/// </summary>
	internal static string StripHtml(string html)
	{
		if (string.IsNullOrWhiteSpace(html))
		{
			return string.Empty;
		}

		var htmlDocument = new HtmlDocument();
		htmlDocument.LoadHtml(html);

		var plainText = htmlDocument.DocumentNode.InnerText;
		return System.Net.WebUtility.HtmlDecode(plainText).Trim();
	}

	private static EmailMessage MapToEmailMessage(Message message)
	{
		var from = message.From?.EmailAddress?.Address ?? string.Empty;
		var to = string.Join(", ", message.ToRecipients?
			.Select(r => r.EmailAddress?.Address ?? string.Empty)
			.Where(a => !string.IsNullOrWhiteSpace(a))
			?? []);

		var bodyContent = message.Body?.ContentType == BodyType.Html
			? StripHtml(message.Body.Content ?? string.Empty)
			: message.Body?.Content ?? string.Empty;

		if (string.IsNullOrWhiteSpace(bodyContent))
		{
			bodyContent = message.BodyPreview ?? string.Empty;
		}

		return new EmailMessage(
			Id: message.Id ?? string.Empty,
			Subject: message.Subject ?? string.Empty,
			From: from,
			To: to,
			ReceivedDateTime: message.ReceivedDateTime,
			Body: bodyContent);
	}
}
