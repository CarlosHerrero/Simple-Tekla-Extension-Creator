using System.Text.RegularExpressions;

namespace Simple_Tekla_Extension_Creator.Core
{
    /// <summary>
    /// Shared, UI-independent logic used to scaffold a new simple Tekla extension project:
    /// base folder/props bootstrap, Tekla Open API macro-file extraction, and generation of the
    /// csproj/Program.cs/WinForms/WPF/OpenApiCode.cs files. Used by both the interactive WPF app
    /// and the headless CLI tool.
    /// </summary>
    public class ProjectGenerator
    {
        /// <summary>
        /// Every supported Tekla version, mapped to its base repos folder name.
        /// </summary>
        public static readonly (string Version, string Folder)[] AllTeklaVersionFolders =
        [
            ("2023", "2023"),
            ("2024", "2024"),
            ("2025", "2025"),
            ("2026", "2026"),
            ("2027 dailybuild", "2027 Daily"),
        ];

        public string? MacroFilePath { get; private set; }
        public string? MacroBody { get; private set; }
        public string MacroExtraCode { get; private set; } = string.Empty;
        public List<string> MacroUsings { get; private set; } = [];

        public bool HasCustomMacro => !string.IsNullOrEmpty(MacroBody);

        /// <summary>
        /// Ensures that the base repos folder for every supported Tekla version exists, and that each
        /// (except 2023, which does not require one) has a Directory.Build.Props file.
        /// </summary>
        public static bool EnsureAllBaseFoldersAndProps(out string errorMessage)
        {
            errorMessage = string.Empty;
            string userName = Environment.UserName;
            string reposRoot = $@"C:\Users\{userName}\source\repos";

            foreach ((string version, string folder) in AllTeklaVersionFolders)
            {
                string folderPath = Path.Combine(reposRoot, folder);

                try
                {
                    Directory.CreateDirectory(folderPath);

                    if (version != "2023")
                    {
                        string propsPath = Path.Combine(folderPath, "Directory.Build.Props");
                        if (!File.Exists(propsPath))
                        {
                            File.WriteAllText(propsPath, GetDirectoryBuildPropsTemplate(version));
                        }
                    }
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                {
                    errorMessage = $"This application cannot be used because it lacks write access to '{folderPath}'. " +
                        "Please try running the application in administrator mode.";
                    return false;
                }
            }

            return true;
        }

        public static string GetDirectoryBuildPropsTemplate(string teklaVersion)
        {
            string installationPath = teklaVersion == "2027 dailybuild"
                ? @"C:\Program Files\Tekla Structures\2027.0 Daily\bin"
                : $@"C:\Program Files\Tekla Structures\{teklaVersion}.0\bin";

            string platformGroup = teklaVersion == "2027 dailybuild"
                ? "\r\n\t<PropertyGroup>\r\n        <PlatformTarget>x64</PlatformTarget>\r\n        <Platforms>x64</Platforms>\r\n\t</PropertyGroup>\r\n"
                : string.Empty;

            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
                "<Project ToolsVersion=\"14.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\" >\r\n" +
                "\t<PropertyGroup>\r\n" +
                "\t\t<!-- Force configuration to be debug in case none provided, happens if running from command line: example: dotnet build -->\r\n" +
                "\t\t<Configuration Condition=\"'$(Configuration)' == ''\">Debug</Configuration>\r\n" +
                $"\t\t<TeklaStructuresInstallationPath Condition=\"'$(Configuration)' == 'Debug'\">{installationPath}</TeklaStructuresInstallationPath>\r\n" +
                "\t\t<!-- Or Tekla Version can be used instead -->\r\n" +
                "\t\t<!--TeklaVersion Condition=\"'$(Configuration)' == 'Debug'\">2024.0</TeklaVersion-->\r\n" +
                "\t</PropertyGroup>\r\n" +
                platformGroup +
                "</Project>\r\n";
        }

        public static string NormalizeTeklaVersion(string version)
        {
            return version.Trim().Equals("2027", StringComparison.OrdinalIgnoreCase)
                ? "2027 dailybuild"
                : version.Trim();
        }

        public static string GetReposBasePath(string teklaVersionFolder)
        {
            string userName = Environment.UserName;
            string folder = teklaVersionFolder == "2027 dailybuild" ? "2027 Daily" : teklaVersionFolder;
            return Path.Combine($@"C:\Users\{userName}\source\repos", folder);
        }

        public static string GetProjectPath(string teklaVersionFolder, string projectName)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                return string.Empty;
            }

