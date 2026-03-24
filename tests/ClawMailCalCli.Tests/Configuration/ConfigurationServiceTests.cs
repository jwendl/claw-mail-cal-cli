using System.Text.Json;
using ClawMailCalCli.Configuration;

namespace ClawMailCalCli.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="ConfigurationService"/>.
/// </summary>
[Trait("Category", "Unit")]
public class ConfigurationServiceTests : IDisposable
{
	private readonly string _tempDirectory;
	private bool _disposed;

	/// <summary>
	/// Initializes a new instance of <see cref="ConfigurationServiceTests"/> using a
	/// temporary directory to isolate file system access.
	/// </summary>
	public ConfigurationServiceTests()
	{
		_tempDirectory = Path.Combine(Path.GetTempPath(), $"claw-mail-cal-cli-tests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(_tempDirectory);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		if (Directory.Exists(_tempDirectory))
		{
			Directory.Delete(_tempDirectory, recursive: true);
		}

		_disposed = true;
		GC.SuppressFinalize(this);
	}

	private ConfigurationService CreateService(string? overrideConfigPath = null)
	{
		return new ConfigurationService(overrideConfigPath ?? _tempDirectory);
	}

	// -------------------------------------------------------------------------
	// ReadConfigurationAsync
	// -------------------------------------------------------------------------

	[Fact]
	public async Task ReadConfigurationAsync_WhenConfigFileExists_ReturnsConfiguration()
	{
		// Arrange
		var configFilePath = Path.Combine(_tempDirectory, "config.json");
		var expectedConfiguration = new ClawConfiguration("https://my-keyvault.vault.azure.net/", "work");
		var json = JsonSerializer.Serialize(expectedConfiguration, new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true,
		});
		await File.WriteAllTextAsync(configFilePath, json);
		var configurationService = CreateService();

		// Act
		var result = await configurationService.ReadConfigurationAsync();

		// Assert
		result.KeyVaultUri.Should().Be("https://my-keyvault.vault.azure.net/");
		result.DefaultAccount.Should().Be("work");
	}

	[Fact]
	public async Task ReadConfigurationAsync_WhenConfigFileExistsWithoutDefaultAccount_ReturnsConfigurationWithNullDefaultAccount()
	{
		// Arrange
		var configFilePath = Path.Combine(_tempDirectory, "config.json");
		var json = """{"keyVaultUri": "https://vault.azure.net/"}""";
		await File.WriteAllTextAsync(configFilePath, json);
		var configurationService = CreateService();

		// Act
		var result = await configurationService.ReadConfigurationAsync();

		// Assert
		result.KeyVaultUri.Should().Be("https://vault.azure.net/");
		result.DefaultAccount.Should().BeNull();
	}

	[Fact]
	public async Task ReadConfigurationAsync_WhenConfigFileDoesNotExist_ThrowsInvalidOperationException()
	{
		// Arrange
		var configurationService = CreateService();

		// Act
		var act = async () => await configurationService.ReadConfigurationAsync();

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*config.json*");
	}

	[Fact]
	public async Task ReadConfigurationAsync_WhenConfigFileDoesNotExist_ErrorMessageContainsKeyVaultUriHint()
	{
		// Arrange
		var configurationService = CreateService();

		// Act
		var act = async () => await configurationService.ReadConfigurationAsync();

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*keyVaultUri*");
	}

	[Fact]
	public async Task ReadConfigurationAsync_WhenConfigFileContainsInvalidJson_ThrowsJsonException()
	{
		// Arrange
		var configFilePath = Path.Combine(_tempDirectory, "config.json");
		await File.WriteAllTextAsync(configFilePath, "not valid json {{{");
		var configurationService = CreateService();

		// Act
		var act = async () => await configurationService.ReadConfigurationAsync();

		// Assert
		await act.Should().ThrowAsync<System.Text.Json.JsonException>();
	}

	[Fact]
	public async Task ReadConfigurationAsync_WhenConfigFileContainsNullJson_ThrowsInvalidOperationException()
	{
		// Arrange
		var configFilePath = Path.Combine(_tempDirectory, "config.json");
		await File.WriteAllTextAsync(configFilePath, "null");
		var configurationService = CreateService();

		// Act
		var act = async () => await configurationService.ReadConfigurationAsync();

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*could not be parsed*");
	}

	// -------------------------------------------------------------------------
	// WriteConfigurationAsync
	// -------------------------------------------------------------------------

	[Fact]
	public async Task WriteConfigurationAsync_WhenCalled_WritesConfigFileToExpectedPath()
	{
		// Arrange
		var configurationService = CreateService();
		var configuration = new ClawConfiguration("https://my-keyvault.vault.azure.net/", "work");

		// Act
		await configurationService.WriteConfigurationAsync(configuration);

		// Assert
		var configFilePath = Path.Combine(_tempDirectory, "config.json");
		File.Exists(configFilePath).Should().BeTrue();
	}

