var app = new CommandApp<DefaultCommand>();

app.Configure(config =>
{
	config.SetApplicationName("claw-mail-cal-cli");
	config.UseStrictParsing();

	config.Settings.Registrar.Register<IConfigurationService, ConfigurationService>();
});

return app.Run(args);
