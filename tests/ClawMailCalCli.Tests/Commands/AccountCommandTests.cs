using System.Reflection;
using ClawMailCalCli.Commands.Account;
using ClawMailCalCli.Commands.Settings;
using ClawMailCalCli.Models;
using ClawMailCalCli.Services.Interfaces;
using Spectre.Console.Cli;

namespace ClawMailCalCli.Tests.Commands;

/// <summary>
/// Unit tests for account-related commands.
/// </summary>
[Trait("Category", "Unit")]
public class AccountCommandTests
{
	private readonly Mock<IAccountService> _mockAccountService;

	public AccountCommandTests()
	{
		_mockAccountService = new Mock<IAccountService>();
	}

	/// <summary>
	/// Creates a <see cref="CommandContext"/> using reflection since its constructor is internal
	/// to the Spectre.Console.Cli assembly.
	/// </summary>
	private static CommandContext CreateCommandContext()
	{
		var remainingArguments = Mock.Of<IRemainingArguments>();
		return (CommandContext)Activator.CreateInstance(
			typeof(CommandContext),
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
			binder: null,
			args: [Array.Empty<string>(), remainingArguments, "command", null],
			culture: null)!;
	}

	[Fact]
	public async Task AddAccountCommand_WhenAccountAddedSuccessfully_ReturnsZero()
	{
		// Arrange
		_mockAccountService
			.Setup(service => service.AddAccountAsync("new-account", "test@example.com", It.IsAny<AccountType>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var command = new AddAccountCommand(_mockAccountService.Object);
		var settings = new AddAccountSettings { Name = "new-account", Email = "test@example.com" };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
	}

	[Fact]
	public async Task AddAccountCommand_WhenTypeIsWork_PassesWorkTypeToService()
	{
		// Arrange
		_mockAccountService
			.Setup(service => service.AddAccountAsync("work-account", "user@contoso.com", AccountType.Work, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var command = new AddAccountCommand(_mockAccountService.Object);
		var settings = new AddAccountSettings { Name = "work-account", Email = "user@contoso.com", Type = AccountType.Work };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
		_mockAccountService.Verify(service => service.AddAccountAsync("work-account", "user@contoso.com", AccountType.Work, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task AddAccountCommand_WhenTypeIsNotSpecified_DefaultsToPersonal()
	{
		// Arrange
		_mockAccountService
			.Setup(service => service.AddAccountAsync("personal-account", "user@hotmail.com", AccountType.Personal, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var command = new AddAccountCommand(_mockAccountService.Object);
		var settings = new AddAccountSettings { Name = "personal-account", Email = "user@hotmail.com" };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
		_mockAccountService.Verify(service => service.AddAccountAsync("personal-account", "user@hotmail.com", AccountType.Personal, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task AddAccountCommand_WhenAccountAlreadyExists_ReturnsOne()
	{
		// Arrange
		_mockAccountService
			.Setup(service => service.AddAccountAsync("existing-account", "test@example.com", It.IsAny<AccountType>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		var command = new AddAccountCommand(_mockAccountService.Object);
		var settings = new AddAccountSettings { Name = "existing-account", Email = "test@example.com" };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
	}

	[Fact]
	public async Task DeleteAccountCommand_WhenAccountDeletedSuccessfully_ReturnsZero()
	{
		// Arrange
		_mockAccountService
			.Setup(service => service.DeleteAccountAsync("test-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var command = new DeleteAccountCommand(_mockAccountService.Object);
		var settings = new DeleteAccountSettings { Name = "test-account" };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
	}

	[Fact]
	public async Task DeleteAccountCommand_WhenAccountDoesNotExist_ReturnsOne()
	{
		// Arrange
		_mockAccountService
			.Setup(service => service.DeleteAccountAsync("nonexistent", It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		var command = new DeleteAccountCommand(_mockAccountService.Object);
		var settings = new DeleteAccountSettings { Name = "nonexistent" };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
	}

	[Fact]
	public async Task SetAccountCommand_WhenDefaultAccountSetSuccessfully_ReturnsZero()
	{
		// Arrange
		_mockAccountService
			.Setup(service => service.SetDefaultAccountAsync("work-account", It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var command = new SetAccountCommand(_mockAccountService.Object);
		var settings = new SetAccountSettings { Name = "work-account" };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
	}

	[Fact]
	public async Task SetAccountCommand_WhenAccountDoesNotExist_ReturnsOne()
	{
		// Arrange
		_mockAccountService
			.Setup(service => service.SetDefaultAccountAsync("nonexistent", It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		var command = new SetAccountCommand(_mockAccountService.Object);
		var settings = new SetAccountSettings { Name = "nonexistent" };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
	}

	[Fact]
	public async Task ListAccountsCommand_WhenNoAccounts_ReturnsZero()
	{
		// Arrange
		_mockAccountService
			.Setup(service => service.ListAccountsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var command = new ListAccountsCommand(_mockAccountService.Object);
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, CancellationToken.None);

		// Assert
		result.Should().Be(0);
	}

	[Fact]
	public async Task ListAccountsCommand_WhenAccountsExist_ReturnsZero()
	{
		// Arrange
		IReadOnlyList<Account> accounts =
		[
			new Account("work-account", "user@contoso.com", AccountType.Work),
			new Account("personal-account", "user@hotmail.com", AccountType.Personal),
		];

		_mockAccountService
			.Setup(service => service.ListAccountsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(accounts);

		var command = new ListAccountsCommand(_mockAccountService.Object);
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, CancellationToken.None);

		// Assert
		result.Should().Be(0);
	}
}
