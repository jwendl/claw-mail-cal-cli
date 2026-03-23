using Microsoft.Graph;

namespace ClawMailCalCli.IntegrationTests.Fakes;

/// <summary>
/// A pre-seeded in-memory implementation of <see cref="IGraphClientService"/> for integration tests.
/// Returns pre-configured data without making real Microsoft Graph API calls.
/// The operation delegate is not invoked; instead the pre-seeded result for the matching return type is returned.
/// </summary>
public sealed class FakeGraphClientService : IGraphClientService
{
	private readonly Dictionary<Type, object?> _seedData = [];

	/// <summary>
	/// Seeds the fake with a return value for calls whose return type is <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type of the result to seed.</typeparam>
	/// <param name="result">The value to return when an operation with this return type is executed.</param>
	/// <returns>The same <see cref="FakeGraphClientService"/> for method chaining.</returns>
	public FakeGraphClientService Seed<T>(T result)
	{
		_seedData[typeof(T)] = result;
		return this;
	}

	/// <inheritdoc />
	/// <remarks>
	/// The operation delegate is never invoked. Instead, the pre-seeded value for type <typeparamref name="T"/>
	/// is returned directly. If no value was seeded for <typeparamref name="T"/>, <see langword="default"/>
	/// is returned — which is <see langword="null"/> for reference types. This behaviour is intentional: tests
	/// that expect a null response (e.g., error paths) can rely on an unseeded fake returning null without
	/// additional configuration.
	/// </remarks>
	public Task<T> ExecuteWithRetryAsync<T>(Func<GraphServiceClient, Task<T>> operation, CancellationToken cancellationToken = default)
	{
		if (_seedData.TryGetValue(typeof(T), out var seeded))
		{
			return Task.FromResult((T)seeded!);
		}

		return Task.FromResult(default(T)!);
	}
}
