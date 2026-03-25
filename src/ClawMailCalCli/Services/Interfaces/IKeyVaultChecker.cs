namespace ClawMailCalCli.Services.Interfaces;

/// <summary>
/// Checks whether an Azure Key Vault is reachable over the network with the current credentials, regardless of secret access permissions.
/// </summary>
public interface IKeyVaultChecker
{
	/// <summary>
	/// Returns <see langword="true"/> if the Key Vault at <paramref name="vaultUri"/> is reachable with the current credentials
	/// (for example, responding with HTTP 200, 401, or 403), regardless of whether those credentials have secret read permissions;
	/// otherwise returns <see langword="false"/>.
	/// </summary>
	/// <param name="vaultUri">The absolute HTTPS URI of the Azure Key Vault to probe.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task<bool> IsReachableAsync(string vaultUri, CancellationToken cancellationToken = default);
}
