using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
using Microsoft.Extensions.Logging;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="GraphServiceClientBuilder"/>.
/// </summary>
[Trait("Category", "Unit")]
public class GraphServiceClientBuilderTests
{
	private readonly Mock<IKeyVaultService> _mockKeyVaultService;
	private readonly ILogger<GraphServiceClientBuilder> _logger;

	public GraphServiceClientBuilderTests()
	{
		_mockKeyVaultService = new Mock<IKeyVaultService>();
		_logger = new NullLogger<GraphServiceClientBuilder>();
	}

	private GraphServiceClientBuilder CreateGraphServiceClientBuilder() =>
		new GraphServiceClientBuilder(_mockKeyVaultService.Object, _logger);

	[Fact]
	public async Task BuildAsync_WhenAuthRecordSecretIsNull_ReturnsNull()
	{
		// Arrange
		var account = new Account("test-account", "test@example.com", AccountType.Work);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-test-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var graphServiceClientBuilder = CreateGraphServiceClientBuilder();

		// Act
		var result = await graphServiceClientBuilder.BuildAsync(account);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task BuildAsync_WhenAuthRecordSecretIsEmpty_ReturnsNull()
	{
		// Arrange
		var account = new Account("test-account", "test@example.com", AccountType.Work);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-test-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(string.Empty);

		var graphServiceClientBuilder = CreateGraphServiceClientBuilder();

		// Act
		var result = await graphServiceClientBuilder.BuildAsync(account);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task BuildAsync_WhenAuthRecordIsInvalidBase64_ReturnsNull()
	{
		// Arrange
		var account = new Account("test-account", "test@example.com", AccountType.Work);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-test-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync("this-is-not-valid-base64!!!");

		var graphServiceClientBuilder = CreateGraphServiceClientBuilder();

		// Act
		var result = await graphServiceClientBuilder.BuildAsync(account);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task BuildAsync_WhenAuthRecordIsValidBase64ButNotAuthRecord_ReturnsNull()
	{
		// Arrange
		var account = new Account("test-account", "test@example.com", AccountType.Work);
		var invalidRecordBase64 = Convert.ToBase64String("not-a-valid-auth-record"u8.ToArray());

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-test-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(invalidRecordBase64);

		var graphServiceClientBuilder = CreateGraphServiceClientBuilder();

		// Act
		var result = await graphServiceClientBuilder.BuildAsync(account);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task BuildAsync_WhenAuthRecordDeserializationFails_DoesNotFetchClientIdSecret()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		var validBase64NotAuthRecord = Convert.ToBase64String("not-a-valid-auth-record-json"u8.ToArray());

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(validBase64NotAuthRecord);

		var graphServiceClientBuilder = CreateGraphServiceClientBuilder();

		// Act
		var result = await graphServiceClientBuilder.BuildAsync(account);

		// Assert — deserialization of the auth record fails, so the method returns null before ever fetching client-id
		result.Should().BeNull();
		_mockKeyVaultService.Verify(
			service => service.GetSecretAsync("exchange-client-id", It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Theory]
	[InlineData(AccountType.Personal, "hotmail")]
	[InlineData(AccountType.Work, "exchange")]
	public async Task BuildAsync_WhenAuthRecordSecretIsNull_DoesNotFetchClientIdSecret(AccountType accountType, string expectedPrefix)
	{
		// Arrange
		var account = new Account("test-account", "test@example.com", accountType);

		_mockKeyVaultService
			.Setup(service => service.GetSecretAsync("auth-record-test-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync((string?)null);

		var graphServiceClientBuilder = CreateGraphServiceClientBuilder();

		// Act
		await graphServiceClientBuilder.BuildAsync(account);

		// Assert — client-id secret should not be fetched when auth record is missing
		_mockKeyVaultService.Verify(
			service => service.GetSecretAsync($"{expectedPrefix}-client-id", It.IsAny<CancellationToken>()),
			Times.Never);
	}
}
