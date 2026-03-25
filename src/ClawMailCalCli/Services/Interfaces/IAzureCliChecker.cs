namespace ClawMailCalCli.Services.Interfaces;

/// <summary>
/// Checks whether the Azure CLI is installed and the user is authenticated.
/// </summary>
public interface IAzureCliChecker
{
	/// <summary>
	/// Returns <see langword="true"/> if the Azure CLI is installed and the user is authenticated;
	/// otherwise returns <see langword="false"/>.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default);
}
