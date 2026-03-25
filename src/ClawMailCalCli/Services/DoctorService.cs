using ClawMailCalCli.Models;
using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Services;

/// <summary>
/// Performs prerequisite environment checks for the <c>doctor</c> command.
/// </summary>
public partial class DoctorService(IConfigurationService configurationService, IAzureCliChecker azureCliChecker, IKeyVaultChecker keyVaultChecker)
	: IDoctorService
{
	/// <inheritdoc />
	public async Task<IReadOnlyList<DoctorCheckResult>> RunAllChecksAsync(CancellationToken cancellationToken = default)
	{
		var results = new List<DoctorCheckResult>();

		var isAzureCliAuthenticated = await azureCliChecker.IsAuthenticatedAsync(cancellationToken);
		if (isAzureCliAuthenticated)
		{
			results.Add(new DoctorCheckResult("Azure CLI installed", true, "Azure CLI is installed and authenticated"));
		}
		else
		{
			results.Add(new DoctorCheckResult("Azure CLI installed", false, "Azure CLI not found or not logged in", "Install Azure CLI and run 'az login': https://learn.microsoft.com/en-us/cli/azure/install-azure-cli"));
		}

		ClawConfiguration? configuration = null;
		try
		{
			configuration = await configurationService.ReadConfigurationAsync();
			results.Add(new DoctorCheckResult("Config file found", true, "~/.claw-mail-cal-cli/config.json"));
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception)
		{
			results.Add(new DoctorCheckResult("Config file found", false, "Configuration file not found or invalid", "Create ~/.claw-mail-cal-cli/config.json with a valid 'keyVaultUri'. Example: {\"keyVaultUri\": \"https://my-kv.vault.azure.net/\"}"));
		}

		results.Add(await CheckKeyVaultAsync(configuration, cancellationToken));
		results.Add(CheckDefaultAccount(configuration));

		return results;
	}

	private async Task<DoctorCheckResult> CheckKeyVaultAsync(ClawConfiguration? configuration, CancellationToken cancellationToken)
	{
		if (configuration is null)
		{
			return new DoctorCheckResult("Key Vault reachable", false, "Skipped (config file not found or invalid)", "Fix the config file first");
		}

		var isReachable = await keyVaultChecker.IsReachableAsync(configuration.KeyVaultUri, cancellationToken);
		return new DoctorCheckResult("Key Vault reachable", isReachable, isReachable ? configuration.KeyVaultUri : "Key Vault is not reachable", isReachable ? null : "Ensure the Key Vault URI is correct and run 'az login'");
	}

	private static DoctorCheckResult CheckDefaultAccount(ClawConfiguration? configuration)
	{
		if (configuration is null)
		{
			return new DoctorCheckResult("Default account set", false, "Skipped (config file not found or invalid)", "Fix the config file first");
		}

		if (!string.IsNullOrWhiteSpace(configuration.DefaultAccount))
		{
			return new DoctorCheckResult("Default account set", true, configuration.DefaultAccount);
		}

		return new DoctorCheckResult("Default account set", false, "No default account configured", "Run 'claw-mail-cal-cli account set <name>' to set a default account");
	}
}
