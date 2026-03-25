using ClawMailCalCli.Models;

namespace ClawMailCalCli.Services.Interfaces;

/// <summary>
/// Runs a series of environment checks and returns the results.
/// Used by the <c>doctor</c> command to verify that prerequisites are met.
/// </summary>
public interface IDoctorService
{
	/// <summary>
	/// Runs all environment checks and returns one <see cref="DoctorCheckResult"/> per check.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task<IReadOnlyList<DoctorCheckResult>> RunAllChecksAsync(CancellationToken cancellationToken = default);
}
