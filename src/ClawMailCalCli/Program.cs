using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using ClawMailCalCli;
using ClawMailCalCli.Models;
using ClawMailCalCli.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var configuration = new ConfigurationBuilder()
	.SetBasePath(AppContext.BaseDirectory)
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
	.AddEnvironmentVariables()
	.Build();

var services = new ServiceCollection();

services.Configure<EntraOptions>(configuration.GetSection("entra"));
services.Configure<KeyVaultOptions>(configuration.GetSection("keyVault"));

services.AddLogging();

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
services.AddSingleton<IAccountService, AccountService>();
services.AddSingleton<IDeviceCodeCredentialProvider, DeviceCodeCredentialProvider>();
services.AddSingleton<IAuthenticationService, AuthenticationService>();

var registrar = new TypeRegistrar(services);
var app = new CommandApp<DefaultCommand>(registrar);

app.Configure(config =>
{
	config.SetApplicationName("claw-mail-cal-cli");
	config.AddCommand<LoginCommand>("login")
		.WithDescription("Authenticate an account using the Entra ID device code flow.")
		.WithExample("login", "my-account");
});

return app.Run(args);
