using Azure.Identity;
using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
using ClawMailCalCli.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;

namespace ClawMailCalCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="GraphClientService"/>.
/// </summary>
[Trait("Category", "Unit")]
public class GraphClientServiceTests
{
	private readonly Mock<IAccountService> _mockAccountService;
	private readonly Mock<IGraphServiceClientBuilder> _mockGraphServiceClientBuilder;
	private readonly Mock<IAuthenticationService> _mockAuthenticationService;
	private readonly ILogger<GraphClientService> _logger;
	private readonly Mock<IOutputService> _mockOutputService;

	public GraphClientServiceTests()
	{
		_mockAccountService = new Mock<IAccountService>();
		_mockGraphServiceClientBuilder = new Mock<IGraphServiceClientBuilder>();
		_mockAuthenticationService = new Mock<IAuthenticationService>();
		_logger = new NullLogger<GraphClientService>();
		_mockOutputService = new Mock<IOutputService>();
	}

	private GraphClientService CreateGraphClientService(NonInteractiveMode? nonInteractiveMode = null) =>
		new GraphClientService(
			_mockAccountService.Object,
			_mockGraphServiceClientBuilder.Object,
			_mockAuthenticationService.Object,
			nonInteractiveMode ?? new NonInteractiveMode(),
			_logger,
			_mockOutputService.Object);

