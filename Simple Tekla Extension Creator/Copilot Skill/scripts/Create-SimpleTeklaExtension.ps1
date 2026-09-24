param(
	[Parameter(Mandatory = $true)]
	[string]$ProjectName,

	[ValidateSet("2023", "2024", "2025", "2026", "2027")]
	[string]$Version = "2026",

	[ValidateSet("Console", "WinForms", "WPF")]
	[string]$UI = "Console",

	[switch]$OpenProject = $true,

	[string]$MacroFile,

	[string]$CreatorProjectPath
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($CreatorProjectPath)) {
	# 1. Explicit -CreatorProjectPath (already handled above by the parameter itself)
	# 2. SIMPLE_TEKLA_CREATOR_PATH environment variable (points to the .csproj or its folder)
	# 3. Fallback: assume this script still lives inside the Simple Tekla Extension Creator repo
	$envPath = $env:SIMPLE_TEKLA_CREATOR_PATH
	if (-not [string]::IsNullOrWhiteSpace($envPath)) {
		if ((Test-Path -Path $envPath -PathType Container)) {
			$CreatorProjectPath = Join-Path $envPath "Simple Tekla Extension Creator.csproj"
		}
		else {
			$CreatorProjectPath = $envPath
		}
	}
	else {
		$solutionDir = Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent
		$CreatorProjectPath = Join-Path $solutionDir "Simple Tekla Extension Creator.csproj"
	}
}

if (-not (Test-Path -Path $CreatorProjectPath)) {
	throw "Creator project file not found: $CreatorProjectPath. Pass -CreatorProjectPath explicitly, or set the SIMPLE_TEKLA_CREATOR_PATH environment variable to the csproj file (or its folder)."
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

if ($OpenProject) {
	$arguments += "--open"
}

if (-not [string]::IsNullOrWhiteSpace($MacroFile)) {
	if (-not (Test-Path -Path $MacroFile)) {
		throw "Macro file not found: $MacroFile"
	}

	$arguments += "--macro", $MacroFile
}

& dotnet @arguments
exit $LASTEXITCODE
