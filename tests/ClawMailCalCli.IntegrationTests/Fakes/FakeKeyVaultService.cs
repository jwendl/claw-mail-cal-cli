namespace ClawMailCalCli.IntegrationTests.Fakes;

/// <summary>
/// An in-memory implementation of <see cref="IKeyVaultService"/> for integration tests.
/// Stores secrets in a dictionary instead of Azure Key Vault, so tests run without real Azure credentials.
/// </summary>
public sealed class FakeKeyVaultService : IKeyVaultService
{
	private readonly Dictionary<string, string> _secrets = [];

	/// <inheritdoc />
	public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
	{
		_secrets.TryGetValue(secretName, out var value);
		return Task.FromResult<string?>(value);
	}

	/// <inheritdoc />
	public Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
	{
		_secrets[secretName] = secretValue;
		return Task.CompletedTask;
	}
}
