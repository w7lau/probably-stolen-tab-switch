param([string]$GameDir = 'R:\SteamLibrary\steamapps\common\Probably Stolen')
$ErrorActionPreference = 'Stop'
[xml]$project = [IO.File]::ReadAllText("$PSScriptRoot\src\StorageTabCycle.csproj")
$version = [string]$project.Project.PropertyGroup.Version
[xml]$manifest = [IO.File]::ReadAllText("$PSScriptRoot\package\StorageTabCycle\manifest.xml")
if ($manifest.Manifest.ModVersion -ne $version) { throw 'Project and package versions must match.' }
$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if (!$dotnet) { $dotnet = 'C:\Program Files\dotnet\dotnet.exe' }
if (!(Test-Path -LiteralPath "$GameDir\Probably Stolen_Data\Managed\Assembly-CSharp.dll")) {
    throw 'GameDir must point to the installed full game.'
}
& $dotnet build "$PSScriptRoot\src\StorageTabCycle.csproj" -c Release "-p:GameDir=$GameDir" --nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
Copy-Item -LiteralPath "$PSScriptRoot\src\bin\Release\netstandard2.1\StorageTabCycle.dll" -Destination "$PSScriptRoot\package\StorageTabCycle\StorageTabCycle.dll"
Copy-Item -LiteralPath "$PSScriptRoot\LICENSE" -Destination "$PSScriptRoot\package\StorageTabCycle\LICENSE.txt"
New-Item -ItemType Directory -Force "$PSScriptRoot\dist" | Out-Null
Compress-Archive -Path "$PSScriptRoot\package\StorageTabCycle" -DestinationPath "$PSScriptRoot\dist\StorageTabCycle-$version.zip" -Force
Write-Output "Built package/StorageTabCycle and dist/StorageTabCycle-$version.zip"
