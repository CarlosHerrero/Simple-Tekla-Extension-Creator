# Copilot instructions for this repository

## Simple Tekla Extension Creator skill

This repo ships an agent skill that scaffolds a new Tekla Structures extension project
(Console, WinForms, or WPF, targeting Tekla Structures 2023-2027) without needing to open
the app's UI.

- Skill description and usage: [Simple Tekla Extension Creator/Copilot Skill/SKILL.md](../Simple%20Tekla%20Extension%20Creator/Copilot%20Skill/SKILL.md)
- Script to invoke: `Simple Tekla Extension Creator/Copilot Skill/scripts/Create-SimpleTeklaExtension.ps1`

Use this skill whenever the user asks to create/scaffold a new Tekla extension, sample, or
starter project. Read `SKILL.md` first for the full list of parameters (`ProjectName`,
`Version`, `UI`, `OpenProject`, `CreatorProjectPath`) and expected behavior before invoking
the script.

### Prerequisites to run the skill
- Windows (paths and Tekla Structures installation locations are Windows-specific)
- .NET 10 SDK
- PowerShell 7+ (`pwsh`)

When invoking the script from a different repo/solution, either pass `-CreatorProjectPath`
explicitly or rely on the `SIMPLE_TEKLA_CREATOR_PATH` environment variable (set once at the
user level, pointing at `Simple Tekla Extension Creator.csproj` or its folder).
