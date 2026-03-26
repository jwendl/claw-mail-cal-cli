using ClawMailCalCli.Models;
using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Services;

/// <summary>
/// Performs prerequisite environment checks for the <c>doctor</c> command.
/// </summary>
public partial class DoctorService(IConfigurationService configurationService, IAzureCliChecker azureCliChecker, IKeyVaultChecker keyVaultChecker, IAccountService accountService)
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
		results.Add(await CheckDefaultAccountAsync(cancellationToken));
		results.Add(CheckTokenCacheStorageMode());

		return results;
	}

	private static DoctorCheckResult CheckTokenCacheStorageMode()
	{
		if (OperatingSystem.IsLinux())
		{
			return new DoctorCheckResult("Token cache storage", true, "Expected file-based token cache managed by Azure.Identity on Linux. This tool does not validate the actual cache path or permissions; see Azure.Identity documentation for details.");
		}

		if (OperatingSystem.IsWindows())
		{
			return new DoctorCheckResult("Token cache storage", true, "Expected to use Windows DPAPI-based protected storage as per Azure.Identity defaults. This tool does not validate the underlying store.");
		}

		if (OperatingSystem.IsMacOS())
		{
			return new DoctorCheckResult("Token cache storage", true, "Expected to use the macOS Keychain as per Azure.Identity defaults. This tool does not validate the underlying store.");
		}

		return new DoctorCheckResult("Token cache storage", true, "Platform-default token cache behavior based on Azure.Identity. This tool does not validate the underlying store.");
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

	private async Task<DoctorCheckResult> CheckDefaultAccountAsync(CancellationToken cancellationToken)
	{
		var defaultAccount = await accountService.GetDefaultAccountAsync(cancellationToken);
		if (defaultAccount is not null)
		{
			return new DoctorCheckResult("Default account set", true, defaultAccount.Name);
		}

		return new DoctorCheckResult("Default account set", false, "No default account configured", "Run 'claw-mail-cal-cli account set <name>' to set a default account");
	}
}
