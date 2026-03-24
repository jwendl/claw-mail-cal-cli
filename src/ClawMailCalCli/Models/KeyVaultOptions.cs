namespace ClawMailCalCli.Models;

/// <summary>
/// Holds the Azure Key Vault configuration values read from <c>appsettings.json</c>.
/// </summary>
public class KeyVaultOptions
{
	/// <summary>Gets or sets the URI of the Azure Key Vault (e.g. https://my-vault.vault.azure.net/).</summary>
	public string VaultUri { get; set; } = string.Empty;
}