            return Path.Combine(GetReposBasePath(teklaVersionFolder), projectName);
        }

        public static string GetMacrosDefaultFolder(string teklaVersionFolder)
        {
            string versionFolder = teklaVersionFolder == "2027 dailybuild" ? "2027.0 Daily" : $"{teklaVersionFolder}.0";
            return $@"C:\ProgramData\Trimble\Tekla Structures\{versionFolder}\Environments\common\macros\modeling";
        }

        public bool TryLoadMacroFile(string filePath, out string error)
        {
            error = string.Empty;

            if (!File.Exists(filePath))
            {
                error = $"Macro file not found: '{filePath}'.";
                return false;
            }

            try
            {
                (List<string> usings, string body, string extraCode) = ExtractMacroRunBody(filePath);
                MacroUsings = usings;
                MacroBody = body;
                MacroExtraCode = extraCode;
                MacroFilePath = filePath;
                return true;
            }
            catch (Exception ex)
            {
                error = $"Could not read Open API code from '{filePath}': {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Reads a Tekla macro (.cs) file and extracts the code found inside its 'Run' method body,
        /// along with any 'using' directives declared at the top of the file, and any additional
        /// helper methods/classes declared in the file outside of the wrapper class that only
        /// contains the 'Run' entry point (e.g. 'Script' in classic UserScript macros, or 'Macro' in
        /// newer Tekla.Macros.Runtime-based macros). The surrounding namespace/class/method
        /// declarations that only exist to host the 'Run' entry point are not part of the result.
        /// </summary>
        private static (List<string> Usings, string Body, string ExtraCode) ExtractMacroRunBody(string filePath)
        {
            string content = File.ReadAllText(filePath);

            List<string> usings = Regex.Matches(content, @"(?m)^\s*using\s+[^;]+;")
                .Select(m => m.Value.Trim())
                .Distinct()
                .ToList();

            int runIndex = content.IndexOf("void Run(", StringComparison.Ordinal);
            if (runIndex < 0)
            {
                throw new InvalidOperationException("Could not find a 'Run' method in the selected file.");
            }

            int parenClose = content.IndexOf(')', runIndex);
            if (parenClose < 0)
            {
                throw new InvalidOperationException("Malformed 'Run' method signature.");
            }

            int braceOpen = content.IndexOf('{', parenClose);
            if (braceOpen < 0)
            {
                throw new InvalidOperationException("Could not find the body of the 'Run' method.");
            }

            int braceClose = FindMatchingBrace(content, braceOpen);
            if (braceClose < 0)
            {
                throw new InvalidOperationException("Could not find the end of the 'Run' method body.");
            }

            string rawBody = content[(braceOpen + 1)..braceClose];
            string body = NormalizeIndent(rawBody);
            if (string.IsNullOrWhiteSpace(body))
            {
                throw new InvalidOperationException("The 'Run' method body is empty.");
            }

            string extraCode = ExtractExtraTypes(content, runIndex);

            return (usings, body, extraCode);
        }

        /// <summary>
        /// Finds and returns the source of every top-level class/struct declared in the macro file,
        /// except the wrapper class that contains the macro's 'Run' entry point. These extra types
        /// (e.g. helper classes such as 'SwapHandles' in the Tekla sample macros) are copied verbatim
        /// into the generated Open API logic file.
        /// </summary>
        private static string ExtractExtraTypes(string content, int runIndex)
        {
            var extraTypes = new List<string>();
            foreach (Match match in Regex.Matches(content, @"(?m)^\s*(?:public|internal|private|protected)?\s*(?:static\s+|sealed\s+|abstract\s+|partial\s+)*(class|struct)\s+(\w+)"))
            {
                int braceOpen = content.IndexOf('{', match.Index);
                if (braceOpen < 0)
                {
                    continue;
                }

                int braceClose = FindMatchingBrace(content, braceOpen);
                if (braceClose < 0)
                {
                    continue;
                }

                // Skip the wrapper class/type that contains the 'Run' entry point itself; its body
                // was already extracted separately as the ExecuteTextCode content.
                if (runIndex >= braceOpen && runIndex <= braceClose)
                {
                    continue;
                }

                string declaration = content[match.Index..(braceClose + 1)].TrimEnd();
                extraTypes.Add(declaration);
            }

            return string.Join("\r\n\r\n", extraTypes);
        }

        private static int FindMatchingBrace(string content, int braceOpen)
        {
            int depth = 0;
            for (int i = braceOpen; i < content.Length; i++)
            {
                if (content[i] == '{')
                {
                    depth++;
                }
                else if (content[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private static string NormalizeIndent(string rawBody)
        {
            List<string> lines = rawBody.Replace("\r\n", "\n").Split('\n').ToList();
            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
            {
                lines.RemoveAt(0);
            }
            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
            {
                lines.RemoveAt(lines.Count - 1);
            }

            if (lines.Count == 0)
            {
                return string.Empty;
            }

            int minIndent = lines
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Length - l.TrimStart(' ', '\t').Length)
                .DefaultIfEmpty(0)
                .Min();

            return string.Join("\r\n", lines.Select(l => l.Length >= minIndent ? l[minIndent..] : l.TrimStart()));
        }

        private static string IndentCode(string code, int spaces)
        {
            string indent = new(' ', spaces);
            return string.Join("\r\n", code.Replace("\r\n", "\n").Split('\n')
                .Select(l => string.IsNullOrWhiteSpace(l) ? string.Empty : indent + l));
        }

        private string GetDefaultActionBody(string appType)
        {
            const string beamSetup =
                "var beam = new Beam();\r\n" +
                "beam.Name = \"BEAM\";\r\n" +
                "beam.Profile.ProfileString = \"HEA300\";\r\n" +
                "beam.Material.MaterialString = \"S235JR\";\r\n" +
                "beam.Class = \"1\";\r\n" +
                "beam.StartPoint = new Tekla.Structures.Geometry3d.Point(0, 0, 0);\r\n" +
                "beam.EndPoint = new Tekla.Structures.Geometry3d.Point(6000, 0, 0);";

            return appType switch
            {
                "Console" => beamSetup + "\r\nbeam.Insert();\r\n\r\nmodel.CommitChanges();\r\nConsole.WriteLine(\"Beam inserted successfully.\");",
                "WinForms" => beamSetup + "\r\n\r\nif (beam.Insert())\r\n{\r\n    _model.CommitChanges();\r\n    lblStatus.ForeColor = System.Drawing.Color.Green;\r\n    lblStatus.Text = \"Beam inserted successfully.\";\r\n}\r\nelse\r\n{\r\n    lblStatus.ForeColor = System.Drawing.Color.Red;\r\n    lblStatus.Text = \"Failed to insert beam.\";\r\n}",
                _ => beamSetup + "\r\n\r\nif (beam.Insert())\r\n{\r\n    _model.CommitChanges();\r\n    txtStatus.Foreground = Brushes.Green;\r\n    txtStatus.Text = \"Beam inserted successfully.\";\r\n}\r\nelse\r\n{\r\n    txtStatus.Foreground = Brushes.Red;\r\n    txtStatus.Text = \"Failed to insert beam.\";\r\n}"
            };
        }

        private string GetActionBody(string appType)
        {
            if (!HasCustomMacro)
            {
                return GetDefaultActionBody(appType);
            }

            string successAssignment = appType switch
            {
                "Console" => "Console.WriteLine(\"Code executed successfully.\");",
                "WinForms" => "lblStatus.ForeColor = System.Drawing.Color.Green;\r\nlblStatus.Text = \"Code executed successfully.\";",
                _ => "txtStatus.Foreground = Brushes.Green;\r\ntxtStatus.Text = \"Code executed successfully.\";"
            };

            return "OpenApiCode.ExecuteTextCode();\r\n\r\n" + successAssignment;
        }

        /// <summary>
        /// Builds the content of the separate "OpenApiCode.cs" file that hosts the Tekla Open API
        /// logic copied from the selected macro's 'Run' method (exposed as 'ExecuteTextCode'), plus
        /// any additional helper classes/methods declared in the macro file.
        /// </summary>
        public string GenerateOpenApiCodeFile(string safeNamespace)
        {
            List<string> baseUsings = ["using System;", "using Tekla.Structures.Model;"];
            List<string> allUsings = baseUsings
                .Concat(MacroUsings)
                .Distinct()
                .ToList();
            string usings = string.Join("\r\n", allUsings);

            string extraTypesBlock = string.IsNullOrWhiteSpace(MacroExtraCode)
                ? string.Empty
                : "\r\n\r\n" + IndentCode(MacroExtraCode, 4);

            return $@"{usings}
namespace {safeNamespace}
{{
    public static class OpenApiCode
    {{
        public static void ExecuteTextCode()
        {{
            {IndentCode(MacroBody ?? string.Empty, 12).TrimStart()}
        }}
    }}{extraTypesBlock}
}}
";
        }

        public bool ValidateInputs(string projectName, string teklaVersionFolder, bool bootstrapFailed, string bootstrapError, out string error)
        {
            error = string.Empty;

            if (bootstrapFailed)
            {
                error = bootstrapError;
                return false;
            }

            if (string.IsNullOrEmpty(projectName))
            {
                error = "Please enter a project name.";
                return false;
            }

            string reposBase = GetReposBasePath(teklaVersionFolder);

            if (!Directory.Exists(reposBase))
            {
                error = $"The folder '{reposBase}' does not exist.";
                return false;
            }

            // Directory.Build.Props is required for all versions except 2023
            if (teklaVersionFolder != "2023")
            {
                string directoryBuildProps = Path.Combine(reposBase, "Directory.Build.Props");
                if (!File.Exists(directoryBuildProps))
                {
                    error = $"Directory.Build.Props not found in '{reposBase}'.";
                    return false;
                }
            }

            string projectPath = GetProjectPath(teklaVersionFolder, projectName);
            if (Directory.Exists(projectPath))
            {
                error = $"Project folder already exists: '{projectPath}'.";
                return false;
            }

            return true;
        }

        public static string GetNuGetPackageVersion(string teklaVersion)
        {
            return teklaVersion switch
            {
                "2023" => "2023.0.1",
                "2024" => "2024.0.4",
                "2025" => "2025.0.0",
                "2026" => "2026.0.3",
                "2027 dailybuild" => "2027.0.0-daily",
                _ => "2026.0.3"
            };
        }

        public string GenerateCsproj(string appType, string teklaVersion)
        {
            string outputType = appType == "Console" ? "Exe" : "WinExe";
            string frameworkProps = appType switch
            {
                "WinForms" => "\n    <UseWindowsForms>true</UseWindowsForms>",
                "WPF" => "\n    <UseWPF>true</UseWPF>\n    <ImportWindowsDesktopTargets>true</ImportWindowsDesktopTargets>",
                _ => string.Empty
            };

            if (teklaVersion == "2027 dailybuild")
            {
                string binPath = @"C:\Program Files\Tekla Structures\2027.0 Daily\bin";
                return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>{outputType}</OutputType>
    <TargetFramework>net48</TargetFramework>{frameworkProps}
    <PlatformTarget>x64</PlatformTarget>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""TSAppConfigPatcherTask"" Version=""2.1.3"" />
  </ItemGroup>

  <ItemGroup>
    <Reference Include=""Tekla.Structures"">
      <HintPath>{binPath}\Tekla.Structures.dll</HintPath>
    </Reference>
    <Reference Include=""Tekla.Structures.Model"">
      <HintPath>{binPath}\Tekla.Structures.Model.dll</HintPath>
    </Reference>
    <Reference Include=""Tekla.Structures.Catalogs"">
      <HintPath>{binPath}\Tekla.Structures.Catalogs.dll</HintPath>
    </Reference>
    <Reference Include=""Tekla.Structures.Datatype"">
      <HintPath>{binPath}\Tekla.Structures.Datatype.dll</HintPath>
    </Reference>
    <Reference Include=""Tekla.Structures.Dialog"">
      <HintPath>{binPath}\Tekla.Structures.Dialog.dll</HintPath>
    </Reference>
    <Reference Include=""Tekla.Structures.Drawing"">
      <HintPath>{binPath}\Tekla.Structures.Drawing.dll</HintPath>
    </Reference>
  </ItemGroup>

</Project>
";
            }

            string packageVersion = GetNuGetPackageVersion(teklaVersion);

            string patcherPackage = teklaVersion != "2023"
                ? "\n    <PackageReference Include=\"TSAppConfigPatcherTask\" Version=\"2.1.3\" />"
                : string.Empty;

            return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>{outputType}</OutputType>
    <TargetFramework>net48</TargetFramework>{frameworkProps}
    <PlatformTarget>x64</PlatformTarget>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Tekla.Structures"" Version=""{packageVersion}"" />
    <PackageReference Include=""Tekla.Structures.Model"" Version=""{packageVersion}"" />
    <PackageReference Include=""Tekla.Structures.Catalogs"" Version=""{packageVersion}"" />
    <PackageReference Include=""Tekla.Structures.Dialog"" Version=""{packageVersion}"" />
    <PackageReference Include=""Tekla.Structures.Drawing"" Version=""{packageVersion}"" />{patcherPackage}
  </ItemGroup>

</Project>
";
        }

        public string GenerateProgramCs(string appType, string projectName)
        {
            string safeNamespace = projectName.Replace(" ", "_").Replace("-", "_");

            if (appType == "Console")
            {
                return $@"using System;
using Tekla.Structures.Model;
using Tekla.Structures.Geometry3d;

namespace {safeNamespace}
{{
    class Program
    {{
        static void Main(string[] args)
        {{
            var model = new Model();
            if (!model.GetConnectionStatus())
            {{
                Console.WriteLine(""Cannot connect to Tekla Structures."");
                return;
            }}

            Console.WriteLine(""Model name: "" + model.GetInfo().ModelName);

            {IndentCode(GetActionBody("Console"), 12).TrimStart()}
        }}
    }}
}}
";
            }
            else if (appType == "WinForms")
            {
                return $@"using System;
using System.Windows.Forms;

namespace {safeNamespace}
{{
    static class Program
    {{
        [STAThread]
        static void Main()
        {{
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new {safeNamespace}Dialog());
        }}
    }}
}}
";
            }
            else // WPF
            {
                return string.Empty; // WPF uses App.xaml as entry point
            }
        }

        public void GenerateWinFormsFiles(string projectPath, string safeNamespace, string projectName)
        {
            string className = safeNamespace + "Dialog";
            string form1Cs = $@"using System;
using System.Windows.Forms;
using Tekla.Structures.Model;

namespace {safeNamespace}
{{
    public partial class {className} : Form
    {{
        private readonly Model _model = new Model();

        public {className}()
        {{
            InitializeComponent();
            if (_model.GetConnectionStatus())
            {{
                lblModelName.Text = ""Model: "" + _model.GetInfo().ModelName;
            }}
            else
            {{
                lblModelName.Text = ""Not connected to Tekla Structures"";
                btnInsertBeam.Enabled = false;
            }}
        }}

        private void btnInsertBeam_Click(object sender, EventArgs e)
        {{
            try
            {{
                {IndentCode(GetActionBody("WinForms"), 16).TrimStart()}
            }}
            catch (Exception ex)
            {{
                lblStatus.ForeColor = System.Drawing.Color.Red;
                lblStatus.Text = ""Error: "" + ex.Message;
            }}
        }}
    }}
}}
";
            string form1DesignerCs = $@"namespace {safeNamespace}
{{
    partial class {className}
    {{
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblModelName;
        private System.Windows.Forms.Button btnInsertBeam;
        private System.Windows.Forms.Label lblStatus;

        protected override void Dispose(bool disposing)
        {{
            if (disposing && (components != null))
            {{
                components.Dispose();
            }}
            base.Dispose(disposing);
        }}

        private void InitializeComponent()
        {{
            this.lblModelName = new System.Windows.Forms.Label();
            this.btnInsertBeam = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            this.lblModelName.AutoSize = true;
            this.lblModelName.Location = new System.Drawing.Point(12, 15);
            this.lblModelName.Name = ""lblModelName"";
            this.lblModelName.Text = ""Connecting..."";
            this.btnInsertBeam.Location = new System.Drawing.Point(12, 40);
            this.btnInsertBeam.Name = ""btnInsertBeam"";
            this.btnInsertBeam.Size = new System.Drawing.Size(150, 30);
            this.btnInsertBeam.Text = ""{(HasCustomMacro ? "Run Code" : "Insert Beam")}"";
            this.btnInsertBeam.UseVisualStyleBackColor = true;
            this.btnInsertBeam.Click += new System.EventHandler(this.btnInsertBeam_Click);
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 80);
            this.lblStatus.Name = ""lblStatus"";
            this.lblStatus.Text = """";
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(450, 110);
            this.Controls.Add(this.lblModelName);
            this.Controls.Add(this.btnInsertBeam);
            this.Controls.Add(this.lblStatus);
            this.Name = ""{className}"";
            this.Text = ""{projectName}"";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();
        }}
    }}
}}
";
            File.WriteAllText(Path.Combine(projectPath, $"{className}.cs"), form1Cs);
            File.WriteAllText(Path.Combine(projectPath, $"{className}.Designer.cs"), form1DesignerCs);
        }

        public void GenerateWpfFiles(string projectPath, string safeNamespace, string projectName)
        {
            string className = safeNamespace + "Window";
            string appXaml = $@"<Application x:Class=""{safeNamespace}.App""
             xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
             xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
             StartupUri=""{className}.xaml"">
</Application>
";
            string appXamlCs = $@"using System.Windows;

namespace {safeNamespace}
{{
    public partial class App : Application
    {{
    }}
}}
";
            string mainWindowXaml = $@"<Window x:Class=""{safeNamespace}.{className}""
        xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
        Title=""{projectName}"" Height=""150"" Width=""450"" Topmost=""True"">
    <StackPanel Margin=""10"">
        <TextBlock x:Name=""txtModelName"" Text=""Connecting..."" Margin=""0,0,0,8""/>
        <Button Content=""{(HasCustomMacro ? "Run Code" : "Insert Beam")}"" HorizontalAlignment=""Left""
                Padding=""20,6"" Click=""BtnInsertBeam_Click"" x:Name=""btnInsertBeam""/>
        <TextBlock x:Name=""txtStatus"" Margin=""0,8,0,0""/>
    </StackPanel>
</Window>
";
            string mainWindowXamlCs = $@"using System;
using System.Windows;
using System.Windows.Media;
using Tekla.Structures.Model;

namespace {safeNamespace}
{{
    public partial class {className} : Window
    {{
        private readonly Model _model = new Model();

        public {className}()
        {{
            InitializeComponent();
            if (_model.GetConnectionStatus())
            {{
                txtModelName.Text = ""Model: "" + _model.GetInfo().ModelName;
            }}
            else
            {{
                txtModelName.Text = ""Not connected to Tekla Structures"";
                btnInsertBeam.IsEnabled = false;
            }}
        }}

        private void BtnInsertBeam_Click(object sender, RoutedEventArgs e)
        {{
            try
            {{
                {IndentCode(GetActionBody("WPF"), 16).TrimStart()}
            }}
            catch (Exception ex)
            {{
                txtStatus.Foreground = Brushes.Red;
                txtStatus.Text = ""Error: "" + ex.Message;
            }}
        }}
    }}
}}
";
            File.WriteAllText(Path.Combine(projectPath, "App.xaml"), appXaml);
            File.WriteAllText(Path.Combine(projectPath, "App.xaml.cs"), appXamlCs);
            File.WriteAllText(Path.Combine(projectPath, $"{className}.xaml"), mainWindowXaml);
            File.WriteAllText(Path.Combine(projectPath, $"{className}.xaml.cs"), mainWindowXamlCs);
        }

        /// <summary>
        /// Creates the full project (csproj + entry-point files + OpenApiCode.cs if a macro was
        /// loaded) at the standard repos path for the given Tekla version. Does not open the project
        /// in an editor; callers are responsible for that (interactive UI or CLI tool).
        /// </summary>
        public bool TryCreateProject(string projectName, string appType, string teklaVersionFolder, bool bootstrapFailed, string bootstrapError, out string projectPath, out string message)
        {
            projectName = projectName.Trim();
            projectPath = GetProjectPath(teklaVersionFolder, projectName);

            if (!ValidateInputs(projectName, teklaVersionFolder, bootstrapFailed, bootstrapError, out string error))
            {
                message = error;
                return false;
            }

            string safeNamespace = projectName.Replace(" ", "_").Replace("-", "_");

            try
            {
                Directory.CreateDirectory(projectPath);

                string csprojContent = GenerateCsproj(appType, teklaVersionFolder);
                File.WriteAllText(Path.Combine(projectPath, $"{projectName}.csproj"), csprojContent);

                if (appType == "WPF")
                {
                    GenerateWpfFiles(projectPath, safeNamespace, projectName);
                }
                else
                {
                    string programCs = GenerateProgramCs(appType, projectName);
                    File.WriteAllText(Path.Combine(projectPath, "Program.cs"), programCs);

                    if (appType == "WinForms")
                    {
                        GenerateWinFormsFiles(projectPath, safeNamespace, projectName);
                    }
                }

                if (HasCustomMacro)
                {
                    string openApiCodeCs = GenerateOpenApiCodeFile(safeNamespace);
                    File.WriteAllText(Path.Combine(projectPath, "OpenApiCode.cs"), openApiCodeCs);
                }

                message = $"Project created successfully at: {projectPath}";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Failed to create project: {ex.Message}";
                return false;
            }
        }
    }
}
