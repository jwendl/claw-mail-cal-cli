using System.Runtime.InteropServices;

namespace ClawMailCalCli.Services;

/// <summary>
/// Sets restrictive file-system permissions on the MSAL token cache directory on Linux
/// so that only the owning user can read or write cache files.
/// </summary>
internal static partial class TokenCacheFileProtector
{
	private const int OwnerReadWrite = 0b_110_000_000; // 0600 — owner read/write only
	private const int OwnerReadWriteExecute = 0b_111_000_000; // 0700 — owner rwx only

	/// <summary>
	/// On Linux, sets the <paramref name="directoryPath"/> to mode 700 (owner rwx) and
	/// each file inside it to mode 600 (owner rw).  On other platforms this method is a
	/// no-op.
	/// </summary>
	internal static void ProtectCacheDirectory(string directoryPath)
	{
		if (!OperatingSystem.IsLinux())
		{
			return;
		}

		var directoryResult = Chmod(directoryPath, OwnerReadWriteExecute);
		if (directoryResult != 0)
		{
			var lastError = Marshal.GetLastPInvokeError();
			throw new IOException($"Failed to set permissions on cache directory '{directoryPath}' (chmod returned {directoryResult}, errno {lastError}).");
		}

		foreach (var filePath in Directory.GetFiles(directoryPath))
		{
			var fileResult = Chmod(filePath, OwnerReadWrite);
			if (fileResult != 0)
			{
				var lastError = Marshal.GetLastPInvokeError();
				throw new IOException($"Failed to set permissions on cache file '{filePath}' (chmod returned {fileResult}, errno {lastError}).");
			}
		}
	}

	[LibraryImport("libc", EntryPoint = "chmod", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
	private static partial int Chmod(string path, int mode);
}
