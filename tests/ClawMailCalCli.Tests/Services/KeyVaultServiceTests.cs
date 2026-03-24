using Azure;
using Azure.Security.KeyVault.Secrets;
using ClawMailCalCli.Services;
using Microsoft.Extensions.Logging;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="KeyVaultService"/>.
/// </summary>
[Trait("Category", "Unit")]
public class KeyVaultServiceTests
{
	private readonly Mock<SecretClient> _mockSecretClient;
	private readonly Mock<ILogger<KeyVaultService>> _mockLogger;

	public KeyVaultServiceTests()
	{
		_mockSecretClient = new Mock<SecretClient>();
		_mockLogger = new Mock<ILogger<KeyVaultService>>();
	}

	private KeyVaultService CreateKeyVaultService() =>
		new KeyVaultService(_mockSecretClient.Object, _mockLogger.Object);

	[Fact]
	public async Task GetSecretAsync_WhenSecretExists_ReturnsValue()
	{
		// Arrange
		var secretName = "my-secret";
		var secretValue = "my-value";
		var keyVaultSecret = new KeyVaultSecret(secretName, secretValue);
		var response = Response.FromValue(keyVaultSecret, Mock.Of<Response>());

		_mockSecretClient
			.Setup(secretClient => secretClient.GetSecretAsync(secretName, It.IsAny<string>(), It.IsAny<SecretContentType?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(response);

		var keyVaultService = CreateKeyVaultService();

		// Act
		var result = await keyVaultService.GetSecretAsync(secretName);

		// Assert
		result.Should().Be(secretValue);
	}

	[Fact]
	public async Task GetSecretAsync_WhenSecretNotFound_ReturnsNull()
	{
		// Arrange
		var secretName = "missing-secret";
		_mockSecretClient
			.Setup(secretClient => secretClient.GetSecretAsync(secretName, It.IsAny<string>(), It.IsAny<SecretContentType?>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new RequestFailedException(404, "Secret not found"));

		var keyVaultService = CreateKeyVaultService();

		// Act
		var result = await keyVaultService.GetSecretAsync(secretName);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetSecretAsync_WhenDebugLoggingEnabled_LogsSecretName()
	{
		// Arrange
		var secretName = "logged-secret";
		var secretValue = "value";
		var keyVaultSecret = new KeyVaultSecret(secretName, secretValue);
		var response = Response.FromValue(keyVaultSecret, Mock.Of<Response>());

		_mockSecretClient
			.Setup(secretClient => secretClient.GetSecretAsync(secretName, It.IsAny<string>(), It.IsAny<SecretContentType?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(response);

		_mockLogger
			.Setup(logger => logger.IsEnabled(LogLevel.Debug))
			.Returns(true);

		var keyVaultService = CreateKeyVaultService();

		// Act
		await keyVaultService.GetSecretAsync(secretName);

		// Assert — IsEnabled(Debug) was consulted before logging
		_mockLogger.Verify(
			logger => logger.IsEnabled(LogLevel.Debug),
			Times.Once);
	}

	[Fact]
	public async Task GetSecretAsync_WhenDebugLoggingDisabled_DoesNotLog()
	{
		// Arrange
		var secretName = "silent-secret";
		var secretValue = "value";
		var keyVaultSecret = new KeyVaultSecret(secretName, secretValue);
		var response = Response.FromValue(keyVaultSecret, Mock.Of<Response>());

		_mockSecretClient
			.Setup(secretClient => secretClient.GetSecretAsync(secretName, It.IsAny<string>(), It.IsAny<SecretContentType?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(response);

		_mockLogger
			.Setup(logger => logger.IsEnabled(LogLevel.Debug))
			.Returns(false);

		var keyVaultService = CreateKeyVaultService();

		// Act
		await keyVaultService.GetSecretAsync(secretName);

		// Assert — Log() must not be called when IsEnabled returns false
		_mockLogger.Verify(
			logger => logger.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception?>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Never);
	}

	[Fact]
	public async Task SetSecretAsync_WhenDebugLoggingEnabled_LogsSecretName()
	{
		// Arrange
		var secretName = "write-secret";
		var secretValue = "write-value";
		var keyVaultSecret = new KeyVaultSecret(secretName, secretValue);
		var response = Response.FromValue(keyVaultSecret, Mock.Of<Response>());

		_mockSecretClient
			.Setup(secretClient => secretClient.SetSecretAsync(secretName, secretValue, It.IsAny<CancellationToken>()))
			.ReturnsAsync(response);

		_mockLogger
			.Setup(logger => logger.IsEnabled(LogLevel.Debug))
			.Returns(true);

		var keyVaultService = CreateKeyVaultService();

		// Act
		await keyVaultService.SetSecretAsync(secretName, secretValue);

		// Assert — IsEnabled(Debug) was consulted before logging
		_mockLogger.Verify(
			logger => logger.IsEnabled(LogLevel.Debug),
			Times.Once);
	}

	[Fact]
	public async Task SetSecretAsync_WhenDebugLoggingDisabled_DoesNotLog()
	{
		// Arrange
		var secretName = "silent-write";
		var secretValue = "silent-value";
		var keyVaultSecret = new KeyVaultSecret(secretName, secretValue);
		var response = Response.FromValue(keyVaultSecret, Mock.Of<Response>());

		_mockSecretClient
			.Setup(secretClient => secretClient.SetSecretAsync(secretName, secretValue, It.IsAny<CancellationToken>()))
			.ReturnsAsync(response);

		_mockLogger
			.Setup(logger => logger.IsEnabled(LogLevel.Debug))
			.Returns(false);

		var keyVaultService = CreateKeyVaultService();

		// Act
		await keyVaultService.SetSecretAsync(secretName, secretValue);

		// Assert — Log() must not be called when IsEnabled returns false
		_mockLogger.Verify(
			logger => logger.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception?>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Never);
	}
}
