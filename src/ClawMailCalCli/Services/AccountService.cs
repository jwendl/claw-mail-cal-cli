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
		if (!TryNormalizeName(name, out var normalizedName))
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("Account name '{Name}' is invalid. Names must be non-empty and must not contain commas.", name);
			}

			return false;
		}

		var existingNames = await GetAccountNamesAsync(cancellationToken);
		if (existingNames.Contains(normalizedName, StringComparer.OrdinalIgnoreCase))
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("Account '{Name}' already exists.", normalizedName);
			}

			return false;
		}

		await secretStore.SetSecretValueAsync($"account-{normalizedName}-email", email, cancellationToken);

		var updatedNames = existingNames.Append(normalizedName).ToList();
		await secretStore.SetSecretValueAsync(AccountNamesSecret, string.Join(",", updatedNames), cancellationToken);

		if (logger.IsEnabled(LogLevel.Information))
		{
			logger.LogInformation("Account '{Name}' added successfully.", normalizedName);
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
		if (!TryNormalizeName(name, out var normalizedName))
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("Account name '{Name}' is invalid.", name);
			}

			return false;
		}

		var existingNames = await GetAccountNamesAsync(cancellationToken);
		if (!existingNames.Contains(normalizedName, StringComparer.OrdinalIgnoreCase))
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("Account '{Name}' does not exist.", normalizedName);
			}

			return false;
		}

		await secretStore.DeleteSecretAsync($"account-{normalizedName}-email", cancellationToken);

		var updatedNames = existingNames
			.Where(n => !string.Equals(n, normalizedName, StringComparison.OrdinalIgnoreCase))
			.ToList();

		await secretStore.SetSecretValueAsync(AccountNamesSecret, string.Join(",", updatedNames), cancellationToken);

		if (logger.IsEnabled(LogLevel.Information))
		{
			logger.LogInformation("Account '{Name}' deleted successfully.", normalizedName);
		}

		return true;
	}

	/// <inheritdoc />
	public async Task<bool> SetDefaultAccountAsync(string name, CancellationToken cancellationToken = default)
	{
		if (!TryNormalizeName(name, out var normalizedName))
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("Account name '{Name}' is invalid.", name);
			}

			return false;
		}

		var existingNames = await GetAccountNamesAsync(cancellationToken);
		if (!existingNames.Contains(normalizedName, StringComparer.OrdinalIgnoreCase))
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("Account '{Name}' does not exist.", normalizedName);
			}

			return false;
		}

		await secretStore.SetSecretValueAsync("default-account", normalizedName, cancellationToken);

		if (logger.IsEnabled(LogLevel.Information))
		{
			logger.LogInformation("Default account set to '{Name}'.", normalizedName);
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

	private static bool TryNormalizeName(string name, out string normalizedName)
	{
		var trimmed = name.Trim().ToLowerInvariant();
		if (string.IsNullOrEmpty(trimmed) || trimmed.Contains(','))
		{
			normalizedName = string.Empty;
			return false;
		}

		normalizedName = trimmed;
		return true;
	}
}
