using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
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

	public GraphClientServiceTests()
	{
		_mockAccountService = new Mock<IAccountService>();
		_mockGraphServiceClientBuilder = new Mock<IGraphServiceClientBuilder>();
		_mockAuthenticationService = new Mock<IAuthenticationService>();
		_logger = new NullLogger<GraphClientService>();
	}

	private GraphClientService CreateGraphClientService() =>
		new GraphClientService(
			_mockAccountService.Object,
			_mockGraphServiceClientBuilder.Object,
			_mockAuthenticationService.Object,
			_logger);

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
			.Returns(Task.CompletedTask);

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
			.Returns(Task.CompletedTask);

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
			.Returns(Task.CompletedTask);

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
			.Returns(Task.CompletedTask);

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
}
