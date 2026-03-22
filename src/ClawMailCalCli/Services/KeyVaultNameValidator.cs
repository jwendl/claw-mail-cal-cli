using System.Text.RegularExpressions;

namespace ClawMailCalCli.Services;

/// <summary>
/// Validates that account names conform to Azure Key Vault secret naming rules
/// (alphanumeric characters and hyphens only).
/// </summary>
internal static partial class KeyVaultNameValidator
{
	[GeneratedRegex(@"^[a-zA-Z0-9-]+$")]
	private static partial Regex ValidNamePattern();

	/// <summary>
	/// Throws <see cref="ArgumentException"/> if <paramref name="accountName"/> contains
	/// characters not permitted in an Azure Key Vault secret name.
	/// </summary>
	/// <param name="accountName">The account name to validate.</param>
	internal static void EnsureValid(string accountName)
	{
		if (string.IsNullOrWhiteSpace(accountName))
		{
			throw new ArgumentException("Account name cannot be empty.", nameof(accountName));
		}

		if (!ValidNamePattern().IsMatch(accountName))
		{
			throw new ArgumentException(
				$"Account name '{accountName}' contains invalid characters. Azure Key Vault secret names only allow letters, digits, and hyphens.",
				nameof(accountName));
		}
	}
}
