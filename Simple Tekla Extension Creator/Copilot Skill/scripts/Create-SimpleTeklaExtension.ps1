param(
	[Parameter(Mandatory = $true)]
	[string]$ProjectName,

	[ValidateSet("2023", "2024", "2025", "2026", "2027")]
	[string]$Version = "2026",

	[ValidateSet("Console", "WinForms", "WPF")]
	[string]$UI = "Console",

	[string]$CreatorProjectPath
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($CreatorProjectPath)) {
	$solutionDir = Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent
	$CreatorProjectPath = Join-Path $solutionDir "Simple Tekla Extension Creator.csproj"
}

if (-not (Test-Path -Path $CreatorProjectPath)) {
	throw "Creator project file not found: $CreatorProjectPath"
}

$arguments = @(
	"run",
	"--project", $CreatorProjectPath,
	"--",
	"--non-interactive",
	"--project", $ProjectName,
	"--version", $Version,
	"--ui", $UI
)

& dotnet @arguments
exit $LASTEXITCODE
