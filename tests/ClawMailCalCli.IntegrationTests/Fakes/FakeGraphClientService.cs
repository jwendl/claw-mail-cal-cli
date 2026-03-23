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
	/// is returned directly. Call <see cref="Seed{T}"/> before executing any operation whose return type
	/// is <typeparamref name="T"/>; an <see cref="InvalidOperationException"/> is thrown if no seed has
	/// been registered for the requested type.
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// Thrown when no seeded value has been registered for <typeparamref name="T"/>.
	/// Call <see cref="Seed{T}"/> with the expected return value before running the operation.
	/// </exception>
	public Task<T> ExecuteWithRetryAsync<T>(Func<GraphServiceClient, Task<T>> operation, CancellationToken cancellationToken = default)
	{
		if (_seedData.TryGetValue(typeof(T), out var seeded))
		{
			return Task.FromResult((T)seeded!);
		}

		throw new InvalidOperationException(
			$"FakeGraphClientService has no seeded value for return type '{typeof(T).Name}'. " +
			$"Call Seed<{typeof(T).Name}>(...) in the test Arrange section before invoking this operation.");
	}
}
