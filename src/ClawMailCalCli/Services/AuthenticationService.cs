using Azure.Identity;
using ClawMailCalCli.Models;
using Microsoft.Extensions.Logging;

namespace ClawMailCalCli.Services;

/// <summary>
/// Implements Entra ID device code flow authentication, storing and retrieving
/// <see cref="AuthenticationRecord"/> instances from Azure Key Vault for silent
/// re-authentication on subsequent runs.
/// </summary>
/// <remarks>
/// The Entra application client ID and tenant ID are read from Key Vault secrets named
/// <c>{account-type-prefix}-client-id</c> and <c>{account-type-prefix}-tenant-id</c>,
/// where the prefix is <c>hotmail</c> for personal accounts and <c>exchange</c> for
/// work/school accounts.
/// </remarks>
public class AuthenticationService(IAccountService accountService, IKeyVaultService keyVaultService, IDeviceCodeCredentialProvider deviceCodeCredentialProvider, ILogger<AuthenticationService> logger)
	: IAuthenticationService
{
	private static readonly string[] GraphScopes =
	[
		"https://graph.microsoft.com/Mail.Read",
		"https://graph.microsoft.com/Mail.ReadWrite",
		"https://graph.microsoft.com/Mail.Send",
		"https://graph.microsoft.com/Calendars.Read",
		"https://graph.microsoft.com/Calendars.ReadWrite",
		"https://graph.microsoft.com/User.Read",
	];

	private static string AuthRecordSecretName(string accountName)
	{
		KeyVaultNameValidator.EnsureValid(accountName);
		return $"auth-record-{accountName}";
	}

	/// <inheritdoc />
	public async Task AuthenticateAsync(string accountName, CancellationToken cancellationToken = default)
	{
		var account = await accountService.GetAccountAsync(accountName, cancellationToken);
		if (account is null)
		{
			AnsiConsole.MarkupLine($"[red]Error:[/] Account '[bold]{accountName}[/]' not found. Use [bold]account add[/] to create it first.");
			return;
		}

		var prefix = AccountTypeKeyVaultPrefix(account.Type);
		var clientId = await keyVaultService.GetSecretAsync($"{prefix}-client-id", cancellationToken);
		var tenantId = await keyVaultService.GetSecretAsync($"{prefix}-tenant-id", cancellationToken);

		if (string.IsNullOrWhiteSpace(clientId))
		{
			AnsiConsole.MarkupLine($"[red]Error:[/] Key Vault secret '[bold]{prefix}-client-id[/]' is not set. Add it to Key Vault before authenticating.");
			return;
		}

		if (string.IsNullOrWhiteSpace(tenantId))
		{
			tenantId = "common";
		}

		var credentialOptions = new DeviceCodeCredentialOptions
		{
			AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
			ClientId = clientId,
			TenantId = tenantId,
			TokenCachePersistenceOptions = new TokenCachePersistenceOptions(),
			DeviceCodeCallback = (deviceCodeInfo, _) =>
			{
				AnsiConsole.MarkupLine($"[bold]Authenticating account:[/] {accountName}");
				AnsiConsole.MarkupLine(deviceCodeInfo.Message);
				return Task.CompletedTask;
			},
		};

		var existingRecord = await LoadAuthenticationRecordAsync(accountName, cancellationToken);
		if (existingRecord is not null)
		{
			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogDebug("Found cached AuthenticationRecord for account '{AccountName}', using silent authentication.", accountName);
			}

			credentialOptions.AuthenticationRecord = existingRecord;
			AnsiConsole.MarkupLine($"[green]✓[/] Account '[bold]{accountName}[/]' is already authenticated.");
			return;
		}

		if (logger.IsEnabled(LogLevel.Debug))
		{
			logger.LogDebug("No cached AuthenticationRecord found for account '{AccountName}', starting device code flow.", accountName);
		}

		var authenticationRecord = await deviceCodeCredentialProvider.AuthenticateAsync(credentialOptions, GraphScopes, cancellationToken);
		await SaveAuthenticationRecordAsync(accountName, authenticationRecord, cancellationToken);

		AnsiConsole.MarkupLine($"[green]✓[/] Account '[bold]{accountName}[/]' authenticated successfully.");
	}

	private async Task<AuthenticationRecord?> LoadAuthenticationRecordAsync(string accountName, CancellationToken cancellationToken)
	{
		var secretValue = await keyVaultService.GetSecretAsync(AuthRecordSecretName(accountName), cancellationToken);
		if (string.IsNullOrWhiteSpace(secretValue))
		{
			return null;
		}

		try
		{
			var recordBytes = Convert.FromBase64String(secretValue);
			using var memoryStream = new MemoryStream(recordBytes);
			return await AuthenticationRecord.DeserializeAsync(memoryStream, cancellationToken);
		}
		catch (FormatException formatException)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning(formatException, "Failed to parse cached AuthenticationRecord for account '{AccountName}'. Stored secret is not valid Base64. User must re-authenticate.", accountName);
			}
		}
		catch (Exception deserializationException) when (deserializationException is not FormatException)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning(deserializationException, "Failed to deserialize cached AuthenticationRecord for account '{AccountName}'. User must re-authenticate.", accountName);
			}
		}

		AnsiConsole.MarkupLine($"[yellow]Warning:[/] Cached authentication data for account '[bold]{accountName}[/]' is invalid. Please re-authenticate. You may need to delete the Key Vault secret '[bold]{AuthRecordSecretName(accountName)}[/]'.");
		return null;
	}

	private async Task SaveAuthenticationRecordAsync(string accountName, AuthenticationRecord authenticationRecord, CancellationToken cancellationToken)
	{
		using var memoryStream = new MemoryStream();
		await authenticationRecord.SerializeAsync(memoryStream, cancellationToken);
		var base64Value = Convert.ToBase64String(memoryStream.ToArray());
		await keyVaultService.SetSecretAsync(AuthRecordSecretName(accountName), base64Value, cancellationToken);
	}

	/// <summary>
	/// Returns the Key Vault secret name prefix for the given account type.
	/// Personal accounts use <c>hotmail</c>; work/school accounts use <c>exchange</c>.
	/// </summary>
	private static string AccountTypeKeyVaultPrefix(AccountType accountType) => accountType switch
	{
		AccountType.Personal => "hotmail",
		AccountType.Work => "exchange",
		_ => throw new InvalidOperationException($"Unknown account type: {accountType}"),
	};
}
