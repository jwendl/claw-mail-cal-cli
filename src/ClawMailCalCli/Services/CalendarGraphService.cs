using System.Net;
using System.Text.RegularExpressions;
using Azure.Identity;
using ClawMailCalCli.Models;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace ClawMailCalCli.Services;

/// <summary>
/// Calls Microsoft Graph calendar endpoints using the account's cached Entra credentials.
/// </summary>
public partial class CalendarGraphService(IAccountService accountService, IKeyVaultService keyVaultService, IOptions<EntraOptions> entraOptions, ILogger<CalendarGraphService> logger)
	: ICalendarGraphService
{
	private static readonly string[] CalendarReadScopes = ["https://graph.microsoft.com/Calendars.Read"];
	private static readonly string[] EventSelectFields = ["subject", "start", "end", "location", "organizer", "attendees", "body"];

	/// <inheritdoc />
	public async Task<CalendarEvent?> GetEventByIdAsync(string accountName, string eventId, CancellationToken cancellationToken = default)
	{
		var graphClient = await CreateGraphClientAsync(accountName, cancellationToken);
		if (graphClient is null)
		{
			return null;
		}

		try
		{
			var graphEvent = await graphClient.Me.Events[eventId].GetAsync(config =>
			{
				config.QueryParameters.Select = EventSelectFields;
			}, cancellationToken);

			return graphEvent is null ? null : ToCalendarEvent(graphEvent);
		}
		catch (ODataError odataError) when (odataError.ResponseStatusCode == 404)
		{
			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogDebug("Event ID '{EventId}' not found for account '{AccountName}'.", eventId, accountName);
			}

			return null;
		}
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<CalendarEvent>> GetEventsBySubjectFilterAsync(string accountName, string subject, CancellationToken cancellationToken = default)
	{
		var graphClient = await CreateGraphClientAsync(accountName, cancellationToken);
		if (graphClient is null)
		{
			return [];
		}

		var escapedSubject = subject.Replace("'", "''");
		var response = await graphClient.Me.Events.GetAsync(config =>
		{
			config.QueryParameters.Filter = $"contains(subject, '{escapedSubject}')";
			config.QueryParameters.Select = EventSelectFields;
			config.QueryParameters.Top = 10;
		}, cancellationToken);

		return (response?.Value ?? [])
			.Select(ToCalendarEvent)
			.ToList();
	}

	private async Task<GraphServiceClient?> CreateGraphClientAsync(string accountName, CancellationToken cancellationToken)
	{
		var account = await accountService.GetAccountAsync(accountName, cancellationToken);
		if (account is null)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("Account '{AccountName}' not found.", accountName);
			}

			return null;
		}

		KeyVaultNameValidator.EnsureValid(accountName);
		var secretName = $"auth-record-{accountName}";
		var secretValue = await keyVaultService.GetSecretAsync(secretName, cancellationToken);

		AuthenticationRecord? authRecord = null;
		if (!string.IsNullOrWhiteSpace(secretValue))
		{
			try
			{
				var recordBytes = Convert.FromBase64String(secretValue);
				using var memoryStream = new MemoryStream(recordBytes);
				authRecord = await AuthenticationRecord.DeserializeAsync(memoryStream, cancellationToken);
			}
			catch (Exception deserializationException)
			{
				if (logger.IsEnabled(LogLevel.Warning))
				{
					logger.LogWarning(deserializationException, "Failed to deserialize cached AuthenticationRecord for account '{AccountName}'.", accountName);
				}
			}
		}

		var options = entraOptions.Value;
		var tenantId = account.Type == AccountType.Personal ? options.PersonalTenantId : options.WorkTenantId;

		var credentialOptions = new DeviceCodeCredentialOptions
		{
			AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
			ClientId = options.ClientId,
			TenantId = tenantId,
			TokenCachePersistenceOptions = new TokenCachePersistenceOptions(),
		};

		if (authRecord is not null)
		{
			credentialOptions.AuthenticationRecord = authRecord;
		}

		var credential = new DeviceCodeCredential(credentialOptions);
		return new GraphServiceClient(credential, CalendarReadScopes);
	}

	private static CalendarEvent ToCalendarEvent(Event graphEvent)
	{
		return new CalendarEvent(
			Id: graphEvent.Id ?? string.Empty,
			Subject: graphEvent.Subject ?? string.Empty,
			Start: graphEvent.Start?.DateTime,
			End: graphEvent.End?.DateTime,
			Location: graphEvent.Location?.DisplayName,
			Organizer: graphEvent.Organizer?.EmailAddress?.Name,
			Attendees: (graphEvent.Attendees?
				.Select(attendee => attendee.EmailAddress?.Name ?? attendee.EmailAddress?.Address ?? string.Empty)
				.Where(name => !string.IsNullOrWhiteSpace(name))
				.ToList() ?? []),
			Body: ExtractBodyText(graphEvent.Body)
		);
	}

	private static string? ExtractBodyText(ItemBody? body)
	{
		if (body is null)
		{
			return null;
		}

		if (body.ContentType == BodyType.Text)
		{
			return body.Content?.Trim();
		}

		return StripHtml(body.Content);
	}

	private static string? StripHtml(string? html)
	{
		if (string.IsNullOrWhiteSpace(html))
		{
			return html;
		}

		var text = HtmlTagPattern().Replace(html, string.Empty);
		return WebUtility.HtmlDecode(text).Trim();
	}

	[GeneratedRegex(@"<[^>]+>", RegexOptions.None)]
	private static partial Regex HtmlTagPattern();
}
