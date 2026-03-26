using ClawMailCalCli.Services;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="TokenCacheFileProtector"/>.
/// </summary>
[Trait("Category", "Unit")]
public class TokenCacheFileProtectorTests
{
	[Fact]
	public void ProtectCacheDirectory_OnAnyPlatform_DoesNotThrowForEmptyDirectory()
	{
		// Arrange
		var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		Directory.CreateDirectory(tempDirectory);

		try
		{
			// Act
			var act = () => TokenCacheFileProtector.ProtectCacheDirectory(tempDirectory);

			// Assert
			act.Should().NotThrow();
		}
		finally
		{
			Directory.Delete(tempDirectory, recursive: true);
		}
	}

	[Fact]
	public void ProtectCacheDirectory_OnLinux_SetsDirectoryPermissionsTo0700()
	{
		if (!OperatingSystem.IsLinux())
		{
			return;
		}

		// Arrange
		var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		Directory.CreateDirectory(tempDirectory);

		try
		{
			// Act
			TokenCacheFileProtector.ProtectCacheDirectory(tempDirectory);

			// Assert
			var mode = File.GetUnixFileMode(tempDirectory);
			mode.Should().Be(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
		}
		finally
		{
			Directory.Delete(tempDirectory, recursive: true);
		}
	}

	[Fact]
	public void ProtectCacheDirectory_OnLinux_SetsFilePermissionsTo0600()
	{
		if (!OperatingSystem.IsLinux())
		{
			return;
		}

		// Arrange
		var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		Directory.CreateDirectory(tempDirectory);
		var cacheFilePath = Path.Combine(tempDirectory, "msal.cache");
		File.WriteAllText(cacheFilePath, "test-token-cache-content");

		try
		{
			// Act
			TokenCacheFileProtector.ProtectCacheDirectory(tempDirectory);

			// Assert
			var mode = File.GetUnixFileMode(cacheFilePath);
			mode.Should().Be(UnixFileMode.UserRead | UnixFileMode.UserWrite);
		}
		finally
		{
			// The directory is set to 0700 (owner rwx), so the owner can still delete files within it.
			Directory.Delete(tempDirectory, recursive: true);
		}
	}

	[Fact]
	public void ProtectCacheDirectory_OnNonLinux_LeavesDirectoryPermissionsUnchanged()
	{
		if (OperatingSystem.IsLinux())
		{
			return;
		}

		// Arrange
		var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		Directory.CreateDirectory(tempDirectory);
		var cacheFilePath = Path.Combine(tempDirectory, "msal.cache");
		File.WriteAllText(cacheFilePath, "test-token-cache-content");

		try
		{
			// Act — on non-Linux this should be a no-op and never touch the file system permissions
			var act = () => TokenCacheFileProtector.ProtectCacheDirectory(tempDirectory);

			// Assert
			act.Should().NotThrow();
		}
		finally
		{
			Directory.Delete(tempDirectory, recursive: true);
		}
	}
}
