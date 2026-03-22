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
	var vaultUri = new Uri(keyVaultOptions.VaultUri);
	return new SecretClient(vaultUri, new AzureCliCredential());
});

services.AddSingleton<IKeyVaultService, KeyVaultService>();
services.AddSingleton<IAccountService, AccountService>();
services.AddSingleton<IDeviceCodeCredentialProvider, DeviceCodeCredentialProvider>();
services.AddSingleton<IAuthenticationService, AuthenticationService>();

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
	config.SetApplicationName("claw-mail-cal-cli");
	config.AddCommand<DefaultCommand>("default");
	config.AddCommand<LoginCommand>("login")
		.WithDescription("Authenticate an account using the Entra ID device code flow.")
		.WithExample("login", "my-account");
});

return app.Run(args);
