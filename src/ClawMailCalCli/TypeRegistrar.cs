using Microsoft.Extensions.DependencyInjection;

namespace ClawMailCalCli;

/// <summary>
/// Integrates Microsoft Dependency Injection with Spectre.Console.Cli.
/// </summary>
internal sealed class TypeRegistrar(IServiceCollection services)
	: ITypeRegistrar
{
	/// <inheritdoc />
	public ITypeResolver Build()
	{
		return new TypeResolver(services.BuildServiceProvider());
	}

	/// <inheritdoc />
	public void Register(Type service, Type implementation)
	{
		services.AddTransient(service, implementation);
	}

	/// <inheritdoc />
	public void RegisterInstance(Type service, object implementation)
	{
		services.AddSingleton(service, implementation);
	}

	/// <inheritdoc />
	public void RegisterLazy(Type service, Func<object> factory)
	{
		services.AddTransient(service, _ => factory());
	}
}
