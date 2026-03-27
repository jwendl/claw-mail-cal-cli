using System.Text.Json;
using Azure.Identity;
using ClawMailCalCli.Models;
using ClawMailCalCli.Services.Interfaces;

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
public class AuthenticationService(IAccountService accountService, IKeyVaultService keyVaultService, IDeviceCodeCredentialProvider deviceCodeCredentialProvider, NonInteractiveMode nonInteractiveMode, ILogger<AuthenticationService> logger)
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
	public async Task<bool> AuthenticateAsync(string accountName, CancellationToken cancellationToken = default, bool forceInteractive = false)
	{
		var account = await accountService.GetAccountAsync(accountName, cancellationToken);
		if (account is null)
		{
			AnsiConsole.MarkupLine($"[red]Error:[/] Account '[bold]{Markup.Escape(accountName)}[/]' not found. Use [bold]account add[/] to create it first.");
			return false;
		}

		var prefix = AccountTypeKeyVaultPrefix(account.Type);
		var clientId = await keyVaultService.GetSecretAsync($"{prefix}-client-id", cancellationToken);
		var tenantId = await keyVaultService.GetSecretAsync($"{prefix}-tenant-id", cancellationToken);

		if (string.IsNullOrWhiteSpace(clientId))
		{
			AnsiConsole.MarkupLine($"[red]Error:[/] Key Vault secret '[bold]{prefix}-client-id[/]' is not set. Add it to Key Vault before authenticating.");
			return false;
		}

		if (string.IsNullOrWhiteSpace(tenantId))
		{
			tenantId = TenantDefaults.GetDefaultTenantId(account.Type);

			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogDebug("Key Vault secret '{Prefix}-tenant-id' not set. Using default tenant ID '{TenantId}' for account type {AccountType}.", prefix, tenantId, account.Type);
			}
		}

		var credentialOptions = new DeviceCodeCredentialOptions
		{
			AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
			ClientId = clientId,
			TenantId = tenantId,
			TokenCachePersistenceOptions = TokenCachePersistenceOptionsFactory.Create(),
			DeviceCodeCallback = (deviceCodeInfo, _) =>
			{
				AnsiConsole.MarkupLine($"[bold]Authenticating account:[/] {Markup.Escape(accountName)}");
				AnsiConsole.WriteLine(deviceCodeInfo.Message);
				return Task.CompletedTask;
			},
		};

		var existingRecord = await LoadAuthenticationRecordAsync(accountName, cancellationToken);
		if (existingRecord is not null && !forceInteractive)
		{
			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogDebug("Found cached AuthenticationRecord for account '{AccountName}', using silent authentication.", accountName);
			}

			credentialOptions.AuthenticationRecord = existingRecord;
			AnsiConsole.MarkupLine($"[green]✓[/] Account '[bold]{Markup.Escape(accountName)}[/]' is already authenticated.");
			return true;
		}

		if (logger.IsEnabled(LogLevel.Debug))
		{
			logger.LogDebug("No cached AuthenticationRecord found for account '{AccountName}', starting device code flow.", accountName);
		}

		if (nonInteractiveMode.IsNonInteractive && !forceInteractive)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("Non-interactive mode is active and no cached AuthenticationRecord exists for account '{AccountName}'. Aborting to avoid device-code prompt.", accountName);
			}

			WriteAuthRequiredError(accountName, nonInteractiveMode.IsJson);
			return false;
		}

		try
		{
			var authenticationRecord = await deviceCodeCredentialProvider.AuthenticateAsync(credentialOptions, GraphScopes, cancellationToken);
			await SaveAuthenticationRecordAsync(accountName, authenticationRecord, cancellationToken);

			AnsiConsole.MarkupLine($"[green]✓[/] Account '[bold]{Markup.Escape(accountName)}[/]' authenticated successfully.");
			return true;
		}
		catch (AuthenticationFailedException authenticationFailedException)
		{
			if (logger.IsEnabled(LogLevel.Error))
			{
				logger.LogError(authenticationFailedException, "Device code authentication failed for account '{AccountName}'.", accountName);
			}

			AnsiConsole.MarkupLine($"[red]Error:[/] DeviceCodeCredential authentication failed.");
			AnsiConsole.MarkupLine($"[red]Details:[/] {Markup.Escape(authenticationFailedException.Message)}");

			if (authenticationFailedException.InnerException is not null)
			{
				AnsiConsole.MarkupLine($"[yellow]Inner error:[/] {Markup.Escape(authenticationFailedException.InnerException.Message)}");
			}

			return false;
		}
	}

	private async Task<AuthenticationRecord?> LoadAuthenticationRecordAsync(string accountName, CancellationToken cancellationToken)
	{
		try
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
		catch (FormatException formatException)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning(formatException, "Failed to parse cached AuthenticationRecord for account '{AccountName}'. Stored secret is not valid Base64. User must re-authenticate.", accountName);
			}

			AnsiConsole.MarkupLine($"[yellow]Warning:[/] Cached authentication data for account '[bold]{Markup.Escape(accountName)}[/]' is invalid. Please re-authenticate. You may need to delete the Key Vault secret '[bold]{Markup.Escape(AuthRecordSecretName(accountName))}[/]'.");
		}
		catch (Exception exception) when (exception is not OperationCanceledException)
		{
			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogDebug(exception, "Failed to load cached AuthenticationRecord for account '{AccountName}'. This may be expected for first-time authentication or if Key Vault is unreachable. User will authenticate via device code flow.", accountName);
			}
		}

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

	/// <summary>
	/// Writes an AUTH_REQUIRED error to stdout (JSON) or stderr (human-readable) depending
	/// on whether JSON mode is active.
	/// </summary>
	private static void WriteAuthRequiredError(string accountName, bool isJson)
	{
		const string message = "Authentication required. Run 'claw-mail-cal-cli login <account>' interactively first.";
		const string code = "AUTH_REQUIRED";

		if (isJson)
		{
			var errorObject = new { error = message, code };
			Console.Out.WriteLine(JsonSerializer.Serialize(errorObject));
		}
		else
		{
			Console.Error.WriteLine($"Error: Authentication required for account '{accountName}'.");
			Console.Error.WriteLine($"Hint: Run 'claw-mail-cal-cli login {accountName}' interactively first, then use --non-interactive for subsequent calls.");
		}
	}
}
