namespace ClawMailCalCli.Configuration;

/// <summary>
/// Provides access to read and write the claw-mail-cal-cli configuration file.
/// </summary>
public interface IConfigurationService
{
	/// <summary>
	/// Reads the configuration from the user's home directory.
	/// </summary>
	/// <returns>The loaded <see cref="ClawConfiguration"/>.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the configuration file does not exist or when the configuration content cannot be deserialized into a valid <see cref="ClawConfiguration"/> instance.</exception>
	/// <exception cref="System.Text.Json.JsonException">Thrown when the configuration file contains invalid JSON.</exception>
	Task<ClawConfiguration> ReadConfigurationAsync();

	/// <summary>
	/// Writes the configuration to the user's home directory.
	/// </summary>
	/// <param name="configuration">The configuration to persist.</param>
	Task WriteConfigurationAsync(ClawConfiguration configuration);
}
