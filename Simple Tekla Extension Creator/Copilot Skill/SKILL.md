---
name: simple-tekla-extension-creator
description: Creates a new simple Tekla extension project from command line inputs, wrapping the Simple Tekla Extension Creator app in non-interactive mode so an agent can scaffold projects (choosing Tekla version and UI technology) without opening the UI. Can optionally extract Tekla Open API code from an existing macro (.cs) file and use it as the project's action code instead of the default beam-insertion example.
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
- Replace the default beam-insertion example with Tekla Open API code extracted from an existing macro (`.cs`) file

## Inputs
- `ProjectName` (required): Name of the new project/folder
- `Version` (optional): Tekla version. Default: `2026`
- `UI` (optional): App UI type. Default: `Console`
- `OpenProject` (optional switch): Open the created project after generation, using Visual Studio (Insiders/2022), Visual Studio Code, or Cursor, whichever is found first. Default: `true`. Pass `-OpenProject:$false` to skip opening.
- `MacroFile` (optional): Path to a Tekla Open API macro `.cs` file (e.g. from `C:\ProgramData\Trimble\Tekla Structures\<version>.0\Environments\common\macros\modeling`). The code inside the macro's `Run` method, plus any additional helper classes/methods declared in the macro file, is extracted into a separate `OpenApiCode.cs` file in the generated project, exposed through a static `OpenApiCode.ExecuteTextCode()` method. The project's entry point calls `ExecuteTextCode()` instead of the default beam-insertion example. The Tekla model-name check is always kept. If omitted, the default beam-insertion example is used.
- `CreatorProjectPath` (optional): Path to `Simple Tekla Extension Creator.Tool.csproj`, used only as a fallback for local development when the `simple-tekla-extension-creator` dotnet tool is not installed. If omitted, falls back to the `SIMPLE_TEKLA_CREATOR_PATH` environment variable (can point to the `.csproj` file or its containing folder), then to the repo this script lives in.

## Script
`scripts/Create-SimpleTeklaExtension.ps1`

## Installation
This skill is backed by a packable dotnet tool. Install it once (globally) so the script can
invoke it without needing this repo's source code:

```powershell
dotnet tool install --global SimpleTeklaExtensionCreator
```

Update it later with:

```powershell
dotnet tool update --global SimpleTeklaExtensionCreator
```

If the tool is not installed, the script automatically falls back to `dotnet run --project`
against `Simple Tekla Extension Creator.Tool.csproj` (useful for local development).

## Usage
From the solution folder:

```powershell
./Copilot Skill/scripts/Create-SimpleTeklaExtension.ps1 -ProjectName MyTeklaTool -Version 2026 -UI WPF
```

With explicit creator project path:

```powershell
./Copilot Skill/scripts/Create-SimpleTeklaExtension.ps1 -ProjectName MyTeklaTool -Version 2025 -UI Console -CreatorProjectPath "C:\git\Simple-Tekla-Extension-Creator\Simple Tekla Extension Creator\Simple Tekla Extension Creator.csproj"
```

Using a Tekla Open API macro file to replace the default beam-insertion code:

```powershell
./Copilot Skill/scripts/Create-SimpleTeklaExtension.ps1 -ProjectName MyTeklaTool -Version 2026 -UI WPF -MacroFile "C:\ProgramData\Trimble\Tekla Structures\2026.0\Environments\common\macros\modeling\Swap Handles.cs"
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
- Runs the generator in headless mode, preferring the installed `simple-tekla-extension-creator` tool and falling back to `dotnet run --project` on the tool project:
  - `--project <name> --version <version> --ui <ui> [--open] [--macro <path>]`
- By default the created project is opened in Visual Studio, Visual Studio Code, or Cursor; pass `-OpenProject:$false` to skip opening it
- If `-MacroFile` is provided, the code inside the macro's `Run` method (and any extra helper classes/methods, and `using` directives) is written to a separate `OpenApiCode.cs` file exposing a static `OpenApiCode.ExecuteTextCode()` method, which the generated project calls instead of the default beam-insertion example. The Tekla model-name check is always kept
- Prints success or error output
- Returns process exit code:
  - `0` on success
  - non-zero on failure

## Notes
- The generator validates destination paths and existing project folders.
- For Tekla versions except `2023`, required files such as `Directory.Build.Props` must exist in the expected repo base path.
- Invocation order: the installed `simple-tekla-extension-creator` dotnet tool is used if found on `PATH` and no `-CreatorProjectPath` is given; otherwise the script falls back to `dotnet run --project`, resolving the project path from the explicit parameter, then `SIMPLE_TEKLA_CREATOR_PATH` environment variable, then the script's own repo location.

## Sharing this skill with another repo
To use this skill in a different repo/solution without cloning this one:

1. Install the dotnet tool globally (see [Installation](#installation) above).
2. Copy this whole `Copilot Skill/` folder into the other repo, anywhere you like
   (e.g. `tools/simple-tekla-extension-creator-skill/`).
3. Do **not** overwrite that repo's existing `AGENTS.md` or `.github/copilot-instructions.md`
   if it already has one. Instead, open (or create) those files and **append** the contents of
   [`AGENT_SNIPPET.md`](AGENT_SNIPPET.md), updating its two relative links to point at wherever
   you placed the folder in step 2.

This avoids clobbering any existing agent instructions the target repo may already have.
