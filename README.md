# Simple-Tekla-Extension-Creator
Creates simple apps to test Tekla API - You can select app type (Console, WinForms, WPF) and TS version.
Generated projects always check the Tekla model name, and you can optionally replace the default
example beam-insertion code with real Tekla Open API code extracted from an existing macro (`.cs`) file.

## Agent skill / CLI scaffolding

The scaffolder is also available as a standalone dotnet tool, so you can use it without cloning
this repo:

```powershell
dotnet tool install --global SimpleTeklaExtensionCreator
simple-tekla-extension-creator --project MyTeklaTool --version 2026 --ui WPF
```

You can also scaffold a new project from the command line (or let an AI coding agent do it
for you in Visual Studio, VS Code, or Cursor) without opening the app UI:

```powershell
./Simple Tekla Extension Creator/Copilot Skill/scripts/Create-SimpleTeklaExtension.ps1 -ProjectName MyTeklaTool -Version 2026 -UI WPF
```

To replace the default beam-insertion example with Tekla Open API code from an existing macro file:

```powershell
./Simple Tekla Extension Creator/Copilot Skill/scripts/Create-SimpleTeklaExtension.ps1 -ProjectName MyTeklaTool -Version 2026 -UI WPF -MacroFile "C:\ProgramData\Trimble\Tekla Structures\2026.0\Environments\common\macros\modeling\Swap Handles.cs"
```

See [Simple Tekla Extension Creator/Copilot Skill/SKILL.md](Simple%20Tekla%20Extension%20Creator/Copilot%20Skill/SKILL.md)
for all parameters and expected behavior. Requires the .NET 10 SDK and PowerShell 7+ on Windows.

The PowerShell script automatically prefers the installed `simple-tekla-extension-creator` tool.
If you'd rather run from source (no tool installed), it falls back to `dotnet run --project`
against the `Simple Tekla Extension Creator.Tool` project; without passing `-CreatorProjectPath`
every time, set the `SIMPLE_TEKLA_CREATOR_PATH` environment variable (once, at the user level) to
this repo's `Simple Tekla Extension Creator` folder or the tool's `.csproj` file.
