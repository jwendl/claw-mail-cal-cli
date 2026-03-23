namespace ClawMailCalCli.Commands;

/// <summary>
/// Authenticates a named account using the Entra ID device code flow.
/// Usage: <c>claw-mail-cal-cli login &lt;account-name&gt;</c>
/// </summary>
internal sealed class LoginCommand(IAuthenticationService authenticationService)
	: AsyncCommand<LoginCommand.Settings>
{
	/// <summary>
	/// Settings (arguments and options) for the <see cref="LoginCommand"/>.
	/// </summary>
	internal sealed class Settings : CommandSettings
	{
		/// <summary>Gets or sets the name of the account to authenticate.</summary>
		[CommandArgument(0, "<account-name>")]
		public string AccountName { get; set; } = string.Empty;
	}

	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
	{
		await authenticationService.AuthenticateAsync(settings.AccountName, cancellationToken);
		return 0;
	}
}
