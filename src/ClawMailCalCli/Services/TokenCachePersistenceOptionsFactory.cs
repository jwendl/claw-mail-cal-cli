using Azure.Identity;

namespace ClawMailCalCli.Services;

/// <summary>
/// Factory that creates OS-aware <see cref="TokenCachePersistenceOptions"/> so that
/// libsecret / GNOME Keyring is not required on Linux.
/// </summary>
internal static class TokenCachePersistenceOptionsFactory
{
	/// <summary>
	/// Returns <see cref="TokenCachePersistenceOptions"/> configured for the current
	/// operating system.  On Linux, <see cref="TokenCachePersistenceOptions.UnsafeAllowUnencryptedStorage"/>
	/// is set to <see langword="true"/> to bypass the libsecret / D-Bus requirement.
	/// On Windows (DPAPI) and macOS (Keychain) the default encrypted storage is used.
	/// </summary>
	internal static TokenCachePersistenceOptions Create() =>
		new()
		{
			Name = "ClawMailCalCli",
			UnsafeAllowUnencryptedStorage = OperatingSystem.IsLinux(),
		};
}
