using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClawMailCalCli.Configuration;

/// <summary>
/// Reads and writes the claw-mail-cal-cli configuration file located at
/// <c>~/.claw-mail-cal-cli/config.json</c>.
/// </summary>
public class ConfigurationService
	: IConfigurationService
{
	private static readonly string DefaultConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claw-mail-cal-cli");

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	private readonly string _configDirectory;
	private readonly string _configFilePath;

	/// <summary>
	/// Initializes a new instance of <see cref="ConfigurationService"/> using the default
	/// configuration directory (<c>~/.claw-mail-cal-cli</c>).
	/// </summary>
	public ConfigurationService()
		: this(DefaultConfigDirectory)
	{
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ConfigurationService"/> using the specified
	/// configuration directory. Intended for use in tests.
	/// </summary>
	/// <param name="configDirectory">The directory in which <c>config.json</c> is stored.</param>
	public ConfigurationService(string configDirectory)
	{
		_configDirectory = configDirectory;
		_configFilePath = Path.Combine(_configDirectory, "config.json");
	}

	/// <inheritdoc />
	public async Task<ClawConfiguration> ReadConfigurationAsync()
	{
		if (!File.Exists(_configFilePath))
		{
			throw new InvalidOperationException($"Configuration file not found at '{_configFilePath}'. Please create it with a valid 'keyVaultUri' entry. Example: {{\"keyVaultUri\": \"https://my-keyvault.vault.azure.net/\"}}");
		}

		var json = await File.ReadAllTextAsync(_configFilePath);
		var configuration = JsonSerializer.Deserialize<ClawConfiguration>(json, JsonOptions);

		if (configuration is null)
		{
			throw new InvalidOperationException(
				$"Configuration file at '{_configFilePath}' could not be parsed. " +
				$"Ensure it contains a valid JSON object with a 'keyVaultUri' property.");
		}

		if (string.IsNullOrWhiteSpace(configuration.KeyVaultUri) ||
			!Uri.TryCreate(configuration.KeyVaultUri, UriKind.Absolute, out var keyVaultUri) ||
			!string.Equals(keyVaultUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException($"Configuration file at '{_configFilePath}' contains an invalid 'keyVaultUri' value. The value must be an absolute HTTPS URI. Example: {{\"keyVaultUri\": \"https://my-keyvault.vault.azure.net/\"}}");
		}

		return configuration;
	}

	/// <inheritdoc />
	public async Task WriteConfigurationAsync(ClawConfiguration configuration)
	{
		Directory.CreateDirectory(_configDirectory);
		var json = JsonSerializer.Serialize(configuration, JsonOptions);
		await File.WriteAllTextAsync(_configFilePath, json);
	}
}
