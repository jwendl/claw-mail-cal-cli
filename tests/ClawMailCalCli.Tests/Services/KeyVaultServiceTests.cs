using Azure;
using Azure.Security.KeyVault.Secrets;
using ClawMailCalCli.Services;

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
		var keyVaultService = new KeyVaultService(fakeClient);

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
		var keyVaultService = new KeyVaultService(fakeClient);

		// Act
		var result = await keyVaultService.GetSecretAsync("nonexistent-secret");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task SetSecretAsync_WhenCalled_DelegatesToSecretClient()
	{
		// Arrange
		var secretName = "my-secret";
		var secretValue = "secret-value-456";
		var keyVaultSecret = SecretModelFactory.KeyVaultSecret(new SecretProperties(secretName), secretValue);
		var fakeClient = new FakeSecretClient(Response.FromValue(keyVaultSecret, Mock.Of<Response>()));
		var keyVaultService = new KeyVaultService(fakeClient);

		// Act
		await keyVaultService.SetSecretAsync(secretName, secretValue);

		// Assert
		fakeClient.SetSecretCallCount.Should().Be(1);
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
