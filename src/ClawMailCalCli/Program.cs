using Azure.Security.KeyVault.Secrets;
using ClawMailCalCli;
using ClawMailCalCli.Commands.Account;
using ClawMailCalCli.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

var services = new ServiceCollection();

services.AddLogging();

var keyVaultUri = Environment.GetEnvironmentVariable("KEYVAULT_URI");
Uri? keyVaultUriParsed = null;

if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
	if (!Uri.TryCreate(keyVaultUri, UriKind.Absolute, out keyVaultUriParsed))
	{
		AnsiConsole.MarkupLine("[red]✗ Invalid KEYVAULT_URI environment variable value: '{0}'[/]", keyVaultUri);
		AnsiConsole.MarkupLine("[yellow]- Please provide a valid absolute URI or unset KEYVAULT_URI to use the placeholder vault.[/]");
		return 1;
	}
}

var keyVaultUriToUse = keyVaultUriParsed ?? new Uri("https://placeholder.vault.azure.net/");

services.AddSingleton(_ => new SecretClient(keyVaultUriToUse, new Azure.Identity.DefaultAzureCredential()));
services.AddSingleton<ISecretStore, KeyVaultSecretStore>();
services.AddScoped<IAccountService, AccountService>();

var registrar = new TypeRegistrar(services);
var app = new CommandApp<DefaultCommand>(registrar);

app.Configure(config =>
{
	config.SetApplicationName("claw-mail-cal-cli");

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