	[Fact]
	public async Task ExecuteWithRetryAsync_WhenNoDefaultAccount_ThrowsInvalidOperationException()
	{
		// Arrange
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync((Account?)null);

		var graphClientService = CreateGraphClientService();

		// Act
		var act = async () => await graphClientService.ExecuteWithRetryAsync(_ => Task.FromResult("result"));

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*No default account*");
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WhenNoDefaultAccount_DoesNotCallGraphServiceClientBuilder()
	{
		// Arrange
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync((Account?)null);

		var graphClientService = CreateGraphClientService();

		// Act
		try { await graphClientService.ExecuteWithRetryAsync(_ => Task.FromResult("result")); } catch { }

		// Assert
		_mockGraphServiceClientBuilder.Verify(
			builder => builder.BuildAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WhenAccountNotAuthenticated_ThrowsInvalidOperationException()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync((GraphServiceClient?)null);

		var graphClientService = CreateGraphClientService();

		// Act
		var act = async () => await graphClientService.ExecuteWithRetryAsync(_ => Task.FromResult("result"));

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*not authenticated*");
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WhenAccountNotAuthenticated_DoesNotCallAuthenticationService()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync((GraphServiceClient?)null);

		var graphClientService = CreateGraphClientService();

		// Act
		try { await graphClientService.ExecuteWithRetryAsync(_ => Task.FromResult("result")); } catch { }

		// Assert — no re-auth should have been attempted
		_mockAuthenticationService.Verify(
			authService => authService.AuthenticateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WhenOperationSucceeds_ReturnsResult()
	{
		// Arrange
		var account = new Account("personal-account", "user@hotmail.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		var fakeGraphClient = BuildFakeGraphServiceClient();
		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeGraphClient);

		var graphClientService = CreateGraphClientService();

		// Act
		var result = await graphClientService.ExecuteWithRetryAsync(_ => Task.FromResult("success"));

		// Assert
		result.Should().Be("success");
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WhenOperationSucceeds_DoesNotCallAuthenticationService()
	{
		// Arrange
		var account = new Account("personal-account", "user@hotmail.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		var fakeGraphClient = BuildFakeGraphServiceClient();
		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeGraphClient);

		var graphClientService = CreateGraphClientService();

		// Act
		await graphClientService.ExecuteWithRetryAsync(_ => Task.FromResult("success"));

		// Assert — no re-auth should have been attempted for a successful operation
		_mockAuthenticationService.Verify(
			authService => authService.AuthenticateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WhenOperationThrows401_TriggersReAuthentication()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		var fakeGraphClient = BuildFakeGraphServiceClient();
		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeGraphClient);

		_mockAuthenticationService
			.Setup(authService => authService.AuthenticateAsync("work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var callCount = 0;
		Func<GraphServiceClient, Task<string>> operation = _ =>
		{
			callCount++;
			if (callCount == 1)
			{
				throw new ODataError { ResponseStatusCode = 401 };
			}

			return Task.FromResult("retry-success");
		};

		var graphClientService = CreateGraphClientService();

		// Act
		var result = await graphClientService.ExecuteWithRetryAsync(operation);

		// Assert
		result.Should().Be("retry-success");
		_mockAuthenticationService.Verify(
			authService => authService.AuthenticateAsync("work-account", It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WhenOperationThrows401_CallsOperationTwice()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		var fakeGraphClient = BuildFakeGraphServiceClient();
		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeGraphClient);

		_mockAuthenticationService
			.Setup(authService => authService.AuthenticateAsync("work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var callCount = 0;
		Func<GraphServiceClient, Task<string>> operation = _ =>
		{
			callCount++;
			if (callCount == 1)
			{
				throw new ODataError { ResponseStatusCode = 401 };
			}

			return Task.FromResult("success");
		};

		var graphClientService = CreateGraphClientService();

		// Act
		await graphClientService.ExecuteWithRetryAsync(operation);

		// Assert — operation must have been called exactly twice (initial + retry)
		callCount.Should().Be(2);
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WhenOperationThrows401AndReAuthProducesNoClient_ThrowsInvalidOperationException()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		// First call returns a valid client; second call (after re-auth) returns null
		var fakeGraphClient = BuildFakeGraphServiceClient();
		_mockGraphServiceClientBuilder
			.SetupSequence(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeGraphClient)
			.ReturnsAsync((GraphServiceClient?)null);

		_mockAuthenticationService
			.Setup(authService => authService.AuthenticateAsync("work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		Func<GraphServiceClient, Task<string>> operation = _ =>
			throw new ODataError { ResponseStatusCode = 401 };

		var graphClientService = CreateGraphClientService();

		// Act
		var act = async () => await graphClientService.ExecuteWithRetryAsync(operation);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*Re-authentication failed*");
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WhenOperationThrowsNon401Error_DoesNotRetry()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		var fakeGraphClient = BuildFakeGraphServiceClient();
		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeGraphClient);

		var callCount = 0;
		Func<GraphServiceClient, Task<string>> operation = _ =>
		{
			callCount++;
			throw new ODataError { ResponseStatusCode = 403 };
		};

		var graphClientService = CreateGraphClientService();

		// Act
		var act = async () => await graphClientService.ExecuteWithRetryAsync(operation);

		// Assert — a 403 should propagate immediately without re-auth
		await act.Should().ThrowAsync<ODataError>();
		callCount.Should().Be(1);
		_mockAuthenticationService.Verify(
			authService => authService.AuthenticateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WhenOperationThrows401_BuildsClientTwice()
	{
		// Arrange
		var account = new Account("personal-account", "user@hotmail.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		var fakeGraphClient = BuildFakeGraphServiceClient();
		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeGraphClient);

		_mockAuthenticationService
			.Setup(authService => authService.AuthenticateAsync("personal-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var callCount = 0;
		Func<GraphServiceClient, Task<string>> operation = _ =>
		{
			callCount++;
			if (callCount == 1)
			{
				throw new ODataError { ResponseStatusCode = 401 };
			}

			return Task.FromResult("success");
		};

		var graphClientService = CreateGraphClientService();

		// Act
		await graphClientService.ExecuteWithRetryAsync(operation);

		// Assert — the client should be built once before the first attempt and once after re-auth
		_mockGraphServiceClientBuilder.Verify(
			builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()),
			Times.Exactly(2));
	}

	/// <summary>
	/// Creates a <see cref="GraphServiceClient"/> backed by a mock <see cref="IRequestAdapter"/>
	/// for use in tests where the client is passed to an operation but never actually used to make
	/// real HTTP calls.
	/// </summary>
	private static GraphServiceClient BuildFakeGraphServiceClient()
	{
		var mockRequestAdapter = new Mock<IRequestAdapter>();
		mockRequestAdapter.SetupGet(adapter => adapter.BaseUrl).Returns("https://graph.microsoft.com/v1.0");
		return new GraphServiceClient(mockRequestAdapter.Object);
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WithAccountName_WhenAccountNotFound_ThrowsInvalidOperationException()
	{
		// Arrange
		_mockAccountService
			.Setup(accountService => accountService.GetAccountAsync("nonexistent", It.IsAny<CancellationToken>()))
			.ReturnsAsync((Account?)null);

		var graphClientService = CreateGraphClientService();

		// Act
		var act = async () => await graphClientService.ExecuteWithRetryAsync(_ => Task.FromResult("result"), "nonexistent");

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*does not exist*");
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WithAccountName_WhenAccountNotAuthenticated_ThrowsInvalidOperationException()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetAccountAsync("work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync((GraphServiceClient?)null);

		var graphClientService = CreateGraphClientService();

		// Act
		var act = async () => await graphClientService.ExecuteWithRetryAsync(_ => Task.FromResult("result"), "work-account");

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*not authenticated*");
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WithAccountName_WhenOperationSucceeds_ReturnsResult()
	{
		// Arrange
		var account = new Account("personal-account", "user@hotmail.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetAccountAsync("personal-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		var fakeGraphClient = BuildFakeGraphServiceClient();
		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeGraphClient);

		var graphClientService = CreateGraphClientService();

		// Act
		var result = await graphClientService.ExecuteWithRetryAsync(_ => Task.FromResult("success"), "personal-account");

		// Assert
		result.Should().Be("success");
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WithAccountName_WhenOperationThrows401_TriggersReAuthentication()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetAccountAsync("work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		var fakeGraphClient = BuildFakeGraphServiceClient();
		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeGraphClient);

		_mockAuthenticationService
			.Setup(authService => authService.AuthenticateAsync("work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var callCount = 0;
		Func<GraphServiceClient, Task<string>> operation = _ =>
		{
			callCount++;
			if (callCount == 1)
			{
				throw new ODataError { ResponseStatusCode = 401 };
			}

			return Task.FromResult("retry-success");
		};

		var graphClientService = CreateGraphClientService();

		// Act
		var result = await graphClientService.ExecuteWithRetryAsync(operation, "work-account");

		// Assert
		result.Should().Be("retry-success");
		_mockAuthenticationService.Verify(
			authService => authService.AuthenticateAsync("work-account", It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WhenOperationThrows401AndReAuthReturnsFalse_ThrowsInvalidOperationException()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		var fakeGraphClient = BuildFakeGraphServiceClient();
		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeGraphClient);

		_mockAuthenticationService
			.Setup(authService => authService.AuthenticateAsync("work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		Func<GraphServiceClient, Task<string>> operation = _ =>
			throw new ODataError { ResponseStatusCode = 401 };

		var graphClientService = CreateGraphClientService();

		// Act
		var act = async () => await graphClientService.ExecuteWithRetryAsync(operation);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*Re-authentication failed*");
		_mockGraphServiceClientBuilder.Verify(
			builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WithAccountName_WhenOperationThrows401AndReAuthReturnsFalse_ThrowsInvalidOperationException()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetAccountAsync("work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		var fakeGraphClient = BuildFakeGraphServiceClient();
		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeGraphClient);

		_mockAuthenticationService
			.Setup(authService => authService.AuthenticateAsync("work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		Func<GraphServiceClient, Task<string>> operation = _ =>
			throw new ODataError { ResponseStatusCode = 401 };

		var graphClientService = CreateGraphClientService();

		// Act
		var act = async () => await graphClientService.ExecuteWithRetryAsync(operation, "work-account");

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*Re-authentication failed*");
		_mockGraphServiceClientBuilder.Verify(
			builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WhenNonInteractiveAndOperationThrows401_ThrowsWithoutCallingAuthenticationService()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		var fakeGraphClient = BuildFakeGraphServiceClient();
		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeGraphClient);

		Func<GraphServiceClient, Task<string>> operation = _ => throw new ODataError { ResponseStatusCode = 401 };

		var nonInteractiveMode = new NonInteractiveMode { IsNonInteractive = true };
		var graphClientService = CreateGraphClientService(nonInteractiveMode);

		// Act
		var act = async () => await graphClientService.ExecuteWithRetryAsync(operation);

		// Assert — non-interactive mode must fail fast without attempting re-authentication
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*Authentication required*");
		_mockAuthenticationService.Verify(
			authService => authService.AuthenticateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()),
			Times.Never);
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WhenNonInteractiveAndOperationSucceeds_ReturnsResultNormally()
	{
		// Arrange
		var account = new Account("personal-account", "user@hotmail.com", AccountType.Personal);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		var fakeGraphClient = BuildFakeGraphServiceClient();
		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeGraphClient);

		var nonInteractiveMode = new NonInteractiveMode { IsNonInteractive = true };
		var graphClientService = CreateGraphClientService(nonInteractiveMode);

		// Act
		var result = await graphClientService.ExecuteWithRetryAsync(_ => Task.FromResult("success"));

		// Assert — non-interactive mode does not affect successful operations
		result.Should().Be("success");
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WhenOperationThrowsAuthenticationFailed_TriggersForceInteractiveReAuthentication()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		var fakeGraphClient = BuildFakeGraphServiceClient();
		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeGraphClient);

		_mockAuthenticationService
			.Setup(authService => authService.AuthenticateAsync("work-account", It.IsAny<CancellationToken>(), true))
			.ReturnsAsync(true);

		var callCount = 0;
		Func<GraphServiceClient, Task<string>> operation = _ =>
		{
			callCount++;
			if (callCount == 1)
			{
				throw new AuthenticationFailedException("MSAL cache is empty");
			}

			return Task.FromResult("retry-success");
		};

		var graphClientService = CreateGraphClientService();

		// Act
		var result = await graphClientService.ExecuteWithRetryAsync(operation);

		// Assert — re-authentication must be triggered with forceInteractive: true so the stale
		// MSAL cache is bypassed and a fresh device-code flow is started
		result.Should().Be("retry-success");
		_mockAuthenticationService.Verify(
			authService => authService.AuthenticateAsync("work-account", It.IsAny<CancellationToken>(), true),
			Times.Once);
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WhenOperationThrowsAuthenticationFailedAndReAuthReturnsFalse_ThrowsInvalidOperationException()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		var fakeGraphClient = BuildFakeGraphServiceClient();
		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeGraphClient);

		_mockAuthenticationService
			.Setup(authService => authService.AuthenticateAsync("work-account", It.IsAny<CancellationToken>(), true))
			.ReturnsAsync(false);

		Func<GraphServiceClient, Task<string>> operation = _ =>
			throw new AuthenticationFailedException("MSAL cache is empty");

		var graphClientService = CreateGraphClientService();

		// Act
		var act = async () => await graphClientService.ExecuteWithRetryAsync(operation);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*Re-authentication failed*");
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WhenNonInteractiveAndOperationThrowsAuthenticationFailed_ThrowsWithoutCallingAuthenticationService()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetDefaultAccountAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		var fakeGraphClient = BuildFakeGraphServiceClient();
		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeGraphClient);

		Func<GraphServiceClient, Task<string>> operation = _ =>
			throw new AuthenticationFailedException("MSAL cache is empty");

		var nonInteractiveMode = new NonInteractiveMode { IsNonInteractive = true };
		var graphClientService = CreateGraphClientService(nonInteractiveMode);

		// Act
		var act = async () => await graphClientService.ExecuteWithRetryAsync(operation);

		// Assert — non-interactive mode must not attempt re-authentication; the error is surfaced immediately
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*Authentication required*");
		_mockAuthenticationService.Verify(
			authService => authService.AuthenticateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()),
			Times.Never);
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WithAccountName_WhenOperationThrowsAuthenticationFailed_TriggersForceInteractiveReAuthentication()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetAccountAsync("work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		var fakeGraphClient = BuildFakeGraphServiceClient();
		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeGraphClient);

		_mockAuthenticationService
			.Setup(authService => authService.AuthenticateAsync("work-account", It.IsAny<CancellationToken>(), true))
			.ReturnsAsync(true);

		var callCount = 0;
		Func<GraphServiceClient, Task<string>> operation = _ =>
		{
			callCount++;
			if (callCount == 1)
			{
				throw new AuthenticationFailedException("MSAL cache is empty");
			}

			return Task.FromResult("retry-success");
		};

		var graphClientService = CreateGraphClientService();

		// Act
		var result = await graphClientService.ExecuteWithRetryAsync(operation, "work-account");

		// Assert — re-authentication must be triggered with forceInteractive: true
		result.Should().Be("retry-success");
		_mockAuthenticationService.Verify(
			authService => authService.AuthenticateAsync("work-account", It.IsAny<CancellationToken>(), true),
			Times.Once);
	}

	[Fact]
	public async Task ExecuteWithRetryAsync_WithAccountName_WhenOperationThrowsAuthenticationFailedAndReAuthReturnsFalse_ThrowsInvalidOperationException()
	{
		// Arrange
		var account = new Account("work-account", "user@contoso.com", AccountType.Work);
		_mockAccountService
			.Setup(accountService => accountService.GetAccountAsync("work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(account);

		var fakeGraphClient = BuildFakeGraphServiceClient();
		_mockGraphServiceClientBuilder
			.Setup(builder => builder.BuildAsync(account, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fakeGraphClient);

		_mockAuthenticationService
			.Setup(authService => authService.AuthenticateAsync("work-account", It.IsAny<CancellationToken>(), true))
			.ReturnsAsync(false);

		Func<GraphServiceClient, Task<string>> operation = _ =>
			throw new AuthenticationFailedException("MSAL cache is empty");

		var graphClientService = CreateGraphClientService();

		// Act
		var act = async () => await graphClientService.ExecuteWithRetryAsync(operation, "work-account");

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*Re-authentication failed*");
	}
}
