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
