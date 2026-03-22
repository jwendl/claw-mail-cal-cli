var app = new CommandApp();

app.Configure(config =>
{
	config.SetApplicationName("claw-mail-cal-cli");
	config.AddCommand<DefaultCommand>("run")
		.WithDescription("Run the claw-mail-cal-cli application.");
});

return app.Run(args);
