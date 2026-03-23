using Microsoft.Graph;

namespace ClawMailCalCli.Services;

/// <summary>
/// Provides email sending operations via Microsoft Graph.
/// </summary>
public interface IEmailService
{
	/// <summary>
	/// Sends a plain-text email using the default account.
	/// Writes a descriptive error message to the console and returns <see langword="false"/> on failure.
	/// </summary>
	/// <param name="to">The recipient email address.</param>
	/// <param name="subject">The email subject.</param>
	/// <param name="content">The plain-text body of the email.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see langword="true"/> if the email was sent successfully; otherwise <see langword="false"/>.</returns>
	Task<bool> SendEmailAsync(string to, string subject, string content, CancellationToken cancellationToken = default);
}
