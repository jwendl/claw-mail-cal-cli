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

		Chmod(directoryPath, OwnerReadWriteExecute);

		foreach (var filePath in Directory.GetFiles(directoryPath))
		{
			Chmod(filePath, OwnerReadWrite);
		}
	}

	[LibraryImport("libc", EntryPoint = "chmod", StringMarshalling = StringMarshalling.Utf8)]
	private static partial int Chmod(string path, int mode);
}
