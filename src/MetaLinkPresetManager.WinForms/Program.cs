using MetaLinkPresetManager.Core.Infrastructure;
using MetaLinkPresetManager.Core.Services;

namespace MetaLinkPresetManager.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var paths = new AppPaths();
        paths.EnsureCreated();
        var logger = new FileLogger(paths.LogsFolder);

        try
        {
            var settingsRepository = new JsonAppSettingsRepository(paths);
            var presetRepository = new JsonPresetRepository(paths);
            var settings = settingsRepository.LoadAsync().GetAwaiter().GetResult();
            var store = presetRepository.LoadAsync().GetAwaiter().GetResult();
            var commandBuilder = new OculusCommandFileBuilder();
            var debugToolService = new OculusDebugToolService(
                settings,
                paths,
                commandBuilder,
                logger);
            var launcher = new GameLauncher(logger);
            var watcher = new ProcessWatcherService(
                () => store.Presets,
                TimeSpan.FromSeconds(settings.ProcessPollingIntervalSeconds),
                logger);

            logger.Info("Application started.");
            logger.Info($"Loaded settings: {paths.SettingsFile}");
            logger.Info($"Loaded presets: {paths.PresetsFile}");

            Application.Run(new MainForm(
                settings,
                store,
                settingsRepository,
                presetRepository,
                debugToolService,
                launcher,
                watcher,
                logger));
        }
        catch (Exception exception)
        {
            logger.Error("Application startup failed", exception);
            MessageBox.Show(
                $"Meta Link Preset Manager could not start.{Environment.NewLine}{Environment.NewLine}{exception.Message}",
                "Startup failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
