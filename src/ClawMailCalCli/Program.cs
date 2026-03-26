using System.Reflection;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using ClawMailCalCli;
using ClawMailCalCli.Data;
using ClawMailCalCli.Logging;
using ClawMailCalCli.Services;
using ClawMailCalCli.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var verbosityLevel = ParseVerbosityLevel(args);
var minimumLogLevel = MapToLogLevel(verbosityLevel);
var services = new ServiceCollection();

services.AddLogging(loggingBuilder =>
{
	loggingBuilder.SetMinimumLevel(minimumLogLevel);
	loggingBuilder.AddProvider(new StderrLoggerProvider(minimumLogLevel));
});

// SQLite database for account data (names, emails, default selection).
// Key Vault is reserved for secrets such as OAuth tokens.
var dbDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claw-mail-cal-cli");
Directory.CreateDirectory(dbDirectory);
TokenCacheFileProtector.ProtectCacheDirectory(dbDirectory);

// Azure.Identity's persistent MSAL token cache on Linux is stored under ~/.IdentityService by default.
// Protect this directory as well so token cache files inherit hardened permissions.
var identityServiceDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".IdentityService");
Directory.CreateDirectory(identityServiceDirectory);
TokenCacheFileProtector.ProtectCacheDirectory(identityServiceDirectory);
var dbPath = Path.Combine(dbDirectory, "accounts.db");

services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

// IConfigurationService is registered as a singleton so all services share the same instance.
services.AddSingleton<IConfigurationService, ConfigurationService>();

// SecretClient is registered as a lazy factory so that commands that do not need Key Vault
// (e.g. --help, --version, account add) can run without a config.json being present.
// Using GetAwaiter().GetResult() here is safe because console apps have no SynchronizationContext
// and DI factories always run synchronously, eliminating any deadlock risk.
services.AddSingleton(serviceProvider =>
{
	var configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
	var clawConfiguration = configurationService.ReadConfigurationAsync().ConfigureAwait(false).GetAwaiter().GetResult();

	if (string.IsNullOrWhiteSpace(clawConfiguration.KeyVaultUri))
	{
		throw new InvalidOperationException("'keyVaultUri' is not set in '~/.claw-mail-cal-cli/config.json'. Set this value before running any commands that require Key Vault access.");
	}

	if (!Uri.TryCreate(clawConfiguration.KeyVaultUri, UriKind.Absolute, out var vaultUri))
	{
		throw new InvalidOperationException($"'keyVaultUri' value '{clawConfiguration.KeyVaultUri}' in '~/.claw-mail-cal-cli/config.json' is not a valid absolute URI. Provide a URI in the format 'https://my-vault.vault.azure.net/'.");
	}

	return new SecretClient(vaultUri, new AzureCliCredential());
});

services.AddSingleton<IKeyVaultService, KeyVaultService>();
services.AddTransient<IAccountService, AccountService>();
services.AddSingleton<IDeviceCodeCredentialProvider, DeviceCodeCredentialProvider>();
services.AddSingleton<IAuthenticationService, AuthenticationService>();
services.AddTransient<ICalendarGraphService, CalendarGraphService>();
services.AddSingleton<IGraphServiceClientBuilder, GraphServiceClientBuilder>();
services.AddSingleton<IGraphClientService, GraphClientService>();
services.AddTransient<ICalendarService, CalendarService>();
services.AddTransient<IEmailService, EmailService>();
services.AddSingleton<IAzureCliChecker, AzureCliChecker>();
services.AddSingleton<IKeyVaultChecker, KeyVaultChecker>();
services.AddTransient<IDoctorService, DoctorService>();
services.AddSingleton<IOutputService, OutputService>();

// Ensure the SQLite schema is up to date before running any commands.
await using (var startupContext = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
	.UseSqlite($"Data Source={dbPath}")
	.Options))
{
	await startupContext.Database.EnsureCreatedAsync();
}

var registrar = new TypeRegistrar(services);
var app = new CommandApp<DefaultCommand>(registrar);

var applicationVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";

