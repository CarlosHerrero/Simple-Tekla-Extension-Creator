# Simple-Tekla-Extension-Creator
Creates simple apps to test Tekla API - You can select app type (Console, WinForms, WPF) and TS version

## Agent skill / CLI scaffolding

You can also scaffold a new project from the command line (or let an AI coding agent do it
for you in Visual Studio, VS Code, or Cursor) without opening the app UI:

```powershell
./Simple Tekla Extension Creator/Copilot Skill/scripts/Create-SimpleTeklaExtension.ps1 -ProjectName MyTeklaTool -Version 2026 -UI WPF
```

See [Simple Tekla Extension Creator/Copilot Skill/SKILL.md](Simple%20Tekla%20Extension%20Creator/Copilot%20Skill/SKILL.md)
for all parameters and expected behavior. Requires the .NET 10 SDK and PowerShell 7+ on Windows.

To use the script from another solution/repo without passing `-CreatorProjectPath` every
time, set the `SIMPLE_TEKLA_CREATOR_PATH` environment variable (once, at the user level) to
this repo's `Simple Tekla Extension Creator` folder or its `.csproj` file.
