using ClawMailCalCli.Models;

namespace ClawMailCalCli.Services;

/// <summary>
/// Defines operations for managing email/calendar accounts.
/// </summary>
public interface IAccountService
{
	/// <summary>
	/// Adds a new account to the store. Returns false if the account name already exists.
	/// </summary>
	Task<bool> AddAccountAsync(string name, string email, CancellationToken cancellationToken = default);

	/// <summary>
	/// Lists all stored accounts.
	/// </summary>
	Task<IReadOnlyList<Account>> ListAccountsAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes an account by name. Returns false if the account does not exist.
	/// </summary>
	Task<bool> DeleteAccountAsync(string name, CancellationToken cancellationToken = default);

	/// <summary>
	/// Sets the default account by name. Returns false if the account does not exist.
	/// </summary>
	Task<bool> SetDefaultAccountAsync(string name, CancellationToken cancellationToken = default);
}
