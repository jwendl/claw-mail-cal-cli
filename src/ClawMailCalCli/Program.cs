using Azure.Security.KeyVault.Secrets;
using ClawMailCalCli;
using ClawMailCalCli.Commands.Account;
using ClawMailCalCli.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

var services = new ServiceCollection();

services.AddLogging();
services.AddSingleton<IConfigurationService, ConfigurationService>();

// Resolve Key Vault URI: config file takes precedence, KEYVAULT_URI env var is the fallback.
Uri keyVaultUriToUse;

var configurationService = new ConfigurationService();
try
{
	var clawConfiguration = await configurationService.ReadConfigurationAsync();
	keyVaultUriToUse = new Uri(clawConfiguration.KeyVaultUri);
}
catch (Exception exception) when (exception is InvalidOperationException or System.Text.Json.JsonException)
{
	var keyVaultUri = Environment.GetEnvironmentVariable("KEYVAULT_URI");

	if (string.IsNullOrWhiteSpace(keyVaultUri))
	{
		AnsiConsole.MarkupLine("[red]✗ No Key Vault URI configured.[/]");
		AnsiConsole.MarkupLine("[yellow]- Create ~/.claw-mail-cal-cli/config.json with a 'keyVaultUri' entry, or set the KEYVAULT_URI environment variable.[/]");
		return 1;
	}

	if (!Uri.TryCreate(keyVaultUri, UriKind.Absolute, out var keyVaultUriParsed))
	{
		AnsiConsole.MarkupLine("[red]✗ Invalid KEYVAULT_URI environment variable value: '{0}'[/]", Markup.Escape(keyVaultUri));
		AnsiConsole.MarkupLine("[yellow]- Provide a valid absolute URI or configure ~/.claw-mail-cal-cli/config.json with a 'keyVaultUri' entry.[/]");
		return 1;
	}

	keyVaultUriToUse = keyVaultUriParsed;
}

services.AddSingleton(_ => new SecretClient(keyVaultUriToUse, new Azure.Identity.DefaultAzureCredential()));
services.AddSingleton<ISecretStore, KeyVaultSecretStore>();
services.AddScoped<IAccountService, AccountService>();

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
});

return app.Run(args);
