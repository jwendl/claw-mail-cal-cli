namespace ClawMailCalCli.Services;

/// <summary>
/// Abstracts Key Vault secret operations for testability.
/// </summary>
public interface ISecretStore
{
	/// <summary>
	/// Gets the value of a secret, or returns null if the secret does not exist.
	/// </summary>
	Task<string?> GetSecretValueAsync(string name, CancellationToken cancellationToken = default);

	/// <summary>
	/// Sets the value of a secret, creating or updating as needed.
	/// </summary>
	Task SetSecretValueAsync(string name, string value, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes a secret by name. Does not throw if the secret does not exist.
	/// </summary>
	Task DeleteSecretAsync(string name, CancellationToken cancellationToken = default);
}
