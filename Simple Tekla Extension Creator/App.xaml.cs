using System.Windows;

namespace Simple_Tekla_Extension_Creator
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var options = ParseArguments(e.Args);
            if (!string.IsNullOrEmpty(options.Error))
            {
                if (options.NonInteractive)
                {
                    Console.Error.WriteLine(options.Error);
                    Environment.ExitCode = 1;
                }
                else
                {
                    MessageBox.Show(options.Error, "Invalid arguments", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                Shutdown();
                return;
            }

            if (options.ShowHelp)
            {
                string help = "Usage:\n"
                    + "SimpleTeklaExtensionCreator.exe --version <2023|2024|2025|2026|2027> --ui <Console|WinForms|WPF>\n"
                    + "SimpleTeklaExtensionCreator.exe --non-interactive --project <Name> [--version <...>] [--ui <...>] [--open] [--macro <path-to-cs-file>]";

                if (options.NonInteractive)
                {
                    Console.WriteLine(help);
                }
                else
                {
                    MessageBox.Show(help, "Command-line options", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                Shutdown();
                return;
            }

            if (options.NonInteractive)
            {
                if (string.IsNullOrWhiteSpace(options.ProjectName))
                {
                    Console.Error.WriteLine("Missing required option '--project' for non-interactive mode.");
                    Environment.ExitCode = 1;
                    Shutdown();
                    return;
                }

                string version = options.Version ?? "2026";
                string ui = options.Ui ?? "Console";
                string result = string.Empty;

                MainWindow = new MainWindow(version, ui, interactive: false);
                bool created = MainWindow is MainWindow window
                    && window.TryCreateProjectNonInteractive(options.ProjectName, ui, version, options.OpenProject, options.MacroFilePath, out result);

                if (created)
                {
                    Console.WriteLine(result);
                    Environment.ExitCode = 0;
                }
                else
                {
                    Console.Error.WriteLine(result);
                    Environment.ExitCode = 1;
                }

                Shutdown();
                return;
            }

            MainWindow = new MainWindow(options.Version, options.Ui);
            MainWindow.Show();
        }

        private static CliOptions ParseArguments(string[] args)
        {
            string? version = null;
            string? ui = null;
            string? projectName = null;
            string? macroFilePath = null;
            bool nonInteractive = false;
            bool openProject = false;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase) || arg.Equals("-h", StringComparison.OrdinalIgnoreCase) || arg.Equals("/?", StringComparison.OrdinalIgnoreCase))
                {
                    return new CliOptions(version, ui, projectName, macroFilePath, nonInteractive, openProject, true, null);
                }

                if (!arg.StartsWith("--", StringComparison.Ordinal))
                {
                    return new CliOptions(version, ui, projectName, macroFilePath, nonInteractive, openProject, false, $"Unexpected argument '{arg}'.");
                }

                string key;
                string? value = null;
                int equalsIndex = arg.IndexOf('=');
                if (equalsIndex > 2)
                {
                    key = arg[2..equalsIndex];
                    value = arg[(equalsIndex + 1)..];
                }
                else
                {
                    key = arg[2..];

                    bool requiresValue = !key.Equals("non-interactive", StringComparison.OrdinalIgnoreCase)
                        && !key.Equals("noninteractive", StringComparison.OrdinalIgnoreCase)
                        && !key.Equals("create", StringComparison.OrdinalIgnoreCase)
                        && !key.Equals("headless", StringComparison.OrdinalIgnoreCase)
                        && !key.Equals("open", StringComparison.OrdinalIgnoreCase);

                    if (requiresValue)
                    {
                        if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                        {
                            return new CliOptions(version, ui, projectName, macroFilePath, nonInteractive, openProject, false, $"Missing value for '--{key}'.");
                        }

                        value = args[++i];
                    }
                }

                if (key.Equals("version", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return new CliOptions(version, ui, projectName, macroFilePath, nonInteractive, openProject, false, "Missing value for '--version'.");
                    }

                    version = value;
                    continue;
                }

                if (key.Equals("ui", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return new CliOptions(version, ui, projectName, macroFilePath, nonInteractive, openProject, false, "Missing value for '--ui'.");
                    }

                    ui = value;
                    continue;
                }

                if (key.Equals("project", StringComparison.OrdinalIgnoreCase)
                    || key.Equals("projectname", StringComparison.OrdinalIgnoreCase)
                    || key.Equals("name", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return new CliOptions(version, ui, projectName, macroFilePath, nonInteractive, openProject, false, "Missing value for '--project'.");
                    }

                    projectName = value;
                    continue;
                }

                if (key.Equals("macro", StringComparison.OrdinalIgnoreCase)
                    || key.Equals("macrofile", StringComparison.OrdinalIgnoreCase)
                    || key.Equals("macropath", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return new CliOptions(version, ui, projectName, macroFilePath, nonInteractive, openProject, false, "Missing value for '--macro'.");
                    }

                    macroFilePath = value;
                    continue;
                }

                if (key.Equals("non-interactive", StringComparison.OrdinalIgnoreCase)
                    || key.Equals("noninteractive", StringComparison.OrdinalIgnoreCase)
                    || key.Equals("create", StringComparison.OrdinalIgnoreCase)
                    || key.Equals("headless", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        nonInteractive = true;
                        continue;
                    }

                    if (bool.TryParse(value, out bool parsedBool))
                    {
                        nonInteractive = parsedBool;
                        continue;
                    }

                    return new CliOptions(version, ui, projectName, macroFilePath, nonInteractive, openProject, false, $"Invalid boolean value '{value}' for '--{key}'.");
                }

                if (key.Equals("open", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        openProject = true;
                        continue;
                    }

                    if (bool.TryParse(value, out bool parsedOpen))
                    {
                        openProject = parsedOpen;
                        continue;
                    }

                    return new CliOptions(version, ui, projectName, macroFilePath, nonInteractive, openProject, false, $"Invalid boolean value '{value}' for '--{key}'.");
                }

                return new CliOptions(version, ui, projectName, macroFilePath, nonInteractive, openProject, false, $"Unknown option '--{key}'.");
            }

            return new CliOptions(version, ui, projectName, macroFilePath, nonInteractive, openProject, false, null);
        }

        private sealed record CliOptions(
            string? Version,
            string? Ui,
            string? ProjectName,
            string? MacroFilePath,
            bool NonInteractive,
            bool OpenProject,
            bool ShowHelp,
            string? Error);
    }

}
