---
name: simple-tekla-extension-creator
description: Creates a new simple Tekla extension project from command line inputs, wrapping the Simple Tekla Extension Creator app in non-interactive mode so an agent can scaffold projects (choosing Tekla version and UI technology) without opening the UI.
---

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
- `CreatorProjectPath` (optional): Path to `Simple Tekla Extension Creator.csproj`. If omitted, falls back to the `SIMPLE_TEKLA_CREATOR_PATH` environment variable (can point to the `.csproj` file or its containing folder), then to the repo this script lives in.

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

From another solution/repo, without passing `-CreatorProjectPath` every time, set the environment variable once (user-level, so it's available in any shell/agent session):

```powershell
[Environment]::SetEnvironmentVariable("SIMPLE_TEKLA_CREATOR_PATH", "C:\git\CarlosHerrero\Simple-Tekla-Extension-Creator\Simple Tekla Extension Creator", "User")
```

Then call the script (e.g. via a copy on `PATH`, or its full path) from any repo:

```powershell
Create-SimpleTeklaExtension.ps1 -ProjectName MyTeklaTool -Version 2026 -UI WPF
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
- `CreatorProjectPath` resolution order: explicit parameter, then `SIMPLE_TEKLA_CREATOR_PATH` environment variable, then the script's own repo location.
