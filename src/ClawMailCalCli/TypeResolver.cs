namespace ClawMailCalCli;

/// <summary>
/// Resolves types from a <see cref="IServiceProvider"/> for Spectre.Console.Cli.
/// </summary>
internal sealed class TypeResolver(IServiceProvider provider)
	: ITypeResolver, IDisposable
{
	/// <inheritdoc />
	public object? Resolve(Type? type)
	{
		if (type is null)
		{
			return null;
		}

		return provider.GetService(type);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (provider is IDisposable disposable)
		{
			disposable.Dispose();
		}
	}
}
