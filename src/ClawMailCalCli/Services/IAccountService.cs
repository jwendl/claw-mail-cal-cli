using ClawMailCalCli.Models;

namespace ClawMailCalCli.Services;

/// <summary>
/// Manages named accounts stored in Azure Key Vault.
/// </summary>
public interface IAccountService
{
	/// <summary>Returns the account with the given name, or <see langword="null"/> if it does not exist.</summary>
	/// <param name="accountName">The short name of the account.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task<Account?> GetAccountAsync(string accountName, CancellationToken cancellationToken = default);

	/// <summary>Saves (creates or updates) an account in Key Vault.</summary>
	/// <param name="account">The account to persist.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task SaveAccountAsync(Account account, CancellationToken cancellationToken = default);
}
