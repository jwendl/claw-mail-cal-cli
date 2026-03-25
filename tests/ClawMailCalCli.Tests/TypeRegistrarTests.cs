using Microsoft.Extensions.DependencyInjection;

namespace ClawMailCalCli.Tests;

/// <summary>
/// Unit tests for <see cref="TypeRegistrar"/> and its nested <c>TypeResolver</c>.
/// </summary>
[Trait("Category", "Unit")]
public class TypeRegistrarTests
{
	[Fact]
	public void Build_ReturnsTypeResolverThatCanResolveRegisteredService()
	{
		// Arrange
		var services = new ServiceCollection();
		var typeRegistrar = new TypeRegistrar(services);
		typeRegistrar.Register(typeof(IGreeter), typeof(HelloGreeter));

		// Act
		var resolver = typeRegistrar.Build();
		var resolved = resolver.Resolve(typeof(IGreeter));

		// Assert
		resolved.Should().NotBeNull();
		resolved.Should().BeOfType<HelloGreeter>();
		((IDisposable)resolver).Dispose();
	}

	[Fact]
	public void Resolve_WhenTypeIsNotRegistered_ThrowsInvalidOperationException()
	{
		// Arrange
		var services = new ServiceCollection();
		var typeRegistrar = new TypeRegistrar(services);
		var resolver = typeRegistrar.Build();

		// Act
		var act = () => resolver.Resolve(typeof(IGreeter));

		// Assert
		act.Should().Throw<InvalidOperationException>();
		((IDisposable)resolver).Dispose();
	}

	[Fact]
	public void Resolve_WhenRegisteredTypeHasFailingDependency_ThrowsActualException()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddTransient<IGreeter>(_ => throw new InvalidOperationException("Dependency construction failed."));
		var typeRegistrar = new TypeRegistrar(services);
		var resolver = typeRegistrar.Build();

		// Act
		var act = () => resolver.Resolve(typeof(IGreeter));

		// Assert
		act.Should().Throw<InvalidOperationException>().WithMessage("*Dependency construction failed*");
		((IDisposable)resolver).Dispose();
	}

	[Fact]
	public void Resolve_WhenTypeIsNull_ReturnsNull()
	{
		// Arrange
		var services = new ServiceCollection();
		var typeRegistrar = new TypeRegistrar(services);

		// Act
		var resolver = typeRegistrar.Build();
		var resolved = resolver.Resolve(null);

		// Assert
		resolved.Should().BeNull();
		((IDisposable)resolver).Dispose();
	}

	[Fact]
	public void RegisterInstance_AllowsResolutionOfRegisteredInstance()
	{
		// Arrange
		var services = new ServiceCollection();
		var typeRegistrar = new TypeRegistrar(services);
		var instance = new HelloGreeter();
		typeRegistrar.RegisterInstance(typeof(IGreeter), instance);

		// Act
		var resolver = typeRegistrar.Build();
		var resolved = resolver.Resolve(typeof(IGreeter));

		// Assert
		resolved.Should().BeSameAs(instance);
		((IDisposable)resolver).Dispose();
	}

	[Fact]
	public void RegisterLazy_AllowsResolutionViaFactory()
	{
		// Arrange
		var services = new ServiceCollection();
		var typeRegistrar = new TypeRegistrar(services);
		var instance = new HelloGreeter();
		typeRegistrar.RegisterLazy(typeof(IGreeter), () => instance);

		// Act
		var resolver = typeRegistrar.Build();
		var resolved = resolver.Resolve(typeof(IGreeter));

		// Assert
		resolved.Should().NotBeNull();
		((IDisposable)resolver).Dispose();
	}

	[Fact]
	public void Resolve_WhenRegisteredServiceHasBrokenFactory_SurfacesActualException()
	{
		// Arrange
		var services = new ServiceCollection();
		var typeRegistrar = new TypeRegistrar(services);
		typeRegistrar.RegisterLazy(typeof(IGreeter), () => throw new InvalidOperationException("dependency is not configured"));

		// Act
		var resolver = typeRegistrar.Build();
		var act = () => resolver.Resolve(typeof(IGreeter));

		// Assert – the real construction error must propagate, not be masked as null
		// (which would produce Spectre's generic "Could not resolve type" error instead).
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("dependency is not configured");
	}

	[Fact]
	public void Dispose_WhenCalled_DisposesUnderlyingServiceProvider()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IDisposableGreeter, DisposableGreeter>();
		var typeRegistrar = new TypeRegistrar(services);
		var resolver = typeRegistrar.Build();
		var service = (DisposableGreeter)resolver.Resolve(typeof(IDisposableGreeter))!;

		// Act
		((IDisposable)resolver).Dispose();

		// Assert
		service.WasDisposed.Should().BeTrue();
	}

	private interface IGreeter { }
	private sealed class HelloGreeter : IGreeter { }

	private interface IDisposableGreeter { }

	private sealed class DisposableGreeter : IDisposableGreeter, IDisposable
	{
		public bool WasDisposed { get; private set; }

		public void Dispose() => WasDisposed = true;
	}
}
