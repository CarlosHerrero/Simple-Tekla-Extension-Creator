using Simple_Tekla_Extension_Creator.Core;

namespace Simple_Tekla_Extension_Creator.Tool
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (!TryParseArguments(args, out CliOptions options, out string parseError))
            {
                if (!string.IsNullOrEmpty(parseError))
                {
                    Console.Error.WriteLine(parseError);
                }

                PrintHelp();
                return string.IsNullOrEmpty(parseError) ? 0 : 1;
            }

            if (!ProjectGenerator.EnsureAllBaseFoldersAndProps(out string bootstrapError))
            {
                Console.Error.WriteLine(bootstrapError);
                return 1;
            }

            var generator = new ProjectGenerator();

            if (!string.IsNullOrWhiteSpace(options.MacroFile))
            {
                if (!generator.TryLoadMacroFile(options.MacroFile, out string macroError))
                {
                    Console.Error.WriteLine(macroError);
                    return 1;
                }
            }

            string normalizedVersion = ProjectGenerator.NormalizeTeklaVersion(options.Version);

            bool created = generator.TryCreateProject(
                options.ProjectName!,
                options.Ui,
                normalizedVersion,
                bootstrapFailed: false,
                bootstrapError: string.Empty,
                out string projectPath,
                out string message);

            if (!created)
            {
                Console.Error.WriteLine(message);
                return 1;
            }

            Console.WriteLine(message);

            if (options.Open)
            {
                string csprojFile = Path.Combine(projectPath, $"{options.ProjectName}.csproj");
                ProjectLauncher.OpenCreatedProject(projectPath, csprojFile);
            }

            return 0;
        }

        private static bool TryParseArguments(string[] args, out CliOptions options, out string error)
        {
            error = string.Empty;
            string? projectName = null;
            string version = "2026";
            string ui = "Console";
            bool open = false;
            string? macroFile = null;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--project":
                        projectName = GetValue(args, ref i);
                        break;
                    case "--version":
                        version = GetValue(args, ref i) ?? version;
                        break;
                    case "--ui":
                        ui = GetValue(args, ref i) ?? ui;
                        break;
                    case "--macro":
                        macroFile = GetValue(args, ref i);
                        break;
                    case "--open":
                        open = true;
                        break;
                    case "-h":
                    case "--help":
                        options = default!;
                        error = string.Empty;
                        return false;
                    default:
                        error = $"Unknown argument: {args[i]}";
                        options = default!;
                        return false;
                }
            }

            if (string.IsNullOrWhiteSpace(projectName))
            {
                error = "Missing required argument --project.";
                options = default!;
                return false;
            }

            options = new CliOptions(projectName, version, ui, open, macroFile);
            return true;
        }

        private static string? GetValue(string[] args, ref int i)
        {
            if (i + 1 >= args.Length)
            {
                return null;
            }

            i++;
            return args[i];
        }

        private static void PrintHelp()
        {
            Console.WriteLine("""
            Simple Tekla Extension Creator - scaffolds a Tekla Structures extension project.

            Usage:
              simple-tekla-extension-creator --project <name> [--version <2023|2024|2025|2026|2027>]
                                              [--ui <Console|WinForms|WPF>] [--macro <path-to-macro.cs>] [--open]

            Options:
              --project <name>   Required. Name of the project to create.
              --version <ver>    Tekla Structures version. Default: 2026.
              --ui <type>        Console, WinForms, or WPF. Default: Console.
              --macro <path>     Path to a Tekla Open API macro (.cs) file whose 'Run' method body
                                 (and any helper classes/methods) is copied into the generated project's
                                 OpenApiCode.cs, replacing the default beam-insertion example.
              --open             Open the generated project after creation.
              -h, --help         Show this help message.
            """);
        }

        private sealed record CliOptions(string? ProjectName, string Version, string Ui, bool Open, string? MacroFile);
    }
}
