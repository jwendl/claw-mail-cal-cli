using ClawMailCalCli.Models;

namespace ClawMailCalCli.Services;

/// <summary>
/// Defines operations for managing email/calendar accounts.
/// </summary>
public interface IAccountService
{
/// <summary>Returns the account with the given name, or <see langword="null"/> if it does not exist.</summary>
/// <param name="accountName">The short name of the account.</param>
/// <param name="cancellationToken">Cancellation token.</param>
Task<Account?> GetAccountAsync(string accountName, CancellationToken cancellationToken = default);

/// <summary>Saves (creates or updates) an account in the local database.</summary>
/// <param name="account">The account to persist.</param>
/// <param name="cancellationToken">Cancellation token.</param>
Task SaveAccountAsync(Account account, CancellationToken cancellationToken = default);

/// <summary>
/// Adds a new account to the store. Returns false if the account name already exists.
/// </summary>
Task<bool> AddAccountAsync(string name, string email, AccountType accountType = AccountType.Personal, CancellationToken cancellationToken = default);

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