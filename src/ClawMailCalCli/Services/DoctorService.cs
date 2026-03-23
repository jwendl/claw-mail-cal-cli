using System.Text.RegularExpressions;
using ClawMailCalCli.Configuration;
using ClawMailCalCli.Models;

namespace ClawMailCalCli.Services;

/// <summary>
/// Performs prerequisite environment checks for the <c>doctor</c> command.
/// </summary>
public class DoctorService(IProcessRunner processRunner, IConfigurationService configurationService, IKeyVaultChecker keyVaultChecker)
	: IDoctorService
{
	/// <inheritdoc />
	public async Task<IReadOnlyList<DoctorCheckResult>> RunAllChecksAsync(CancellationToken cancellationToken = default)
	{
		var results = new List<DoctorCheckResult>();

		var azureCliInstalledResult = await CheckAzureCliInstalledAsync(cancellationToken);
		results.Add(azureCliInstalledResult);

		if (azureCliInstalledResult.IsSuccess)
		{
			results.Add(await CheckAzureCliLoggedInAsync(cancellationToken));
		}
		else
		{
			results.Add(new DoctorCheckResult(
				"Azure CLI logged in",
				false,
				"Skipped (Azure CLI not installed)",
				"Install Azure CLI to enable login: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli"));
		}

		ClawConfiguration? configuration = null;
		try
		{
			configuration = await configurationService.ReadConfigurationAsync();
			results.Add(new DoctorCheckResult(
				"Config file found",
				true,
				"~/.claw-mail-cal-cli/config.json"));
		}
		catch (Exception exception) when (exception is not null)
		{
			results.Add(new DoctorCheckResult(
				"Config file found",
				false,
				"Configuration file not found or invalid",
				"Create ~/.claw-mail-cal-cli/config.json with a valid 'keyVaultUri'. " +
				"Example: {\"keyVaultUri\": \"https://my-kv.vault.azure.net/\"}"));
		}

		results.Add(await CheckKeyVaultAsync(configuration, cancellationToken));
		results.Add(CheckDefaultAccount(configuration));

		return results;
	}

	private async Task<DoctorCheckResult> CheckAzureCliInstalledAsync(CancellationToken cancellationToken)
	{
		var result = await processRunner.RunAsync("az", "--version", cancellationToken);
		if (result.ExitCode == 0)
		{
			var version = ParseAzVersion(result.StandardOutput);
			return new DoctorCheckResult(
				"Azure CLI installed",
				true,
				version is not null ? $"v{version}" : "Installed");
		}

		return new DoctorCheckResult(
			"Azure CLI installed",
			false,
			"Azure CLI not found",
			"Install Azure CLI: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli");
	}

	private async Task<DoctorCheckResult> CheckAzureCliLoggedInAsync(CancellationToken cancellationToken)
	{
		var result = await processRunner.RunAsync("az", "account show --query user.name --output tsv", cancellationToken);
		if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput))
		{
			return new DoctorCheckResult(
				"Azure CLI logged in",
				true,
				result.StandardOutput.Trim());
		}

		return new DoctorCheckResult(
			"Azure CLI logged in",
			false,
			"Not logged in to Azure CLI",
			"Run 'az login' to authenticate with Azure");
	}

	private async Task<DoctorCheckResult> CheckKeyVaultAsync(ClawConfiguration? configuration, CancellationToken cancellationToken)
	{
		if (configuration is null)
		{
			return new DoctorCheckResult(
				"Key Vault reachable",
				false,
				"Skipped (config file not found or invalid)",
				"Fix the config file first");
		}

		var isReachable = await keyVaultChecker.IsReachableAsync(configuration.KeyVaultUri, cancellationToken);
		return new DoctorCheckResult(
			"Key Vault reachable",
			isReachable,
			isReachable ? configuration.KeyVaultUri : "Key Vault is not reachable",
			isReachable ? null : "Ensure the Key Vault URI is correct and run 'az login'");
	}

	private static DoctorCheckResult CheckDefaultAccount(ClawConfiguration? configuration)
	{
		if (!string.IsNullOrWhiteSpace(configuration?.DefaultAccount))
		{
			return new DoctorCheckResult(
				"Default account set",
				true,
				configuration.DefaultAccount);
		}

		return new DoctorCheckResult(
			"Default account set",
			false,
			"No default account configured",
			"Run 'claw-mail-cal-cli account set <name>' to set a default account");
	}

	private static string? ParseAzVersion(string output)
	{
		var match = Regex.Match(output, @"azure-cli\s+(\d+\.\d+\.\d+)");
		return match.Success ? match.Groups[1].Value : null;
	}
}