	[Fact]
	public async Task WriteConfigurationAsync_WhenCalled_FileContainsExpectedKeyVaultUri()
	{
		// Arrange
		var configurationService = CreateService();
		var configuration = new ClawConfiguration("https://my-keyvault.vault.azure.net/", "work");

		// Act
		await configurationService.WriteConfigurationAsync(configuration);

		// Assert
		var configFilePath = Path.Combine(_tempDirectory, "config.json");
		var json = await File.ReadAllTextAsync(configFilePath);
		json.Should().Contain("https://my-keyvault.vault.azure.net/");
	}

	[Fact]
	public async Task WriteConfigurationAsync_WhenCalled_FileContainsExpectedDefaultAccount()
	{
		// Arrange
		var configurationService = CreateService();
		var configuration = new ClawConfiguration("https://my-keyvault.vault.azure.net/", "work");

		// Act
		await configurationService.WriteConfigurationAsync(configuration);

		// Assert
		var configFilePath = Path.Combine(_tempDirectory, "config.json");
		var json = await File.ReadAllTextAsync(configFilePath);
		json.Should().Contain("work");
	}

	[Fact]
	public async Task WriteConfigurationAsync_ThenReadConfigurationAsync_RoundTripsSuccessfully()
	{
		// Arrange
		var configurationService = CreateService();
		var originalConfiguration = new ClawConfiguration("https://vault.azure.net/", "personal");

		// Act
		await configurationService.WriteConfigurationAsync(originalConfiguration);
		var readConfiguration = await configurationService.ReadConfigurationAsync();

		// Assert
		readConfiguration.KeyVaultUri.Should().Be(originalConfiguration.KeyVaultUri);
		readConfiguration.DefaultAccount.Should().Be(originalConfiguration.DefaultAccount);
	}

	[Fact]
	public async Task WriteConfigurationAsync_WhenDirectoryDoesNotExist_CreatesDirectoryAndWritesFile()
	{
		// Arrange
		var nestedDirectory = Path.Combine(_tempDirectory, "nested", "deep");
		var configurationService = CreateService(nestedDirectory);
		var configuration = new ClawConfiguration("https://vault.azure.net/");

		// Act
		await configurationService.WriteConfigurationAsync(configuration);

		// Assert
		var configFilePath = Path.Combine(nestedDirectory, "config.json");
		File.Exists(configFilePath).Should().BeTrue();
	}

	[Fact]
	public async Task WriteConfigurationAsync_WhenDefaultAccountIsNull_OmitsDefaultAccountFromJson()
	{
		// Arrange
		var configurationService = CreateService();
		var configuration = new ClawConfiguration("https://vault.azure.net/");

		// Act
		await configurationService.WriteConfigurationAsync(configuration);

		// Assert
		var configFilePath = Path.Combine(_tempDirectory, "config.json");
		var json = await File.ReadAllTextAsync(configFilePath);
		json.Should().NotContain("defaultAccount");
	}

	[Fact]
	public async Task WriteConfigurationAsync_WhenDefaultAccountIsNull_RoundTripsToNullDefaultAccount()
	{
		// Arrange
		var configurationService = CreateService();
		var configuration = new ClawConfiguration("https://vault.azure.net/");

		// Act
		await configurationService.WriteConfigurationAsync(configuration);
		var readConfiguration = await configurationService.ReadConfigurationAsync();

		// Assert
		readConfiguration.DefaultAccount.Should().BeNull();
	}

	// -------------------------------------------------------------------------
	// KeyVaultUri validation
	// -------------------------------------------------------------------------

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("not-a-uri")]
	[InlineData("/relative/path")]
	[InlineData("http://insecure-vault.vault.azure.net/")]
	[InlineData("ftp://vault.azure.net/")]
	public async Task ReadConfigurationAsync_WhenKeyVaultUriIsInvalid_ThrowsInvalidOperationException(string invalidUri)
	{
		// Arrange
		var configFilePath = Path.Combine(_tempDirectory, "config.json");
		var json = $$"""{"keyVaultUri": "{{invalidUri}}"}""";
		await File.WriteAllTextAsync(configFilePath, json);
		var configurationService = CreateService();

		// Act
		var act = async () => await configurationService.ReadConfigurationAsync();

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*keyVaultUri*");
	}

	[Fact]
	public async Task ReadConfigurationAsync_WhenKeyVaultUriIsNull_ThrowsInvalidOperationException()
	{
		// Arrange
		var configFilePath = Path.Combine(_tempDirectory, "config.json");
		var json = """{"keyVaultUri": null}""";
		await File.WriteAllTextAsync(configFilePath, json);
		var configurationService = CreateService();

		// Act
		var act = async () => await configurationService.ReadConfigurationAsync();

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*keyVaultUri*");
	}
}
