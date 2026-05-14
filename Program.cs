var settingsFilePath = Path.Combine(AppContext.BaseDirectory, "sync-settings.json");

ISettingsStore settingsStore = new JsonSettingsStore(settingsFilePath);
IUserPrompt userPrompt = new ConsolePrompt();
ISyncLogger syncLogger = new ConsoleSyncLogger();

var configurationBuilder = new SyncConfigurationBuilder(userPrompt);
var syncEngine = new SyncEngine(syncLogger);
var application = new SyncApplication(settingsStore, configurationBuilder, syncEngine, syncLogger, userPrompt);

return await application.RunAsync(args);
