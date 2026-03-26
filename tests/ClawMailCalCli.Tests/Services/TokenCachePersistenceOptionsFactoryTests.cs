using ClawMailCalCli.Services;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="TokenCachePersistenceOptionsFactory"/>.
/// </summary>
[Trait("Category", "Unit")]
public class TokenCachePersistenceOptionsFactoryTests
{
	[Fact]
	public void Create_ReturnsNonNullOptions()
	{
		// Act
		var options = TokenCachePersistenceOptionsFactory.Create();

		// Assert
		options.Should().NotBeNull();
	}

	[Fact]
	public void Create_OnCurrentPlatform_SetsUnsafeAllowUnencryptedStorageCorrectly()
	{
		// Act
		var options = TokenCachePersistenceOptionsFactory.Create();

		// Assert
		if (OperatingSystem.IsLinux())
		{
			options.UnsafeAllowUnencryptedStorage.Should().BeTrue();
		}
		else
		{
			options.UnsafeAllowUnencryptedStorage.Should().BeFalse();
		}
	}
}
