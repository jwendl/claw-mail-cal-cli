using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using ClawMailCalCli;
using ClawMailCalCli.Commands.Calendar;
using ClawMailCalCli.Data;
using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var configuration = new ConfigurationBuilder()
.SetBasePath(AppContext.BaseDirectory)
.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
.AddEnvironmentVariables()
.Build();

var services = new ServiceCollection();

services.Configure<KeyVaultOptions>(configuration.GetSection("keyVault"));

services.AddLogging();

// SQLite database for account data (names, emails, default selection).
// Key Vault is reserved for secrets such as OAuth tokens.
var dbDirectory = Path.Combine(
Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
".claw-mail-cal-cli");
Directory.CreateDirectory(dbDirectory);
var dbPath = Path.Combine(dbDirectory, "accounts.db");

services.AddDbContextFactory<ApplicationDbContext>(options =>
options.UseSqlite($"Data Source={dbPath}"));

// Key Vault client for OAuth token storage (not account data).
services.AddSingleton(serviceProvider =>
{
	var keyVaultOptions = serviceProvider.GetRequiredService<IOptions<KeyVaultOptions>>().Value;
	if (string.IsNullOrWhiteSpace(keyVaultOptions.VaultUri))
	{
		throw new InvalidOperationException("'keyVault:vaultUri' is not configured. Set this value before running any commands that require Key Vault access.");
	}

	if (!Uri.TryCreate(keyVaultOptions.VaultUri, UriKind.Absolute, out var vaultUri))
	{
		throw new InvalidOperationException($"'keyVault:vaultUri' value '{keyVaultOptions.VaultUri}' is not a valid absolute URI. Provide a URI in the format 'https://my-vault.vault.azure.net/'.");
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
services.AddSingleton<IConfigurationService, ConfigurationService>();
services.AddSingleton<IProcessRunner, ProcessRunner>();
services.AddSingleton<IKeyVaultChecker, KeyVaultChecker>();
services.AddTransient<IDoctorService, DoctorService>();

// Ensure the SQLite schema is up to date before running any commands.
await using (var startupContext = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
.UseSqlite($"Data Source={dbPath}")
.Options))
{
	await startupContext.Database.EnsureCreatedAsync();
}

var registrar = new TypeRegistrar(services);
var app = new CommandApp<DefaultCommand>(registrar);

app.Configure(config =>
{
	config.SetApplicationName("claw-mail-cal-cli");
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

return app.Run(args);
