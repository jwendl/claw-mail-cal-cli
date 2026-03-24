namespace ClawMailCalCli.Logging;

/// <summary>
/// Controls how much log output is written to stderr.
/// </summary>
public enum VerbosityLevel
{
	/// <summary>Only errors are shown.</summary>
	Quiet,

	/// <summary>Errors and warnings are shown (default).</summary>
	Normal,

	/// <summary>All log messages, including debug-level diagnostics, are shown.</summary>
	Debug,
}
