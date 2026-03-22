var app = new CommandApp<DefaultCommand>();

app.Configure(config =>
{
	config.SetApplicationName("claw-mail-cal-cli");
});

return app.Run(args);
