namespace ClawMailCalCli.Models;

/// <summary>
/// Holds the Entra ID configuration values read from <c>appsettings.json</c>.
/// </summary>
public class EntraOptions
{
	/// <summary>Gets or sets the Azure AD application (client) ID.</summary>
	public string ClientId { get; set; } = string.Empty;

	/// <summary>Gets or sets the tenant ID for personal Microsoft accounts (defaults to "common").</summary>
	public string PersonalTenantId { get; set; } = "common";

	/// <summary>Gets or sets the tenant ID for work / school accounts.</summary>
	public string WorkTenantId { get; set; } = string.Empty;
}
