using ClawMailCalCli.Models;

namespace ClawMailCalCli.Services;

/// <summary>
/// Centralized defaults for account-type-specific tenant configuration.
/// </summary>
internal static class TenantDefaults
{
	/// <summary>
	/// Returns the default tenant ID for the given account type when not explicitly configured in Key Vault.
	/// Personal Microsoft accounts use <c>consumers</c>; work/school accounts use <c>organizations</c>.
	/// </summary>
	public static string GetDefaultTenantId(AccountType accountType) => accountType switch
	{
		AccountType.Personal => "consumers",
		AccountType.Work => "organizations",
		_ => throw new InvalidOperationException($"Unknown account type: {accountType}"),
	};
}
