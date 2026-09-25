using System.Diagnostics;

namespace Simple_Tekla_Extension_Creator.Core
{
    /// <summary>
    /// Opens a newly generated project in whichever supported editor is found first
    /// (Visual Studio Insiders/2022, Visual Studio Code, or Cursor). Shared by both the
    /// interactive WPF app and the headless CLI tool.
    /// </summary>
    public static class ProjectLauncher
    {
        public static void OpenCreatedProject(string projectPath, string csprojFile)
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
    }
}
