using ClawMailCalCli.Configuration;
using ClawMailCalCli.Models;
using ClawMailCalCli.Services;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="DoctorService"/>.
/// </summary>
[Trait("Category", "Unit")]
public class DoctorServiceTests
{
	private readonly Mock<IProcessRunner> _mockProcessRunner;
	private readonly Mock<IConfigurationService> _mockConfigurationService;
	private readonly Mock<IKeyVaultChecker> _mockKeyVaultChecker;
	private readonly DoctorService _doctorService;

	/// <summary>
	/// Initializes a new instance of <see cref="DoctorServiceTests"/> with mocked dependencies.
	/// </summary>
	public DoctorServiceTests()
	{
		_mockProcessRunner = new Mock<IProcessRunner>();
		_mockConfigurationService = new Mock<IConfigurationService>();
		_mockKeyVaultChecker = new Mock<IKeyVaultChecker>();
		_doctorService = new DoctorService(_mockProcessRunner.Object, _mockConfigurationService.Object, _mockKeyVaultChecker.Object);
	}

	[Fact]
	public async Task RunAllChecksAsync_WhenAllChecksPass_ReturnsAllPassedResults()
	{
		// Arrange
		SetupAzCliInstalled("2.58.0");
		SetupAzCliLoggedIn("user@example.com");
		SetupConfigFileValid("https://my-kv.vault.azure.net/", "work");
		_mockKeyVaultChecker
			.Setup(checker => checker.IsReachableAsync("https://my-kv.vault.azure.net/", It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		// Act
		var results = await _doctorService.RunAllChecksAsync();

		// Assert
		results.Should().HaveCount(5);
		results.Should().AllSatisfy(result => result.Passed.Should().BeTrue());
	}

	[Fact]
	public async Task RunAllChecksAsync_WhenAzureCliNotInstalled_ReturnsFailedAzureCliCheck()
	{
		// Arrange
		_mockProcessRunner
			.Setup(runner => runner.RunAsync("az", "--version", It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ProcessResult(-1, string.Empty, string.Empty));
		SetupConfigFileValid("https://my-kv.vault.azure.net/", "work");
		_mockKeyVaultChecker
			.Setup(checker => checker.IsReachableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		// Act
		var results = await _doctorService.RunAllChecksAsync();

		// Assert
		var azureCliCheck = results.First(result => result.CheckName == "Azure CLI installed");
		azureCliCheck.Passed.Should().BeFalse();
		azureCliCheck.FixHint.Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public async Task RunAllChecksAsync_WhenAzureCliNotInstalled_LoginCheckIsSkipped()
	{
		// Arrange
		_mockProcessRunner
			.Setup(runner => runner.RunAsync("az", "--version", It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ProcessResult(-1, string.Empty, string.Empty));
		SetupConfigFileValid("https://my-kv.vault.azure.net/", "work");
		_mockKeyVaultChecker
			.Setup(checker => checker.IsReachableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		// Act
		var results = await _doctorService.RunAllChecksAsync();

		// Assert
		var loginCheck = results.First(result => result.CheckName == "Azure CLI logged in");
		loginCheck.Passed.Should().BeFalse();
		loginCheck.Message.Should().Contain("Skipped");
		_mockProcessRunner.Verify(runner => runner.RunAsync("az", "account show --query user.name --output tsv", It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task RunAllChecksAsync_WhenAzureCliInstalled_IncludesVersionInMessage()
	{
		// Arrange
		SetupAzCliInstalled("2.58.0");
		SetupAzCliLoggedIn("user@example.com");
		SetupConfigFileValid("https://my-kv.vault.azure.net/", "work");
		_mockKeyVaultChecker
			.Setup(checker => checker.IsReachableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		// Act
		var results = await _doctorService.RunAllChecksAsync();

		// Assert
		var azureCliCheck = results.First(result => result.CheckName == "Azure CLI installed");
		azureCliCheck.Passed.Should().BeTrue();
		azureCliCheck.Message.Should().Contain("2.58.0");
	}

	[Fact]
	public async Task RunAllChecksAsync_WhenAzureCliNotLoggedIn_ReturnsFailedLoginCheck()
	{
		// Arrange
		SetupAzCliInstalled("2.58.0");
		_mockProcessRunner
			.Setup(runner => runner.RunAsync("az", "account show --query user.name --output tsv", It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ProcessResult(1, string.Empty, "ERROR: Please run 'az login'"));
		SetupConfigFileValid("https://my-kv.vault.azure.net/", "work");
		_mockKeyVaultChecker
			.Setup(checker => checker.IsReachableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		// Act
		var results = await _doctorService.RunAllChecksAsync();

		// Assert
		var loginCheck = results.First(result => result.CheckName == "Azure CLI logged in");
		loginCheck.Passed.Should().BeFalse();
		loginCheck.FixHint.Should().Contain("az login");
	}

	[Fact]
	public async Task RunAllChecksAsync_WhenAzureCliLoggedIn_IncludesUserNameInMessage()
	{
		// Arrange
		SetupAzCliInstalled("2.58.0");
		SetupAzCliLoggedIn("user@example.com");
		SetupConfigFileValid("https://my-kv.vault.azure.net/", "work");
		_mockKeyVaultChecker
			.Setup(checker => checker.IsReachableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		// Act
		var results = await _doctorService.RunAllChecksAsync();

		// Assert
		var loginCheck = results.First(result => result.CheckName == "Azure CLI logged in");
		loginCheck.Passed.Should().BeTrue();
		loginCheck.Message.Should().Be("user@example.com");
	}

	[Fact]
	public async Task RunAllChecksAsync_WhenConfigFileMissing_ReturnsFailedConfigCheck()
	{
		// Arrange
		SetupAzCliInstalled("2.58.0");
		SetupAzCliLoggedIn("user@example.com");
		_mockConfigurationService
			.Setup(service => service.ReadConfigurationAsync())
			.ThrowsAsync(new InvalidOperationException("Configuration file not found."));
		_mockKeyVaultChecker
			.Setup(checker => checker.IsReachableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		// Act
		var results = await _doctorService.RunAllChecksAsync();

		// Assert
		var configCheck = results.First(result => result.CheckName == "Config file found");
		configCheck.Passed.Should().BeFalse();
		configCheck.FixHint.Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public async Task RunAllChecksAsync_WhenConfigFileMissing_KeyVaultCheckIsSkipped()
	{
		// Arrange
		SetupAzCliInstalled("2.58.0");
		SetupAzCliLoggedIn("user@example.com");
		_mockConfigurationService
			.Setup(service => service.ReadConfigurationAsync())
			.ThrowsAsync(new InvalidOperationException("Configuration file not found."));

		// Act
		var results = await _doctorService.RunAllChecksAsync();

		// Assert
		var keyVaultCheck = results.First(result => result.CheckName == "Key Vault reachable");
		keyVaultCheck.Passed.Should().BeFalse();
		keyVaultCheck.Message.Should().Contain("Skipped");
		_mockKeyVaultChecker.Verify(checker => checker.IsReachableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task RunAllChecksAsync_WhenKeyVaultReachable_ReturnsPassedKeyVaultCheck()
	{
		// Arrange
		SetupAzCliInstalled("2.58.0");
		SetupAzCliLoggedIn("user@example.com");
		SetupConfigFileValid("https://my-kv.vault.azure.net/", null);
		_mockKeyVaultChecker
			.Setup(checker => checker.IsReachableAsync("https://my-kv.vault.azure.net/", It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		// Act
		var results = await _doctorService.RunAllChecksAsync();

		// Assert
		var keyVaultCheck = results.First(result => result.CheckName == "Key Vault reachable");
		keyVaultCheck.Passed.Should().BeTrue();
		keyVaultCheck.Message.Should().Be("https://my-kv.vault.azure.net/");
	}

	[Fact]
	public async Task RunAllChecksAsync_WhenKeyVaultNotReachable_ReturnsFailedKeyVaultCheck()
	{
		// Arrange
		SetupAzCliInstalled("2.58.0");
		SetupAzCliLoggedIn("user@example.com");
		SetupConfigFileValid("https://my-kv.vault.azure.net/", null);
		_mockKeyVaultChecker
			.Setup(checker => checker.IsReachableAsync("https://my-kv.vault.azure.net/", It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		// Act
		var results = await _doctorService.RunAllChecksAsync();

		// Assert
		var keyVaultCheck = results.First(result => result.CheckName == "Key Vault reachable");
		keyVaultCheck.Passed.Should().BeFalse();
		keyVaultCheck.FixHint.Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public async Task RunAllChecksAsync_WhenDefaultAccountSet_ReturnsPassedAccountCheck()
	{
		// Arrange
		SetupAzCliInstalled("2.58.0");
		SetupAzCliLoggedIn("user@example.com");
		SetupConfigFileValid("https://my-kv.vault.azure.net/", "work");
		_mockKeyVaultChecker
			.Setup(checker => checker.IsReachableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		// Act
		var results = await _doctorService.RunAllChecksAsync();

		// Assert
		var accountCheck = results.First(result => result.CheckName == "Default account set");
		accountCheck.Passed.Should().BeTrue();
		accountCheck.Message.Should().Be("work");
	}

	[Fact]
	public async Task RunAllChecksAsync_WhenConfigFileMissing_DefaultAccountCheckIsSkipped()
	{
		// Arrange
		SetupAzCliInstalled("2.58.0");
		SetupAzCliLoggedIn("user@example.com");
		_mockConfigurationService
			.Setup(service => service.ReadConfigurationAsync())
			.ThrowsAsync(new InvalidOperationException("Configuration file not found."));

		// Act
		var results = await _doctorService.RunAllChecksAsync();

		// Assert
		var accountCheck = results.First(result => result.CheckName == "Default account set");
		accountCheck.Passed.Should().BeFalse();
		accountCheck.Message.Should().Contain("Skipped");
	}

	[Fact]
	public async Task RunAllChecksAsync_WhenNoDefaultAccount_ReturnsFailedAccountCheck()
	{
		// Arrange
		SetupAzCliInstalled("2.58.0");
		SetupAzCliLoggedIn("user@example.com");
		SetupConfigFileValid("https://my-kv.vault.azure.net/", null);
		_mockKeyVaultChecker
			.Setup(checker => checker.IsReachableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		// Act
		var results = await _doctorService.RunAllChecksAsync();

		// Assert
		var accountCheck = results.First(result => result.CheckName == "Default account set");
		accountCheck.Passed.Should().BeFalse();
		accountCheck.FixHint.Should().Contain("account set");
	}

	[Fact]
	public async Task RunAllChecksAsync_WhenAllChecksFail_ReturnsFiveResults()
	{
		// Arrange
		_mockProcessRunner
			.Setup(runner => runner.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ProcessResult(-1, string.Empty, string.Empty));
		_mockConfigurationService
			.Setup(service => service.ReadConfigurationAsync())
			.ThrowsAsync(new InvalidOperationException("Config not found."));

		// Act
		var results = await _doctorService.RunAllChecksAsync();

		// Assert
		results.Should().HaveCount(5);
		results.Should().AllSatisfy(result => result.Passed.Should().BeFalse());
	}

	private void SetupAzCliInstalled(string version)
	{
		_mockProcessRunner
			.Setup(runner => runner.RunAsync("az", "--version", It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ProcessResult(0, $"azure-cli                         {version}", string.Empty));
	}

	private void SetupAzCliLoggedIn(string userName)
	{
		_mockProcessRunner
			.Setup(runner => runner.RunAsync("az", "account show --query user.name --output tsv", It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ProcessResult(0, userName + "\n", string.Empty));
	}

	private void SetupConfigFileValid(string keyVaultUri, string? defaultAccount)
	{
		_mockConfigurationService
			.Setup(service => service.ReadConfigurationAsync())
			.ReturnsAsync(new ClawConfiguration(keyVaultUri, defaultAccount));
	}
}
