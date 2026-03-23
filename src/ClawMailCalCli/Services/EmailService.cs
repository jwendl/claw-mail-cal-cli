using ClawMailCalCli.Models;
using Microsoft.Extensions.Logging;

namespace ClawMailCalCli.Services;

/// <summary>
/// Retrieves email summaries for the current default account via <see cref="IGraphClientService"/>.
/// </summary>
public class EmailService(IAccountService accountService, IGraphClientService graphClientService, ILogger<EmailService> logger)
	: IEmailService
{
	private const int DefaultMessageCount = 20;

	/// <inheritdoc />
	public async Task<IReadOnlyList<EmailSummary>> GetEmailsAsync(string? folderName = null, CancellationToken cancellationToken = default)
	{
		var defaultAccount = await accountService.GetDefaultAccountAsync(cancellationToken);
		if (defaultAccount is null)
		{
			AnsiConsole.MarkupLine("[red]Error:[/] No default account is set. Use [bold]account set <name>[/] to configure one.");
			return [];
		}

		var normalizedFolderName = folderName?.Trim();

		if (logger.IsEnabled(LogLevel.Debug))
		{
			var effectiveFolderName = string.IsNullOrWhiteSpace(normalizedFolderName) ? "inbox" : normalizedFolderName;
			logger.LogDebug("Listing emails for account '{AccountName}', folder '{FolderName}'.", defaultAccount.Name, effectiveFolderName);
		}

		try
		{
			if (string.IsNullOrWhiteSpace(normalizedFolderName))
			{
				return await graphClientService.GetInboxMessagesAsync(defaultAccount.Name, DefaultMessageCount, cancellationToken);
			}

			return await graphClientService.GetFolderMessagesAsync(defaultAccount.Name, normalizedFolderName!, DefaultMessageCount, cancellationToken);
		}
		catch (InvalidOperationException invalidOperationException) when (invalidOperationException.Message.StartsWith("Folder '", StringComparison.Ordinal))
		{
			AnsiConsole.MarkupLine($"[red]Error:[/] Folder '[yellow]{Markup.Escape(normalizedFolderName!)}[/]' was not found.");

			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogDebug(invalidOperationException, "Folder '{FolderName}' was not found for account '{AccountName}'.", normalizedFolderName, defaultAccount.Name);
			}

			return [];
		}
	}
}
