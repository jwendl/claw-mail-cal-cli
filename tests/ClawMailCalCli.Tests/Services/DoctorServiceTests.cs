using ClawMailCalCli.Configuration;
using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="DoctorService"/>.
/// </summary>
[Trait("Category", "Unit")]
public class DoctorServiceTests
{
	private readonly Mock<IConfigurationService> _mockConfigurationService;
	private readonly Mock<IAzureCliChecker> _mockAzureCliChecker;
	private readonly Mock<IKeyVaultChecker> _mockKeyVaultChecker;
	private readonly Mock<IAccountService> _mockAccountService;
	private readonly DoctorService _doctorService;

	/// <summary>
	/// Initializes a new instance of <see cref="DoctorServiceTests"/> with mocked dependencies.
	/// </summary>
	public DoctorServiceTests()
	{
		_mockConfigurationService = new Mock<IConfigurationService>();
		_mockAzureCliChecker = new Mock<IAzureCliChecker>();
		_mockKeyVaultChecker = new Mock<IKeyVaultChecker>();
		_mockAccountService = new Mock<IAccountService>();
		_doctorService = new DoctorService(_mockConfigurationService.Object, _mockAzureCliChecker.Object, _mockKeyVaultChecker.Object, _mockAccountService.Object);
	}

	[Fact]
	public async Task RunAllChecksAsync_WhenAllChecksPass_ReturnsAllPassedResults()
	{
		// Arrange
		_mockAzureCliChecker
			.Setup(checker => checker.IsAuthenticatedAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		SetupConfigFileValid("https://my-kv.vault.azure.net/");
		_mockKeyVaultChecker
			.Setup(checker => checker.IsReachableAsync("https://my-kv.vault.azure.net/", It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		_mockAccountService
			.Setup(service => service.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new Account("work", "work@example.com", AccountType.Work));

		// Act
		var results = await _doctorService.RunAllChecksAsync();

		// Assert
		results.Should().HaveCount(4);
		results.Should().AllSatisfy(result => result.Passed.Should().BeTrue());
	}

	[Fact]
	public async Task RunAllChecksAsync_WhenConfigFileMissing_ReturnsFailedConfigCheck()
	{
		// Arrange
		_mockAzureCliChecker
			.Setup(checker => checker.IsAuthenticatedAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
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
		_mockAzureCliChecker
			.Setup(checker => checker.IsAuthenticatedAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
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
		_mockAzureCliChecker
			.Setup(checker => checker.IsAuthenticatedAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		SetupConfigFileValid("https://my-kv.vault.azure.net/");
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
		_mockAzureCliChecker
			.Setup(checker => checker.IsAuthenticatedAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		SetupConfigFileValid("https://my-kv.vault.azure.net/");
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
		_mockAzureCliChecker
			.Setup(checker => checker.IsAuthenticatedAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		SetupConfigFileValid("https://my-kv.vault.azure.net/");
		_mockKeyVaultChecker
			.Setup(checker => checker.IsReachableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		_mockAccountService
			.Setup(service => service.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new Account("work", "work@example.com", AccountType.Work));

		// Act
		var results = await _doctorService.RunAllChecksAsync();

		// Assert
		var accountCheck = results.First(result => result.CheckName == "Default account set");
		accountCheck.Passed.Should().BeTrue();
		accountCheck.Message.Should().Be("work");
	}

	[Fact]
	public async Task RunAllChecksAsync_WhenConfigFileMissing_DefaultAccountCheckQueriesDatabase()
	{
		// Arrange
		_mockAzureCliChecker
			.Setup(checker => checker.IsAuthenticatedAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		_mockConfigurationService
			.Setup(service => service.ReadConfigurationAsync())
			.ThrowsAsync(new InvalidOperationException("Configuration file not found."));
		_mockAccountService
			.Setup(service => service.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync((Account?)null);

		// Act
		var results = await _doctorService.RunAllChecksAsync();

		// Assert
		var accountCheck = results.First(result => result.CheckName == "Default account set");
		accountCheck.Passed.Should().BeFalse();
		accountCheck.Message.Should().Be("No default account configured");
		accountCheck.FixHint.Should().Contain("account set");
	}

	[Fact]
	public async Task RunAllChecksAsync_WhenNoDefaultAccount_ReturnsFailedAccountCheck()
	{
		// Arrange
		_mockAzureCliChecker
			.Setup(checker => checker.IsAuthenticatedAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		SetupConfigFileValid("https://my-kv.vault.azure.net/");
		_mockKeyVaultChecker
			.Setup(checker => checker.IsReachableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		_mockAccountService
			.Setup(service => service.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync((Account?)null);

		// Act
		var results = await _doctorService.RunAllChecksAsync();

		// Assert
		var accountCheck = results.First(result => result.CheckName == "Default account set");
		accountCheck.Passed.Should().BeFalse();
		accountCheck.FixHint.Should().Contain("account set");
	}

	private void SetupConfigFileValid(string keyVaultUri)
	{
		_mockConfigurationService
			.Setup(service => service.ReadConfigurationAsync())
			.ReturnsAsync(new ClawConfiguration(keyVaultUri));
	}
}
