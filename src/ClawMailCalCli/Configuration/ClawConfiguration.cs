namespace ClawMailCalCli.Configuration;

/// <summary>
/// Represents the configuration for the claw-mail-cal-cli application.
/// </summary>
/// <param name="KeyVaultUri">The URI of the Azure Key Vault to use.</param>
/// <param name="DefaultAccount">The optional default account name.</param>
public record ClawConfiguration(string KeyVaultUri, string? DefaultAccount = null);
