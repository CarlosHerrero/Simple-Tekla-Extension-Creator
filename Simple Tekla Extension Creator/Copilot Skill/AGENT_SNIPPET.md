<!--
  Portable AGENTS.md / .github/copilot-instructions.md snippet for the
  Simple Tekla Extension Creator skill.

  How to use this file:
  1. Copy the "Copilot Skill" folder (this file's parent) into your own repo,
	 anywhere you like (e.g. `tools/simple-tekla-extension-creator-skill/`).
  2. Do NOT overwrite your repo's existing AGENTS.md or
	 .github/copilot-instructions.md. Instead, open those files (creating them
	 if they don't exist yet) and APPEND the section below, verbatim.
  3. Update the two relative links below so they point at wherever you placed
	 the "Copilot Skill" folder in step 1.
-->

## Simple Tekla Extension Creator skill

This repo includes an agent skill that scaffolds a new Tekla Structures extension project
(Console, WinForms, or WPF, targeting Tekla Structures 2023-2027) without needing to open
the app's UI.

- Skill description and usage: [<path-to-skill-folder>/SKILL.md](<path-to-skill-folder>/SKILL.md)
- Script to invoke: `<path-to-skill-folder>/scripts/Create-SimpleTeklaExtension.ps1`

Use this skill whenever the user asks to create/scaffold a new Tekla extension, sample, or
starter project. Read `SKILL.md` first for the full list of parameters (`ProjectName`,
`Version`, `UI`, `OpenProject`, `MacroFile`, `CreatorProjectPath`) and expected behavior before
invoking the script.

By default the script opens the generated project in Visual Studio, Visual Studio Code, or
Cursor (whichever is found first). Pass `-OpenProject:$false` to skip opening it.

The generated project always checks the Tekla model name. By default it also inserts an
example beam, but you can replace that example with real Tekla Open API code by passing
`-MacroFile <path-to-macro.cs>` pointing to an existing Tekla macro file (e.g. from
`C:\ProgramData\Trimble\Tekla Structures\<version>.0\Environments\common\macros`). The code
inside the macro's entry-point method (and any helper classes/methods it declares) is copied
into a separate `OpenApiCode.cs` file in the generated project, exposed via a static
`OpenApiCode.ExecuteTextCode()` method that the project calls instead of the beam example.

### Installation
The generator is distributed as a packable dotnet tool. Install it once, globally:

```powershell
dotnet tool install --global SimpleTeklaExtensionCreator
```

The script prefers the installed `simple-tekla-extension-creator` command and falls back to
`dotnet run --project` against `Simple Tekla Extension Creator.Tool.csproj` for local
development (resolved via `-CreatorProjectPath`, then the `SIMPLE_TEKLA_CREATOR_PATH`
environment variable, then the script's own repo location).

### Prerequisites to run the skill
- Windows (paths and Tekla Structures installation locations are Windows-specific)
- .NET 10 SDK
- PowerShell 7+ (`pwsh`)
- The `SimpleTeklaExtensionCreator` dotnet tool installed globally (or the repo source, for
  local development via `dotnet run --project`)

<!-- End of snippet -->
