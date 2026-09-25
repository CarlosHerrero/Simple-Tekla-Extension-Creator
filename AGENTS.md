# Agent instructions for this repository

## Simple Tekla Extension Creator skill

This repo ships an agent skill that scaffolds a new Tekla Structures extension project
(Console, WinForms, or WPF, targeting Tekla Structures 2023-2027) without needing to open
the app's UI.

- Skill description and usage: [Simple Tekla Extension Creator/Copilot Skill/SKILL.md](Simple%20Tekla%20Extension%20Creator/Copilot%20Skill/SKILL.md)
- Script to invoke: `Simple Tekla Extension Creator/Copilot Skill/scripts/Create-SimpleTeklaExtension.ps1`

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

The script automatically prefers the installed `simple-tekla-extension-creator` command. If the
tool isn't installed (or `-CreatorProjectPath` is passed explicitly), it falls back to
`dotnet run --project` against `Simple Tekla Extension Creator.Tool.csproj`, resolving the path
from `-CreatorProjectPath`, then the `SIMPLE_TEKLA_CREATOR_PATH` environment variable, then the
script's own repo location.

### Prerequisites to run the skill
- Windows (paths and Tekla Structures installation locations are Windows-specific)
- .NET 10 SDK
- PowerShell 7+ (`pwsh`)
- The `SimpleTeklaExtensionCreator` dotnet tool installed globally (or the repo source, for
  local development via `dotnet run --project`)
