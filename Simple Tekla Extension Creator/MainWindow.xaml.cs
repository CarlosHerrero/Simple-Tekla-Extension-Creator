using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Simple_Tekla_Extension_Creator
{
    public partial class MainWindow : Window
    {
        private bool _bootstrapFailed;
        private string _bootstrapError = string.Empty;

        public MainWindow(string? startupTeklaVersion = null, string? startupUi = null, bool interactive = true)
        {
            InitializeComponent();

            if (!EnsureAllBaseFoldersAndProps(out string bootstrapError))
            {
                _bootstrapFailed = true;
                _bootstrapError = bootstrapError;

                if (interactive)
                {
                    MessageBox.Show(bootstrapError, "Insufficient Permissions", MessageBoxButton.OK, MessageBoxImage.Warning);
                    DisableForBootstrapFailure(bootstrapError);
                }
            }

            ApplyStartupSelections(startupTeklaVersion, startupUi);
            UpdatePath();
        }

        private void DisableForBootstrapFailure(string error)
        {
            rootGrid.IsEnabled = false;
            txtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
            txtStatus.Text = error;
        }

        /// <summary>
        /// Every supported Tekla version, mapped to its base repos folder name.
        /// </summary>
        private static readonly (string Version, string Folder)[] AllTeklaVersionFolders =
        [
            ("2023", "2023"),
            ("2024", "2024"),
            ("2025", "2025"),
            ("2026", "2026"),
            ("2027 dailybuild", "2027 Daily"),
        ];

        /// <summary>
        /// Ensures that the base repos folder for every supported Tekla version exists, and that each
        /// (except 2023, which does not require one) has a Directory.Build.Props file.
        /// </summary>
        private static bool EnsureAllBaseFoldersAndProps(out string errorMessage)
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

        private static string GetDirectoryBuildPropsTemplate(string teklaVersion)
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

        private void ApplyStartupSelections(string? startupTeklaVersion, string? startupUi)
        {
            if (!string.IsNullOrWhiteSpace(startupUi))
            {
                TrySelectComboItem(cmbAppType, startupUi);
            }

            if (!string.IsNullOrWhiteSpace(startupTeklaVersion))
            {
                string normalizedVersion = NormalizeTeklaVersion(startupTeklaVersion);
                TrySelectComboItem(cmbTeklaVersion, normalizedVersion);
            }
        }

        private static string NormalizeTeklaVersion(string version)
        {
            return version.Trim().Equals("2027", StringComparison.OrdinalIgnoreCase)
                ? "2027 dailybuild"
                : version.Trim();
        }

        private static bool TrySelectComboItem(ComboBox comboBox, string value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                string? content = item.Content?.ToString();
                if (string.Equals(content, value, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return true;
                }
            }

            return false;
        }

        private string GetSelectedText(ComboBox comboBox)
        {
            return (comboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
        }

        private string GetTeklaVersionFolder()
        {
            return GetSelectedText(cmbTeklaVersion);
        }

        private string GetReposBasePath()
        {
            //string computerName = Environment.MachineName;
            string userName = Environment.UserName;
            string teklaVersion = GetTeklaVersionFolder();
            string folder = teklaVersion == "2027 dailybuild" ? "2027 Daily" : teklaVersion;
            return Path.Combine($@"C:\Users\{userName}\source\repos", folder);
        }

        private string GetProjectPath()
        {
            string projectName = txtProjectName.Text.Trim();
            if (string.IsNullOrEmpty(projectName))
                return string.Empty;
            return Path.Combine(GetReposBasePath(), projectName);
        }

        private void UpdatePath()
        {
            string projectName = txtProjectName.Text.Trim();
            if (string.IsNullOrEmpty(projectName))
            {
                txtPath.Text = GetReposBasePath() + @"\<ProjectName>";
            }
            else
            {
                txtPath.Text = GetProjectPath();
            }

            ValidateInputs(out _);
        }

        private bool ValidateInputs(out string error)
        {
            error = string.Empty;

            if (_bootstrapFailed)
            {
                error = _bootstrapError;
                txtStatus.Text = error;
                btnCreate.IsEnabled = false;
                return false;
            }

            string projectName = txtProjectName.Text.Trim();

            if (string.IsNullOrEmpty(projectName))
            {
                error = "Please enter a project name.";
                txtStatus.Text = error;
                btnCreate.IsEnabled = false;
                return false;
            }

            string reposBase = GetReposBasePath();
            string teklaVersion = GetTeklaVersionFolder();

            if (!Directory.Exists(reposBase))
            {
                error = $"The folder '{reposBase}' does not exist.";
                txtStatus.Text = error;
                btnCreate.IsEnabled = false;
                return false;
            }

            // Directory.Build.Props is required for all versions except 2023
            if (teklaVersion != "2023")
            {
                string directoryBuildProps = Path.Combine(reposBase, "Directory.Build.Props");
                if (!File.Exists(directoryBuildProps))
                {
                    error = $"Directory.Build.Props not found in '{reposBase}'.";
                    txtStatus.Text = error;
                    btnCreate.IsEnabled = false;
                    return false;
                }
            }

            string projectPath = GetProjectPath();
            if (Directory.Exists(projectPath))
            {
                error = $"Project folder already exists: '{projectPath}'.";
                txtStatus.Text = error;
                btnCreate.IsEnabled = false;
                return false;
            }

            txtStatus.Text = string.Empty;
            btnCreate.IsEnabled = true;
            return true;
        }

        private void OnInputChanged(object sender, EventArgs e)
        {
            if (txtPath == null || txtStatus == null || btnCreate == null)
                return;
            UpdatePath();
        }

        private string GetNuGetPackageVersion(string teklaVersion)
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

        private string GenerateCsproj(string appType, string teklaVersion)
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

        private string GenerateProgramCs(string appType, string projectName)
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

            var beam = new Beam();
            beam.Name = ""BEAM"";
            beam.Profile.ProfileString = ""HEA300"";
            beam.Material.MaterialString = ""S235JR"";
            beam.Class = ""1"";
            beam.StartPoint = new Tekla.Structures.Geometry3d.Point(0, 0, 0);
            beam.EndPoint = new Tekla.Structures.Geometry3d.Point(6000, 0, 0);
            beam.Insert();

            model.CommitChanges();
            Console.WriteLine(""Beam inserted successfully."");
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

        private void GenerateWinFormsFiles(string projectPath, string safeNamespace, string projectName)
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
                var beam = new Beam();
                beam.Name = ""BEAM"";
                beam.Profile.ProfileString = ""HEA300"";
                beam.Material.MaterialString = ""S235JR"";
                beam.Class = ""1"";
                beam.StartPoint = new Tekla.Structures.Geometry3d.Point(0, 0, 0);
                beam.EndPoint = new Tekla.Structures.Geometry3d.Point(6000, 0, 0);

                if (beam.Insert())
                {{
                    _model.CommitChanges();
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                    lblStatus.Text = ""Beam inserted successfully."";
                }}
                else
                {{
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                    lblStatus.Text = ""Failed to insert beam."";
                }}
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
            this.btnInsertBeam.Text = ""Insert Beam"";
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

        private void GenerateWpfFiles(string projectPath, string safeNamespace, string projectName)
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
        <Button Content=""Insert Beam"" HorizontalAlignment=""Left""
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
                var beam = new Beam();
                beam.Name = ""BEAM"";
                beam.Profile.ProfileString = ""HEA300"";
                beam.Material.MaterialString = ""S235JR"";
                beam.Class = ""1"";
                beam.StartPoint = new Tekla.Structures.Geometry3d.Point(0, 0, 0);
                beam.EndPoint = new Tekla.Structures.Geometry3d.Point(6000, 0, 0);

                if (beam.Insert())
                {{
                    _model.CommitChanges();
                    txtStatus.Foreground = Brushes.Green;
                    txtStatus.Text = ""Beam inserted successfully."";
                }}
                else
                {{
                    txtStatus.Foreground = Brushes.Red;
                    txtStatus.Text = ""Failed to insert beam."";
                }}
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

        public bool TryCreateProjectNonInteractive(string projectName, string appType, string teklaVersion, bool openProject, out string message)
        {
            message = string.Empty;

            if (string.IsNullOrWhiteSpace(projectName))
            {
                message = "Please provide a project name with --project.";
                return false;
            }

            txtProjectName.Text = projectName.Trim();

            if (!TrySelectComboItem(cmbAppType, appType))
            {
                message = $"Unsupported UI '{appType}'. Allowed values: Console, WinForms, WPF.";
                return false;
            }

            string normalizedVersion = NormalizeTeklaVersion(teklaVersion);
            if (!TrySelectComboItem(cmbTeklaVersion, normalizedVersion))
            {
                message = $"Unsupported version '{teklaVersion}'. Allowed values: 2023, 2024, 2025, 2026, 2027.";
                return false;
            }

            UpdatePath();
            return TryCreateProject(openProjectInVisualStudio: openProject, showMessageBoxes: false, out message);
        }

        private bool TryCreateProject(bool openProjectInVisualStudio, bool showMessageBoxes, out string message)
        {
            if (!ValidateInputs(out string error))
            {
                message = error;
                if (showMessageBoxes)
                {
                    MessageBox.Show(error, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                return false;
            }

            string projectName = txtProjectName.Text.Trim();
            string appType = GetSelectedText(cmbAppType);
            string teklaVersion = GetTeklaVersionFolder();
            string projectPath = GetProjectPath();
            string safeNamespace = projectName.Replace(" ", "_").Replace("-", "_");

            try
            {
                Directory.CreateDirectory(projectPath);

                string csprojContent = GenerateCsproj(appType, teklaVersion);
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

                string successMessage = $"Project created successfully at: {projectPath}";
                txtStatus.Foreground = System.Windows.Media.Brushes.Green;
                txtStatus.Text = successMessage;

                if (openProjectInVisualStudio)
                {
                    string csprojFile = Path.Combine(projectPath, $"{projectName}.csproj");
                    OpenCreatedProject(projectPath, csprojFile);
                }

                message = successMessage;
                return true;
            }
            catch (Exception ex)
            {
                txtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
                txtStatus.Text = $"Error: {ex.Message}";
                message = $"Failed to create project: {ex.Message}";

                if (showMessageBoxes)
                {
                    MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                return false;
            }
        }

        private static void OpenCreatedProject(string projectPath, string csprojFile)
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string[] devenvCandidates =
            [
                Path.Combine(programFiles, @"Microsoft Visual Studio\Insiders\Common7\IDE\devenv.exe"),
                Path.Combine(programFiles, @"Microsoft Visual Studio\2022\Preview\Common7\IDE\devenv.exe"),
            ];

            string? devenvPath = devenvCandidates.FirstOrDefault(File.Exists);
            if (devenvPath != null)
            {
                Process.Start(devenvPath, $"\"{csprojFile}\"");
                return;
            }

            string[] vsCodeCandidates =
            [
                Path.Combine(localAppData, @"Programs\Microsoft VS Code\Code.exe"),
                Path.Combine(programFiles, @"Microsoft VS Code\Code.exe"),
            ];

            string? vsCodePath = vsCodeCandidates.FirstOrDefault(File.Exists);
            if (vsCodePath != null)
            {
                Process.Start(vsCodePath, $"\"{projectPath}\"");
                return;
            }

            string[] cursorCandidates =
            [
                Path.Combine(localAppData, @"Programs\Cursor\Cursor.exe"),
                Path.Combine(programFiles, @"Cursor\Cursor.exe"),
            ];

            string? cursorPath = cursorCandidates.FirstOrDefault(File.Exists);
            if (cursorPath != null)
            {
                Process.Start(cursorPath, $"\"{projectPath}\"");
                return;
            }

            if (TryStartOnPath("code", $"\"{projectPath}\""))
            {
                return;
            }

            if (TryStartOnPath("cursor", $"\"{projectPath}\""))
            {
                return;
            }

            Process.Start(new ProcessStartInfo(csprojFile) { UseShellExecute = true });
        }

        private static bool TryStartOnPath(string fileName, string arguments)
        {
            try
            {
                Process.Start(new ProcessStartInfo(fileName, arguments) { UseShellExecute = true });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            TryCreateProject(openProjectInVisualStudio: true, showMessageBoxes: true, out _);
        }
    }
}