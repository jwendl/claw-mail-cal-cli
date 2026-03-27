namespace ClawMailCalCli.Models;

/// <summary>
/// String constants for machine-readable error codes emitted in structured JSON error output.
/// </summary>
public static class ErrorCodes
{
	/// <summary>The specified account does not exist.</summary>
	public const string AccountNotFound = "ACCOUNT_NOT_FOUND";

	/// <summary>An account with the given name already exists.</summary>
	public const string AccountAlreadyExists = "ACCOUNT_ALREADY_EXISTS";

	/// <summary>No cached credentials are available; the user must log in.</summary>
	public const string AuthRequired = "AUTH_REQUIRED";

	/// <summary>The device code flow or token refresh failed.</summary>
	public const string AuthFailed = "AUTH_FAILED";

	/// <summary>Microsoft Graph returned an error response.</summary>
	public const string GraphApiError = "GRAPH_API_ERROR";

	/// <summary>A required argument was missing or had an invalid format.</summary>
	public const string InvalidArgument = "INVALID_ARGUMENT";

	/// <summary>The configuration file is missing or contains invalid data.</summary>
	public const string ConfigError = "CONFIG_ERROR";

	/// <summary>Azure Key Vault is unreachable.</summary>
	public const string KeyVaultUnreachable = "KEYVAULT_UNREACHABLE";
}
