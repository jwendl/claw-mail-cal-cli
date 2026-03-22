namespace ClawMailCalCli;

/// <summary>
/// Bridges Microsoft's <see cref="IServiceCollection"/> with the Spectre.Console.Cli
/// dependency injection model.
/// </summary>
internal sealed class TypeRegistrar(IServiceCollection services)
: ITypeRegistrar
{
/// <inheritdoc />
public ITypeResolver Build() => new TypeResolver(services.BuildServiceProvider());

/// <inheritdoc />
public void Register(Type service, Type implementation) => services.AddTransient(service, implementation);

/// <inheritdoc />
public void RegisterInstance(Type service, object implementation) => services.AddSingleton(service, implementation);

/// <inheritdoc />
public void RegisterLazy(Type service, Func<object> factory) =>
services.AddTransient(service, _ => factory());

/// <summary>
/// Resolves services from a built <see cref="IServiceProvider"/>.
/// </summary>
private sealed class TypeResolver(IServiceProvider serviceProvider)
: ITypeResolver, IDisposable
{
/// <inheritdoc />
public object? Resolve(Type? type)
{
if (type is null)
{
return null;
}

return serviceProvider.GetService(type);
}

/// <inheritdoc />
public void Dispose()
{
if (serviceProvider is IDisposable disposable)
{
disposable.Dispose();
}
}
}
}