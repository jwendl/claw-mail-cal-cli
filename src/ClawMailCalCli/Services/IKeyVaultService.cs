namespace ClawMailCalCli.Services;

/// <summary>
/// Abstraction over Azure Key Vault secret operations used by the CLI.
/// </summary>
public interface IKeyVaultService
{
	/// <summary>Gets the value of a secret, or <see langword="null"/> if the secret does not exist.</summary>
	/// <param name="secretName">The name of the secret in Key Vault.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);

	/// <summary>Creates or updates a secret in Key Vault.</summary>
	/// <param name="secretName">The name of the secret.</param>
	/// <param name="secretValue">The value to store.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default);
}
