using Azure.Identity;
using ClawMailCalCli.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClawMailCalCli.Services;

/// <summary>
/// Implements Entra ID device code flow authentication, storing and retrieving
/// <see cref="AuthenticationRecord"/> instances from Azure Key Vault for silent
/// re-authentication on subsequent runs.
/// </summary>
public class AuthenticationService(IAccountService accountService, IKeyVaultService keyVaultService, IDeviceCodeCredentialProvider deviceCodeCredentialProvider, IOptions<EntraOptions> entraOptions, ILogger<AuthenticationService> logger)
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

	private static string AuthRecordSecretName(string accountName) => $"auth-record-{accountName}";

	/// <inheritdoc />
	public async Task AuthenticateAsync(string accountName, CancellationToken cancellationToken = default)
	{
		var account = await accountService.GetAccountAsync(accountName, cancellationToken);
		if (account is null)
		{
			AnsiConsole.MarkupLine($"[red]Error:[/] Account '[bold]{accountName}[/]' not found. Use [bold]account add[/] to create it first.");
			return;
		}

		var options = entraOptions.Value;
		var tenantId = account.Type == AccountType.Personal
			? options.PersonalTenantId
			: options.WorkTenantId;

		var credentialOptions = new DeviceCodeCredentialOptions
		{
			AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
			ClientId = options.ClientId,
			TenantId = tenantId,
			TokenCachePersistenceOptions = new TokenCachePersistenceOptions(),
			DeviceCodeCallback = (deviceCodeInfo, token) =>
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

		var recordBytes = Convert.FromBase64String(secretValue);
		using var memoryStream = new MemoryStream(recordBytes);
		return await AuthenticationRecord.DeserializeAsync(memoryStream, cancellationToken);
	}

	private async Task SaveAuthenticationRecordAsync(string accountName, AuthenticationRecord authenticationRecord, CancellationToken cancellationToken)
	{
		using var memoryStream = new MemoryStream();
		await authenticationRecord.SerializeAsync(memoryStream, cancellationToken);
		var base64Value = Convert.ToBase64String(memoryStream.ToArray());
		await keyVaultService.SetSecretAsync(AuthRecordSecretName(accountName), base64Value, cancellationToken);
	}
}