app.Configure(config =>
{
	config.SetApplicationName("claw-mail-cal-cli");
	config.SetApplicationVersion(applicationVersion);
	config.UseStrictParsing();

	config.AddBranch("account", account =>
	{
		account.AddCommand<AddAccountCommand>("add")
			.WithDescription("Add a new account.")
			.WithExample("account add myaccount user@example.com");
		account.AddCommand<ListAccountsCommand>("list")
			.WithDescription("List all accounts.");
		account.AddCommand<DeleteAccountCommand>("delete")
			.WithDescription("Delete an account.")
			.WithExample("account delete myaccount");
		account.AddCommand<SetAccountCommand>("set")
			.WithDescription("Set the default account.")
			.WithExample("account set myaccount");
	});

	config.AddBranch("calendar", calendar =>
	{
		calendar.AddCommand<ReadCalendarCommand>("read")
			.WithDescription("Read a calendar event by title or event ID.")
			.WithExample("calendar read \"Team Meeting\"");
		calendar.AddCommand<ListCalendarCommand>("list")
			.WithDescription("List the next 20 upcoming calendar events.")
			.WithExample("calendar list");
		calendar.AddCommand<CreateCalendarCommand>("create")
			.WithDescription("Create a new calendar event.")
			.WithExample("calendar create \"Team Meeting\" \"2026-03-25T09:00:00\" \"2026-03-25T09:30:00\" \"Weekly team sync\"");
	});

	config.AddCommand<LoginCommand>("login")
	.WithDescription("Authenticate an account using the Entra ID device code flow.")
	.WithExample("login", "my-account");

	config.AddCommand<DoctorCommand>("doctor")
	.WithDescription("Check the developer environment for required prerequisites.")
	.WithExample("doctor");

	config.AddBranch("email", email =>
	{
		email.AddCommand<SendEmailCommand>("send")
			.WithDescription("Send an email via Microsoft Graph.")
			.WithExample("email send user@example.com \"Hello\" \"This is the body.\"");
		email.AddCommand<ListEmailCommand>("list")
			.WithDescription("List the 20 most recent messages from the inbox or a named folder.")
			.WithExample("email list")
			.WithExample("email list", "sentitems");
		email.AddCommand<ReadEmailCommand>("read")
			.WithDescription("Read an email by subject or message ID.")
			.WithExample("email", "read", "my-account", "Meeting notes");
	});
});

return app.Run(StripVerbosityFlag(args));

/// <summary>
/// Parses the <c>--verbosity</c> option from the raw argument list.
/// Defaults to <see cref="VerbosityLevel.Normal"/> when the option is absent or unrecognised.
/// </summary>
static VerbosityLevel ParseVerbosityLevel(string[] arguments)
{
	for (var argumentIndex = 0; argumentIndex < arguments.Length - 1; argumentIndex++)
	{
		if (arguments[argumentIndex] == "--verbosity")
		{
			return arguments[argumentIndex + 1].ToLowerInvariant() switch
			{
				"quiet" => VerbosityLevel.Quiet,
				"debug" => VerbosityLevel.Debug,
				_ => VerbosityLevel.Normal,
			};
		}
	}

	return VerbosityLevel.Normal;
}

/// <summary>
/// Returns a new argument array with the <c>--verbosity &lt;value&gt;</c> pair removed so that
/// Spectre.Console.Cli strict parsing does not reject the unknown flag.
/// </summary>
static string[] StripVerbosityFlag(string[] arguments)
{
	var filtered = new List<string>(arguments.Length);
	for (var argumentIndex = 0; argumentIndex < arguments.Length; argumentIndex++)
	{
		if (arguments[argumentIndex] == "--verbosity" && argumentIndex + 1 < arguments.Length)
		{
			var potentialVerbosityValue = arguments[argumentIndex + 1].ToLowerInvariant();
			if (potentialVerbosityValue is "quiet" or "normal" or "debug")
			{
				argumentIndex++; // skip the value as well
				continue;
			}
		}

		filtered.Add(arguments[argumentIndex]);
	}

	return [.. filtered];
}

/// <summary>
/// Maps a <see cref="VerbosityLevel"/> to the corresponding <see cref="LogLevel"/> minimum threshold.
/// </summary>
static LogLevel MapToLogLevel(VerbosityLevel verbosityLevel) => verbosityLevel switch
{
	VerbosityLevel.Quiet => LogLevel.Error,
	VerbosityLevel.Debug => LogLevel.Debug,
	_ => LogLevel.Warning,
};
