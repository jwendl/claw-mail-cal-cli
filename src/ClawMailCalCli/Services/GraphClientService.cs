using ClawMailCalCli.Services.Interfaces;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;

namespace ClawMailCalCli.Services;

/// <summary>
/// Wraps Microsoft Graph API calls with automatic 401 Unauthorized retry and re-authentication.
/// When a 401 is received the default account is re-authenticated and the operation is retried once.
/// </summary>
public class GraphClientService(IAccountService accountService, IGraphServiceClientBuilder graphServiceClientBuilder, IAuthenticationService authenticationService, ILogger<GraphClientService> logger, IOutputService outputService)
	: IGraphClientService
{
	/// <inheritdoc />
	public async Task<T> ExecuteWithRetryAsync<T>(Func<GraphServiceClient, Task<T>> operation, CancellationToken cancellationToken = default)
	{
		var defaultAccount = await accountService.GetDefaultAccountAsync(cancellationToken);
		if (defaultAccount is null)
		{
			outputService.WriteError("Error: No default account is set. Run 'account set <name>' to choose an account.");
			throw new InvalidOperationException("No default account configured. Run 'account set <name>' to choose one.");
		}

		var graphClient = await graphServiceClientBuilder.BuildAsync(defaultAccount, cancellationToken);
		if (graphClient is null)
		{
			outputService.WriteError($"Error: Account '{defaultAccount.Name}' is not authenticated. Run 'login {defaultAccount.Name}' first.");
			throw new InvalidOperationException($"Account '{defaultAccount.Name}' is not authenticated. Run 'login {defaultAccount.Name}' first.");
		}

		try
		{
			return await operation(graphClient);
		}
		catch (ODataError odataError) when (odataError.ResponseStatusCode == 401)
		{
			if (logger.IsEnabled(LogLevel.Information))
			{
				logger.LogInformation("Received 401 Unauthorized for account '{AccountName}'. Triggering re-authentication.", defaultAccount.Name);
			}

			outputService.WriteError($"Session expired for account '{defaultAccount.Name}'. Re-authenticating...");
			await authenticationService.AuthenticateAsync(defaultAccount.Name, cancellationToken);

			var retryClient = await graphServiceClientBuilder.BuildAsync(defaultAccount, cancellationToken);
			if (retryClient is null)
			{
				outputService.WriteError("Error: Re-authentication failed. Please run 'login' manually.");
				throw new InvalidOperationException($"Re-authentication failed for account '{defaultAccount.Name}'. Run 'login {defaultAccount.Name}' manually.");
			}

			return await operation(retryClient);
		}
	}
}
