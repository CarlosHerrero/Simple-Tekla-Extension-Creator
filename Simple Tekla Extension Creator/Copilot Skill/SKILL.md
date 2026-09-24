# Simple Tekla Extension Creator Skill

## Purpose
This skill creates a new simple Tekla extension project from command line inputs.

It wraps the existing **Simple Tekla Extension Creator** app in non-interactive mode so an agent can scaffold projects without opening the UI.

## What this skill is for
Use this skill when you need to:
- Create a new Tekla extension starter project
- Choose target Tekla version (`2023`, `2024`, `2025`, `2026`, `2027`)
- Choose UI technology (`Console`, `WinForms`, `WPF`)

## Inputs
- `ProjectName` (required): Name of the new project/folder
- `Version` (optional): Tekla version. Default: `2026`
- `UI` (optional): App UI type. Default: `Console`
- `OpenProject` (optional switch): Open the created project after generation, using Visual Studio (Insiders/2022), Visual Studio Code, or Cursor, whichever is found first. Default: `true`. Pass `-OpenProject:$false` to skip opening.
- `CreatorProjectPath` (optional): Path to `Simple Tekla Extension Creator.csproj`

## Script
`scripts/Create-SimpleTeklaExtension.ps1`

## Usage
From the solution folder:

```powershell
./Copilot Skill/scripts/Create-SimpleTeklaExtension.ps1 -ProjectName MyTeklaTool -Version 2026 -UI WPF
```

With explicit creator project path:

```powershell
./Copilot Skill/scripts/Create-SimpleTeklaExtension.ps1 -ProjectName MyTeklaTool -Version 2025 -UI Console -CreatorProjectPath "C:\git\Simple-Tekla-Extension-Creator\Simple Tekla Extension Creator\Simple Tekla Extension Creator.csproj"
```

## Expected behavior
- Runs generator in headless mode:
  - `--non-interactive --project <name> --version <version> --ui <ui> [--open]`
- By default the created project is opened in Visual Studio, Visual Studio Code, or Cursor; pass `-OpenProject:$false` to skip opening it
- Prints success or error output
- Returns process exit code:
  - `0` on success
  - non-zero on failure

## Notes
- The generator validates destination paths and existing project folders.
- For Tekla versions except `2023`, required files such as `Directory.Build.Props` must exist in the expected repo base path.
