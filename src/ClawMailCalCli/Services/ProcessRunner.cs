using System.Diagnostics;
using ClawMailCalCli.Models;

namespace ClawMailCalCli.Services;

/// <summary>
/// Runs external processes by spawning child processes via <see cref="System.Diagnostics.Process"/>.
/// </summary>
public class ProcessRunner
	: IProcessRunner
{
	/// <inheritdoc />
	public async Task<ProcessResult> RunAsync(string fileName, string arguments, CancellationToken cancellationToken = default)
	{
		using var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = fileName,
				Arguments = arguments,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			},
		};

		try
		{
			process.Start();

			var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
			var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

			await process.WaitForExitAsync(cancellationToken);

			var output = await outputTask;
			var error = await errorTask;

			return new ProcessResult(process.ExitCode, output, error);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception exception)
		{
			return new ProcessResult(-1, string.Empty, exception.Message);
		}
	}
}
