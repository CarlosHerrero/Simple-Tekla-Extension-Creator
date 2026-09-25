using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Simple_Tekla_Extension_Creator.Core;

namespace Simple_Tekla_Extension_Creator
{
    public partial class MainWindow : Window
    {
        private readonly ProjectGenerator _generator = new();

        private bool _bootstrapFailed;
        private string _bootstrapError = string.Empty;

        private bool HasCustomMacro => _generator.HasCustomMacro;

        public MainWindow(string? startupTeklaVersion = null, string? startupUi = null, bool interactive = true)
        {
            InitializeComponent();

            if (!ProjectGenerator.EnsureAllBaseFoldersAndProps(out string bootstrapError))
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

        private void ApplyStartupSelections(string? startupTeklaVersion, string? startupUi)
        {
            if (!string.IsNullOrWhiteSpace(startupUi))
            {
                TrySelectComboItem(cmbAppType, startupUi);
            }

            if (!string.IsNullOrWhiteSpace(startupTeklaVersion))
            {
                string normalizedVersion = ProjectGenerator.NormalizeTeklaVersion(startupTeklaVersion);
                TrySelectComboItem(cmbTeklaVersion, normalizedVersion);
            }
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
            return ProjectGenerator.GetReposBasePath(GetTeklaVersionFolder());
        }

        private string GetProjectPath()
        {
            string projectName = txtProjectName.Text.Trim();
            if (string.IsNullOrEmpty(projectName))
                return string.Empty;
            return ProjectGenerator.GetProjectPath(GetTeklaVersionFolder(), projectName);
        }

        private string GetMacrosDefaultFolder()
        {
            return ProjectGenerator.GetMacrosDefaultFolder(GetTeklaVersionFolder());
        }

        private void BtnSelectMacro_Click(object sender, RoutedEventArgs e)
        {
            string defaultFolder = GetMacrosDefaultFolder();

            var dialog = new OpenFileDialog
            {
                Title = "Select a Tekla Open API macro file",
                Filter = "C# macro files (*.cs)|*.cs|All files (*.*)|*.*",
                InitialDirectory = Directory.Exists(defaultFolder)
                    ? defaultFolder
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            if (!_generator.TryLoadMacroFile(dialog.FileName, out string error))
            {
                MessageBox.Show(error, "Invalid Macro File", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            txtMacroPath.Text = Path.GetFileName(dialog.FileName);
            txtStatus.Foreground = System.Windows.Media.Brushes.Green;
            txtStatus.Text = $"Using custom Open API code from '{Path.GetFileName(dialog.FileName)}'.";
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
            string projectName = txtProjectName.Text.Trim();
            string teklaVersion = GetTeklaVersionFolder();

            bool isValid = _generator.ValidateInputs(projectName, teklaVersion, _bootstrapFailed, _bootstrapError, out error);
            txtStatus.Text = isValid ? string.Empty : error;
            btnCreate.IsEnabled = isValid;
            return isValid;
        }

        private void OnInputChanged(object sender, EventArgs e)
        {
            if (txtPath == null || txtStatus == null || btnCreate == null)
                return;
            UpdatePath();
        }

        public bool TryCreateProjectNonInteractive(string projectName, string appType, string teklaVersion, bool openProject, string? macroFilePath, out string message)
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

            string normalizedVersion = ProjectGenerator.NormalizeTeklaVersion(teklaVersion);
            if (!TrySelectComboItem(cmbTeklaVersion, normalizedVersion))
            {
                message = $"Unsupported version '{teklaVersion}'. Allowed values: 2023, 2024, 2025, 2026, 2027.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(macroFilePath))
            {
                if (!_generator.TryLoadMacroFile(macroFilePath, out string macroError))
                {
                    message = macroError;
                    return false;
                }

                txtMacroPath.Text = Path.GetFileName(macroFilePath);
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

            bool created = _generator.TryCreateProject(projectName, appType, teklaVersion, _bootstrapFailed, _bootstrapError, out string projectPath, out message);

            if (created)
            {
                txtStatus.Foreground = System.Windows.Media.Brushes.Green;
                txtStatus.Text = message;

                if (openProjectInVisualStudio)
                {
                    string csprojFile = Path.Combine(projectPath, $"{projectName}.csproj");
                    ProjectLauncher.OpenCreatedProject(projectPath, csprojFile);
                }
            }
            else
            {
                txtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
                txtStatus.Text = message;

                if (showMessageBoxes)
                {
                    MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return created;
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            TryCreateProject(openProjectInVisualStudio: true, showMessageBoxes: true, out _);
        }
    }
}
