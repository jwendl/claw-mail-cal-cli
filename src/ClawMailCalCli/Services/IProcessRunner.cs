using ClawMailCalCli.Models;

namespace ClawMailCalCli.Services;

/// <summary>
/// Abstraction for running external processes, enabling testability of services
/// that invoke CLI tools such as the Azure CLI.
/// </summary>
public interface IProcessRunner
{
	/// <summary>
	/// Runs the specified process with the given arguments and returns the result.
	/// </summary>
	/// <param name="fileName">The file name or path of the executable to run.</param>
	/// <param name="arguments">The command-line arguments to pass to the process.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>
	/// A <see cref="ProcessResult"/> containing the exit code, standard output, and standard error.
	/// Returns a result with exit code <c>-1</c> and empty output if the process cannot be started.
	/// </returns>
	Task<ProcessResult> RunAsync(string fileName, string arguments, CancellationToken cancellationToken = default);
}
