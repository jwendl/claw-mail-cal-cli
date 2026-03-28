using System.Text.Json;
using Azure.Identity;
using ClawMailCalCli.Services.Interfaces;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;

namespace ClawMailCalCli.Services;

/// <summary>
/// Wraps Microsoft Graph API calls with automatic 401 Unauthorized retry and re-authentication.
/// When a 401 is received the default account is re-authenticated and the operation is retried once.
/// </summary>
public class GraphClientService(IAccountService accountService, IGraphServiceClientBuilder graphServiceClientBuilder, IAuthenticationService authenticationService, NonInteractiveMode nonInteractiveMode, ILogger<GraphClientService> logger, IOutputService outputService)
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
			if (nonInteractiveMode.IsNonInteractive)
			{
				const string message = "Authentication required. Run 'claw-mail-cal-cli login <account>' interactively first.";
				const string code = "AUTH_REQUIRED";

				if (nonInteractiveMode.IsJson)
				{
					var errorObject = new { error = message, code };
					Console.Out.WriteLine(JsonSerializer.Serialize(errorObject));
				}
				else
				{
					Console.Error.WriteLine($"Error: Session expired for account '{defaultAccount.Name}'. Run 'login {defaultAccount.Name}' interactively first.");
				}

				throw new InvalidOperationException($"Authentication required for account '{defaultAccount.Name}'. Run 'login {defaultAccount.Name}' interactively first.");
			}

			if (logger.IsEnabled(LogLevel.Information))
			{
				logger.LogInformation("Received 401 Unauthorized for account '{AccountName}'. Triggering re-authentication.", defaultAccount.Name);
			}

			outputService.WriteError($"Session expired for account '{defaultAccount.Name}'. Re-authenticating...");
			var authenticated = await authenticationService.AuthenticateAsync(defaultAccount.Name, cancellationToken);
			if (!authenticated)
			{
				outputService.WriteError($"Error: Re-authentication failed for account '{defaultAccount.Name}'. Please run 'login {defaultAccount.Name}' manually.");
				throw new InvalidOperationException($"Re-authentication failed for account '{defaultAccount.Name}'. Run 'login {defaultAccount.Name}' manually.");
			}

			var retryClient = await graphServiceClientBuilder.BuildAsync(defaultAccount, cancellationToken);
			if (retryClient is null)
			{
				outputService.WriteError("Error: Re-authentication failed. Please run 'login' manually.");
				throw new InvalidOperationException($"Re-authentication failed for account '{defaultAccount.Name}'. Run 'login {defaultAccount.Name}' manually.");
			}

			return await operation(retryClient);
		}
		catch (AuthenticationFailedException authenticationFailedException)
		{
			if (nonInteractiveMode.IsNonInteractive)
			{
				const string message = "Authentication required. Run 'claw-mail-cal-cli login <account>' interactively first.";

				if (nonInteractiveMode.IsJson)
				{
					outputService.WriteJsonError(message, ErrorCodes.AuthRequired);
				}
				else
				{
					outputService.WriteError($"Error: Token acquisition failed for account '{defaultAccount.Name}'. Run 'login {defaultAccount.Name}' interactively first.");
				}

				throw new InvalidOperationException($"Authentication required for account '{defaultAccount.Name}'. Run 'login {defaultAccount.Name}' interactively first.");
			}

			if (logger.IsEnabled(LogLevel.Information))
			{
				logger.LogInformation(authenticationFailedException, "Token acquisition failed for account '{AccountName}'. Triggering re-authentication.", defaultAccount.Name);
			}

			outputService.WriteError($"Token acquisition failed for account '{defaultAccount.Name}'. Re-authenticating...");
			var reauthenticated = await authenticationService.AuthenticateAsync(defaultAccount.Name, cancellationToken, forceInteractive: true);
			if (!reauthenticated)
			{
				outputService.WriteError($"Error: Re-authentication failed for account '{defaultAccount.Name}'. Please run 'login {defaultAccount.Name}' manually.");
				throw new InvalidOperationException($"Re-authentication failed for account '{defaultAccount.Name}'. Run 'login {defaultAccount.Name}' manually.");
			}

			var retryClient = await graphServiceClientBuilder.BuildAsync(defaultAccount, cancellationToken);
			if (retryClient is null)
			{
				outputService.WriteError($"Error: Re-authentication failed for account '{defaultAccount.Name}'. Please run 'login {defaultAccount.Name}' manually.");
				throw new InvalidOperationException($"Re-authentication failed for account '{defaultAccount.Name}'. Run 'login {defaultAccount.Name}' manually.");
			}

			return await operation(retryClient);
		}
	}

	/// <inheritdoc />
	public async Task<T> ExecuteWithRetryAsync<T>(Func<GraphServiceClient, Task<T>> operation, string accountName, CancellationToken cancellationToken = default)
	{
		var account = await accountService.GetAccountAsync(accountName, cancellationToken);
		if (account is null)
		{
			outputService.WriteError($"Error: Account '{accountName}' does not exist.");
			throw new InvalidOperationException($"Account '{accountName}' does not exist.");
		}

		var graphClient = await graphServiceClientBuilder.BuildAsync(account, cancellationToken);
		if (graphClient is null)
		{
			outputService.WriteError($"Error: Account '{accountName}' is not authenticated. Run 'login {accountName}' first.");
			throw new InvalidOperationException($"Account '{accountName}' is not authenticated. Run 'login {accountName}' first.");
		}

		try
		{
			return await operation(graphClient);
		}
		catch (ODataError odataError) when (odataError.ResponseStatusCode == 401)
		{
			if (logger.IsEnabled(LogLevel.Information))
			{
				logger.LogInformation("Received 401 Unauthorized for account '{AccountName}'. Triggering re-authentication.", accountName);
			}

			outputService.WriteError($"Session expired for account '{accountName}'. Re-authenticating...");
			var authenticated = await authenticationService.AuthenticateAsync(accountName, cancellationToken);
			if (!authenticated)
			{
				outputService.WriteError($"Error: Re-authentication failed for account '{accountName}'. Please run 'login {accountName}' manually.");
				throw new InvalidOperationException($"Re-authentication failed for account '{accountName}'. Run 'login {accountName}' manually.");
			}

			var retryClient = await graphServiceClientBuilder.BuildAsync(account, cancellationToken);
			if (retryClient is null)
			{
				outputService.WriteError("Error: Re-authentication failed. Please run 'login' manually.");
				throw new InvalidOperationException($"Re-authentication failed for account '{accountName}'. Run 'login {accountName}' manually.");
			}

			return await operation(retryClient);
		}
		catch (AuthenticationFailedException authenticationFailedException)
		{
			if (logger.IsEnabled(LogLevel.Information))
			{
				logger.LogInformation(authenticationFailedException, "Token acquisition failed for account '{AccountName}'. Triggering re-authentication.", accountName);
			}

			if (nonInteractiveMode.IsNonInteractive)
			{
				var authRequiredError = JsonSerializer.Serialize(new
				{
					code = "AUTH_REQUIRED",
					message = $"Authentication required for account '{accountName}'. Please run 'login {accountName}' interactively.",
					account = accountName
				});

				outputService.WriteError(authRequiredError);
				throw;
			}
			outputService.WriteError($"Token acquisition failed for account '{accountName}'. Re-authenticating...");
			var reauthenticated = await authenticationService.AuthenticateAsync(accountName, cancellationToken, forceInteractive: true);
			if (!reauthenticated)
			{
				outputService.WriteError($"Error: Re-authentication failed for account '{accountName}'. Please run 'login {accountName}' manually.");
				throw new InvalidOperationException($"Re-authentication failed for account '{accountName}'. Run 'login {accountName}' manually.");
			}

			var retryClient = await graphServiceClientBuilder.BuildAsync(account, cancellationToken);
			if (retryClient is null)
			{
				outputService.WriteError($"Error: Re-authentication failed for account '{accountName}'. Please run 'login {accountName}' manually.");
				throw new InvalidOperationException($"Re-authentication failed for account '{accountName}'. Run 'login {accountName}' manually.");
			}

			return await operation(retryClient);
		}
	}
}
