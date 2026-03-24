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
	[Fact]
	public async Task GetSecretAsync_WhenSecretExists_ReturnsSecretValue()
	{
		// Arrange
		var secretName = "my-secret";
		var secretValue = "secret-value-123";
		var keyVaultSecret = SecretModelFactory.KeyVaultSecret(new SecretProperties(secretName), secretValue);
		var fakeClient = new FakeSecretClient(Response.FromValue(keyVaultSecret, Mock.Of<Response>()));
		var keyVaultService = new KeyVaultService(fakeClient, Mock.Of<ILogger<KeyVaultService>>());

		// Act
		var result = await keyVaultService.GetSecretAsync(secretName);

		// Assert
		result.Should().Be(secretValue);
	}

	[Fact]
	public async Task GetSecretAsync_WhenSecretNotFound_ReturnsNull()
	{
		// Arrange
		var fakeClient = new FakeSecretClient(new RequestFailedException(404, "Secret not found"));
		var keyVaultService = new KeyVaultService(fakeClient, Mock.Of<ILogger<KeyVaultService>>());

		// Act
		var result = await keyVaultService.GetSecretAsync("nonexistent-secret");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetSecretAsync_WhenDebugLoggingEnabled_LogsSecretName()
	{
		// Arrange
		var secretName = "logged-secret";
		var secretValue = "value";
		var keyVaultSecret = SecretModelFactory.KeyVaultSecret(new SecretProperties(secretName), secretValue);
		var fakeClient = new FakeSecretClient(Response.FromValue(keyVaultSecret, Mock.Of<Response>()));
		var mockLogger = new Mock<ILogger<KeyVaultService>>();
		mockLogger.Setup(logger => logger.IsEnabled(LogLevel.Debug)).Returns(true);
		var keyVaultService = new KeyVaultService(fakeClient, mockLogger.Object);

		// Act
		await keyVaultService.GetSecretAsync(secretName);

		// Assert — IsEnabled(Debug) was consulted before logging
		mockLogger.Verify(
			logger => logger.IsEnabled(LogLevel.Debug),
			Times.Once);
	}

	[Fact]
	public async Task GetSecretAsync_WhenDebugLoggingDisabled_DoesNotLog()
	{
		// Arrange
		var secretName = "silent-secret";
		var secretValue = "value";
		var keyVaultSecret = SecretModelFactory.KeyVaultSecret(new SecretProperties(secretName), secretValue);
		var fakeClient = new FakeSecretClient(Response.FromValue(keyVaultSecret, Mock.Of<Response>()));
		var mockLogger = new Mock<ILogger<KeyVaultService>>();
		mockLogger.Setup(logger => logger.IsEnabled(LogLevel.Debug)).Returns(false);
		var keyVaultService = new KeyVaultService(fakeClient, mockLogger.Object);

		// Act
		await keyVaultService.GetSecretAsync(secretName);

		// Assert — Log() must not be called when IsEnabled returns false
		mockLogger.Verify(
			logger => logger.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception?>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Never);
	}

	[Fact]
	public async Task SetSecretAsync_WhenCalled_DelegatesToSecretClient()
	{
		// Arrange
		var secretName = "my-secret";
		var secretValue = "secret-value-456";
		var keyVaultSecret = SecretModelFactory.KeyVaultSecret(new SecretProperties(secretName), secretValue);
		var fakeClient = new FakeSecretClient(Response.FromValue(keyVaultSecret, Mock.Of<Response>()));
		var keyVaultService = new KeyVaultService(fakeClient, Mock.Of<ILogger<KeyVaultService>>());

		// Act
		await keyVaultService.SetSecretAsync(secretName, secretValue);

		// Assert
		fakeClient.SetSecretCallCount.Should().Be(1);
	}

	[Fact]
	public async Task SetSecretAsync_WhenDebugLoggingEnabled_LogsSecretName()
	{
		// Arrange
		var secretName = "write-secret";
		var secretValue = "write-value";
		var keyVaultSecret = SecretModelFactory.KeyVaultSecret(new SecretProperties(secretName), secretValue);
		var fakeClient = new FakeSecretClient(Response.FromValue(keyVaultSecret, Mock.Of<Response>()));
		var mockLogger = new Mock<ILogger<KeyVaultService>>();
		mockLogger.Setup(logger => logger.IsEnabled(LogLevel.Debug)).Returns(true);
		var keyVaultService = new KeyVaultService(fakeClient, mockLogger.Object);

		// Act
		await keyVaultService.SetSecretAsync(secretName, secretValue);

		// Assert — IsEnabled(Debug) was consulted before logging
		mockLogger.Verify(
			logger => logger.IsEnabled(LogLevel.Debug),
			Times.Once);
	}

	[Fact]
	public async Task SetSecretAsync_WhenDebugLoggingDisabled_DoesNotLog()
	{
		// Arrange
		var secretName = "silent-write";
		var secretValue = "silent-value";
		var keyVaultSecret = SecretModelFactory.KeyVaultSecret(new SecretProperties(secretName), secretValue);
		var fakeClient = new FakeSecretClient(Response.FromValue(keyVaultSecret, Mock.Of<Response>()));
		var mockLogger = new Mock<ILogger<KeyVaultService>>();
		mockLogger.Setup(logger => logger.IsEnabled(LogLevel.Debug)).Returns(false);
		var keyVaultService = new KeyVaultService(fakeClient, mockLogger.Object);

		// Act
		await keyVaultService.SetSecretAsync(secretName, secretValue);

		// Assert — Log() must not be called when IsEnabled returns false
		mockLogger.Verify(
			logger => logger.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception?>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Never);
	}

	/// <summary>
	/// A test-only subclass of <see cref="SecretClient"/> that returns pre-configured responses.
	/// </summary>
	private sealed class FakeSecretClient : SecretClient
	{
		private readonly Response<KeyVaultSecret>? _getResponse;
		private readonly Exception? _getException;

		public int SetSecretCallCount { get; private set; }

		public FakeSecretClient(Response<KeyVaultSecret> response)
		{
			_getResponse = response;
		}

		public FakeSecretClient(Exception exception)
		{
			_getException = exception;
		}

		public override Task<Response<KeyVaultSecret>> GetSecretAsync(string name, string version, SecretContentType? outContentType, CancellationToken cancellationToken)
		{
			if (_getException is not null)
			{
				throw _getException;
			}

			return Task.FromResult(_getResponse!);
		}

		public override Task<Response<KeyVaultSecret>> SetSecretAsync(string name, string value, CancellationToken cancellationToken = default)
		{
			SetSecretCallCount++;
			return Task.FromResult(_getResponse ?? Response.FromValue(
				SecretModelFactory.KeyVaultSecret(new SecretProperties(name), value),
				Mock.Of<Response>()));
		}
	}
}
