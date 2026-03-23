namespace ClawMailCalCli.Services;

/// <summary>
/// Checks whether an Azure Key Vault is reachable with the current credentials.
/// </summary>
public interface IKeyVaultChecker
{
	/// <summary>
	/// Returns <see langword="true"/> if the Key Vault at <paramref name="vaultUri"/> is
	/// reachable and the current credentials have at least read access; otherwise <see langword="false"/>.
	/// </summary>
	/// <param name="vaultUri">The absolute HTTPS URI of the Azure Key Vault to probe.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task<bool> IsReachableAsync(string vaultUri, CancellationToken cancellationToken = default);
}
