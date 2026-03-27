using System.Reflection;
using ClawMailCalCli.Commands;
using ClawMailCalCli.Models;
using ClawMailCalCli.Services.Interfaces;
using Spectre.Console.Cli;

namespace ClawMailCalCli.Tests.Commands;

/// <summary>
/// Unit tests for <see cref="DoctorCommand"/>.
/// </summary>
[Trait("Category", "Unit")]
public class DoctorCommandTests
{
	private readonly Mock<IDoctorService> _mockDoctorService;
	private readonly Mock<IOutputService> _mockOutputService;

	public DoctorCommandTests()
	{
		_mockDoctorService = new Mock<IDoctorService>();
		_mockOutputService = new Mock<IOutputService>();
	}

	private static CommandContext CreateCommandContext()
	{
		var remainingArguments = Mock.Of<IRemainingArguments>();
		return (CommandContext)Activator.CreateInstance(
			typeof(CommandContext),
			BindingFlags.Instance | BindingFlags.Public,
			binder: null,
			args: [Array.Empty<string>(), remainingArguments, "doctor", null],
			culture: null)!;
	}

	[Fact]
	public async Task ExecuteAsync_WhenAllChecksPassed_ReturnsZero()
	{
		// Arrange
		IReadOnlyList<DoctorCheckResult> checkResults =
		[
			new DoctorCheckResult("Azure CLI", true, "Version 2.0.0 found"),
			new DoctorCheckResult("Key Vault", true, "Reachable"),
		];

		_mockDoctorService
			.Setup(service => service.RunAllChecksAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(checkResults);

		var command = new DoctorCommand(_mockDoctorService.Object, _mockOutputService.Object);
		var settings = new DoctorCommand.Settings();
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
	}

	[Fact]
	public async Task ExecuteAsync_WhenOneCheckFailed_ReturnsOne()
	{
		// Arrange
		IReadOnlyList<DoctorCheckResult> checkResults =
		[
			new DoctorCheckResult("Azure CLI", true, "Version 2.0.0 found"),
			new DoctorCheckResult("Key Vault", false, "Not reachable", "Run az login"),
		];

		_mockDoctorService
			.Setup(service => service.RunAllChecksAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(checkResults);

		var command = new DoctorCommand(_mockDoctorService.Object, _mockOutputService.Object);
		var settings = new DoctorCommand.Settings();
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
	}

	[Fact]
	public async Task ExecuteAsync_WhenAllChecksFailed_ReturnsOne()
	{
		// Arrange
		IReadOnlyList<DoctorCheckResult> checkResults =
		[
			new DoctorCheckResult("Azure CLI", false, "Not installed", "Install Azure CLI"),
			new DoctorCheckResult("Key Vault", false, "Not reachable", "Run az login"),
		];

		_mockDoctorService
			.Setup(service => service.RunAllChecksAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(checkResults);

		var command = new DoctorCommand(_mockDoctorService.Object, _mockOutputService.Object);
		var settings = new DoctorCommand.Settings();
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
	}

	[Fact]
	public async Task ExecuteAsync_WhenCheckFailedWithNoFixHint_ReturnsOne()
	{
		// Arrange
		IReadOnlyList<DoctorCheckResult> checkResults =
		[
			new DoctorCheckResult("Azure CLI", false, "Unknown error"),
		];

		_mockDoctorService
			.Setup(service => service.RunAllChecksAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(checkResults);

		var command = new DoctorCommand(_mockDoctorService.Object, _mockOutputService.Object);
		var settings = new DoctorCommand.Settings();
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
	}

	[Fact]
	public async Task ExecuteAsync_WhenJsonAndAllChecksPassed_WritesJsonResultsAndReturnsZero()
	{
		// Arrange
		IReadOnlyList<DoctorCheckResult> checkResults =
		[
			new DoctorCheckResult("Azure CLI", true, "Version 2.0.0 found"),
			new DoctorCheckResult("Key Vault", true, "Reachable"),
		];

		_mockDoctorService
			.Setup(service => service.RunAllChecksAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(checkResults);

		var command = new DoctorCommand(_mockDoctorService.Object, _mockOutputService.Object);
		var settings = new DoctorCommand.Settings { Json = true };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(0);
		_mockOutputService.Verify(service => service.WriteJson(checkResults), Times.Once);
	}

	[Fact]
	public async Task ExecuteAsync_WhenJsonAndCheckFailed_WritesJsonResultsAndReturnsOne()
	{
		// Arrange
		IReadOnlyList<DoctorCheckResult> checkResults =
		[
			new DoctorCheckResult("Azure CLI", true, "Version 2.0.0 found"),
			new DoctorCheckResult("Key Vault", false, "Not reachable", "Run az login"),
		];

		_mockDoctorService
			.Setup(service => service.RunAllChecksAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(checkResults);

		var command = new DoctorCommand(_mockDoctorService.Object, _mockOutputService.Object);
		var settings = new DoctorCommand.Settings { Json = true };
		var context = CreateCommandContext();

		// Act
		var result = await command.ExecuteAsync(context, settings, CancellationToken.None);

		// Assert
		result.Should().Be(1);
		_mockOutputService.Verify(service => service.WriteJson(checkResults), Times.Once);
	}
}
