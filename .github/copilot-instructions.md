# Copilot instructions for this repository

## Simple Tekla Extension Creator skill

This repo ships an agent skill that scaffolds a new Tekla Structures extension project
(Console, WinForms, or WPF, targeting Tekla Structures 2023-2027) without needing to open
the app's UI.

- Skill description and usage: [Simple Tekla Extension Creator/Copilot Skill/SKILL.md](../Simple%20Tekla%20Extension%20Creator/Copilot%20Skill/SKILL.md)
- Script to invoke: `Simple Tekla Extension Creator/Copilot Skill/scripts/Create-SimpleTeklaExtension.ps1`

Use this skill whenever the user asks to create/scaffold a new Tekla extension, sample, or
starter project. Read `SKILL.md` first for the full list of parameters (`ProjectName`,
`Version`, `UI`, `OpenProject`, `MacroFile`, `CreatorProjectPath`) and expected behavior before
invoking the script.

The generated project always checks the Tekla model name. By default it also inserts an
example beam, but you can replace that example with real Tekla Open API code by passing
`-MacroFile <path-to-macro.cs>` pointing to an existing Tekla macro file (e.g. from
`C:\ProgramData\Trimble\Tekla Structures\<version>.0\Environments\common\macros`). The code
inside the macro's entry-point method (and any helper classes/methods it declares) is copied
into a separate `OpenApiCode.cs` file in the generated project, exposed via a static
`OpenApiCode.ExecuteTextCode()` method that the project calls instead of the beam example.

### Prerequisites to run the skill
- Windows (paths and Tekla Structures installation locations are Windows-specific)
- .NET 10 SDK
- PowerShell 7+ (`pwsh`)

When invoking the script from a different repo/solution, either pass `-CreatorProjectPath`
explicitly or rely on the `SIMPLE_TEKLA_CREATOR_PATH` environment variable (set once at the
user level, pointing at `Simple Tekla Extension Creator.csproj` or its folder).
