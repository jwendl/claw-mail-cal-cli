using System.Text.Json;
using ClawMailCalCli.Models;

namespace ClawMailCalCli.Services;

/// <summary>
/// Manages named accounts persisted as JSON secrets in Azure Key Vault.
/// Secrets are stored under the key <c>account-{name}</c>.
/// </summary>
public class AccountService(IKeyVaultService keyVaultService)
	: IAccountService
{
	private static string SecretName(string accountName)
	{
		KeyVaultNameValidator.EnsureValid(accountName);
		return $"account-{accountName}";
	}

	/// <inheritdoc />
	public async Task<Account?> GetAccountAsync(string accountName, CancellationToken cancellationToken = default)
	{
		var secretValue = await keyVaultService.GetSecretAsync(SecretName(accountName), cancellationToken);
		if (string.IsNullOrWhiteSpace(secretValue))
		{
			return null;
		}

		return JsonSerializer.Deserialize<Account>(secretValue);
	}

	/// <inheritdoc />
	public async Task SaveAccountAsync(Account account, CancellationToken cancellationToken = default)
	{
		var secretValue = JsonSerializer.Serialize(account);
		await keyVaultService.SetSecretAsync(SecretName(account.Name), secretValue, cancellationToken);
	}
}
