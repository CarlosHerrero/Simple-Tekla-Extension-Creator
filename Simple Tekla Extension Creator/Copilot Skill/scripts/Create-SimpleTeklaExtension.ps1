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

if (-not [string]::IsNullOrWhiteSpace($MacroFile) -and -not (Test-Path -Path $MacroFile)) {
	throw "Macro file not found: $MacroFile"
}

function Get-CommonArguments {
	$arguments = @(
		"--project", $ProjectName,
		"--version", $Version,
		"--ui", $UI
	)

	if ($OpenProject) {
		$arguments += "--open"
	}

	if (-not [string]::IsNullOrWhiteSpace($MacroFile)) {
		$arguments += "--macro", $MacroFile
	}

	return $arguments
}

# Preferred path: use the installed dotnet tool (installed via
# `dotnet tool install --global SimpleTeklaExtensionCreator`) so consumers don't need
# the source repo at all.
$installedTool = Get-Command "simple-tekla-extension-creator" -ErrorAction SilentlyContinue

if (-not [string]::IsNullOrWhiteSpace($CreatorProjectPath) -or -not $installedTool) {
	# Fallback / dev path: run the tool project directly with `dotnet run --project`.
	if ([string]::IsNullOrWhiteSpace($CreatorProjectPath)) {
		# 1. Explicit -CreatorProjectPath (already handled above by the parameter itself)
		# 2. SIMPLE_TEKLA_CREATOR_PATH environment variable (points to the .csproj or its folder)
		# 3. Fallback: assume this script still lives inside the Simple Tekla Extension Creator repo
		$envPath = $env:SIMPLE_TEKLA_CREATOR_PATH
		if (-not [string]::IsNullOrWhiteSpace($envPath)) {
			if ((Test-Path -Path $envPath -PathType Container)) {
				$CreatorProjectPath = Join-Path $envPath "Simple Tekla Extension Creator.Tool.csproj"
			}
			else {
				$CreatorProjectPath = $envPath
			}
		}
		else {
			$solutionDir = Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent
			$CreatorProjectPath = Join-Path $solutionDir "Simple Tekla Extension Creator.Tool\Simple Tekla Extension Creator.Tool.csproj"
		}
	}

	if (-not (Test-Path -Path $CreatorProjectPath)) {
		throw "Creator tool project file not found: $CreatorProjectPath. Install the dotnet tool (dotnet tool install --global SimpleTeklaExtensionCreator), pass -CreatorProjectPath explicitly, or set the SIMPLE_TEKLA_CREATOR_PATH environment variable to the csproj file (or its folder)."
	}

	$arguments = @("run", "--project", $CreatorProjectPath, "--") + (Get-CommonArguments)

	& dotnet @arguments
	exit $LASTEXITCODE
}

# Use the globally installed tool.
$arguments = Get-CommonArguments
& simple-tekla-extension-creator @arguments
exit $LASTEXITCODE
