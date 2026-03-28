$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $projectRoot "windows\CopySave.Windows\CopySave.Windows.csproj"
$outputDir = Join-Path $projectRoot "dist\windows"
$dotnetPath = Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"

if (-not (Test-Path $dotnetPath)) {
    $dotnetPath = "dotnet"
}

if (Test-Path $outputDir) {
    Remove-Item -LiteralPath $outputDir -Recurse -Force
}

New-Item -ItemType Directory -Force $outputDir | Out-Null
& $dotnetPath publish $projectPath `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $outputDir
