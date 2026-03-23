using ClawMailCalCli.Data;
using ClawMailCalCli.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClawMailCalCli.Services;

/// <summary>
/// Manages email/calendar accounts using a local SQLite database as backing storage.
/// </summary>
public class AccountService(IDbContextFactory<ApplicationDbContext> dbContextFactory, ILogger<AccountService> logger)
: IAccountService
{
	/// <inheritdoc />
	public async Task<Account?> GetAccountAsync(string accountName, CancellationToken cancellationToken = default)
	{
		var trimmed = accountName.Trim().ToLowerInvariant();
		await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

		var entity = await context.Accounts.FirstOrDefaultAsync(a => a.Name == trimmed, cancellationToken);
		if (entity is null)
		{
			return null;
		}

		return new Account(entity.Name, entity.Email, entity.Type);
	}

	/// <inheritdoc />
	public async Task SaveAccountAsync(Account account, CancellationToken cancellationToken = default)
	{
		var normalizedName = account.Name.Trim().ToLowerInvariant();
		await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

		var entity = await context.Accounts.FirstOrDefaultAsync(a => a.Name == normalizedName, cancellationToken);
		if (entity is not null)
		{
			entity.Email = account.Email;
			entity.Type = account.Type;
		}
		else
		{
			context.Accounts.Add(new AccountEntity { Name = normalizedName, Email = account.Email, Type = account.Type });
		}

		await context.SaveChangesAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task<bool> AddAccountAsync(string name, string email, AccountType accountType = AccountType.Personal, CancellationToken cancellationToken = default)
	{
		if (!TryNormalizeName(name, out var normalizedName))
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("Account name '{Name}' is invalid. Names must be non-empty and must not contain commas.", name);
			}

			return false;
		}

		await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

		var exists = await context.Accounts.AnyAsync(a => a.Name == normalizedName, cancellationToken);
		if (exists)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("Account '{Name}' already exists.", normalizedName);
			}

			return false;
		}

		context.Accounts.Add(new AccountEntity { Name = normalizedName, Email = email, Type = accountType });
		await context.SaveChangesAsync(cancellationToken);

		if (logger.IsEnabled(LogLevel.Information))
		{
			logger.LogInformation("Account '{Name}' added successfully.", normalizedName);
		}

		return true;
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<Account>> ListAccountsAsync(CancellationToken cancellationToken = default)
	{
		await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

		var entities = await context.Accounts
		.OrderBy(a => a.Name)
		.ToListAsync(cancellationToken);

		return entities.Select(e => new Account(e.Name, e.Email, e.Type)).ToList();
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

		await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

		var entity = await context.Accounts.FirstOrDefaultAsync(a => a.Name == normalizedName, cancellationToken);
		if (entity is null)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("Account '{Name}' does not exist.", normalizedName);
			}

			return false;
		}

		context.Accounts.Remove(entity);
		await context.SaveChangesAsync(cancellationToken);

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

		await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

		var entity = await context.Accounts.FirstOrDefaultAsync(a => a.Name == normalizedName, cancellationToken);
		if (entity is null)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("Account '{Name}' does not exist.", normalizedName);
			}

			return false;
		}

		var existingDefaults = await context.Accounts
		.Where(a => a.IsDefault)
		.ToListAsync(cancellationToken);

		foreach (var defaultAccount in existingDefaults)
		{
			defaultAccount.IsDefault = false;
		}

		entity.IsDefault = true;
		await context.SaveChangesAsync(cancellationToken);

		if (logger.IsEnabled(LogLevel.Information))
		{
			logger.LogInformation("Default account set to '{Name}'.", normalizedName);
		}

		return true;
	}

	/// <inheritdoc />
	public async Task<Account?> GetDefaultAccountAsync(CancellationToken cancellationToken = default)
	{
		await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

		var entity = await context.Accounts.FirstOrDefaultAsync(a => a.IsDefault, cancellationToken);
		if (entity is null)
		{
			return null;
		}

		return new Account(entity.Name, entity.Email, entity.Type);
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
