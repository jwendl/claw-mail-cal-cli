using ClawMailCalCli.Models;
using Microsoft.Extensions.Logging;

namespace ClawMailCalCli.Services;

/// <summary>
/// Manages email/calendar accounts using a secret store as backing storage.
/// </summary>
public class AccountService(ISecretStore secretStore, ILogger<AccountService> logger)
	: IAccountService
{
	private const string AccountNamesSecret = "account-names";

	/// <inheritdoc />
	public async Task<bool> AddAccountAsync(string name, string email, CancellationToken cancellationToken = default)
	{
		var existingNames = await GetAccountNamesAsync(cancellationToken);
		if (existingNames.Contains(name, StringComparer.OrdinalIgnoreCase))
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("Account '{Name}' already exists.", name);
			}

			return false;
		}

		await secretStore.SetSecretValueAsync($"account-{name}-email", email, cancellationToken);

		var updatedNames = existingNames.Append(name).ToList();
		await secretStore.SetSecretValueAsync(AccountNamesSecret, string.Join(",", updatedNames), cancellationToken);

		if (logger.IsEnabled(LogLevel.Information))
		{
			logger.LogInformation("Account '{Name}' added successfully.", name);
		}

		return true;
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<Account>> ListAccountsAsync(CancellationToken cancellationToken = default)
	{
		var names = await GetAccountNamesAsync(cancellationToken);
		var accounts = new List<Account>();

		foreach (var name in names)
		{
			var email = await secretStore.GetSecretValueAsync($"account-{name}-email", cancellationToken);
			if (!string.IsNullOrWhiteSpace(email))
			{
				accounts.Add(new Account(name, email));
			}
			else
			{
				if (logger.IsEnabled(LogLevel.Warning))
				{
					logger.LogWarning("Could not retrieve email for account '{Name}'.", name);
				}
			}
		}

		return accounts;
	}

	/// <inheritdoc />
	public async Task<bool> DeleteAccountAsync(string name, CancellationToken cancellationToken = default)
	{
		var existingNames = await GetAccountNamesAsync(cancellationToken);
		if (!existingNames.Contains(name, StringComparer.OrdinalIgnoreCase))
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("Account '{Name}' does not exist.", name);
			}

			return false;
		}

		await secretStore.DeleteSecretAsync($"account-{name}-email", cancellationToken);

		var updatedNames = existingNames
			.Where(n => !string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
			.ToList();

		await secretStore.SetSecretValueAsync(AccountNamesSecret, string.Join(",", updatedNames), cancellationToken);

		if (logger.IsEnabled(LogLevel.Information))
		{
			logger.LogInformation("Account '{Name}' deleted successfully.", name);
		}

		return true;
	}

	/// <inheritdoc />
	public async Task<bool> SetDefaultAccountAsync(string name, CancellationToken cancellationToken = default)
	{
		var existingNames = await GetAccountNamesAsync(cancellationToken);
		if (!existingNames.Contains(name, StringComparer.OrdinalIgnoreCase))
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("Account '{Name}' does not exist.", name);
			}

			return false;
		}

		await secretStore.SetSecretValueAsync("default-account", name, cancellationToken);

		if (logger.IsEnabled(LogLevel.Information))
		{
			logger.LogInformation("Default account set to '{Name}'.", name);
		}

		return true;
	}

	private async Task<IReadOnlyList<string>> GetAccountNamesAsync(CancellationToken cancellationToken)
	{
		var value = await secretStore.GetSecretValueAsync(AccountNamesSecret, cancellationToken);
		if (string.IsNullOrWhiteSpace(value))
		{
			return [];
		}

		return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}
}
