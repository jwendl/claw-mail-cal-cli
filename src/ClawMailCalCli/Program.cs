using Azure.Security.KeyVault.Secrets;
using ClawMailCalCli;
using ClawMailCalCli.Commands.Account;
using ClawMailCalCli.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();

services.AddLogging(builder => builder.AddConsole());

var keyVaultUri = Environment.GetEnvironmentVariable("KEYVAULT_URI");
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
	services.AddSingleton(_ => new SecretClient(new Uri(keyVaultUri), new Azure.Identity.DefaultAzureCredential()));
}
else
{
	services.AddSingleton(_ => new SecretClient(new Uri("https://placeholder.vault.azure.net/"), new Azure.Identity.DefaultAzureCredential()));
}

services.AddSingleton<ISecretStore, KeyVaultSecretStore>();
services.AddScoped<IAccountService, AccountService>();

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

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
