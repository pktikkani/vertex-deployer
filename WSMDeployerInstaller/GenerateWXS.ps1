# Generate WXS file with all Deployer files explicitly listed

param(
    [string]$SourceDir = "..\WSMDeployer\bin\Release\net8.0\win-x64\publish",
    [string]$OutputFile = "Generated.wxs"
)

$ErrorActionPreference = "Stop"

if (!(Test-Path $SourceDir)) {
    Write-Host "ERROR: Source directory not found: $SourceDir" -ForegroundColor Red
    exit 1
}

Write-Host "Generating WXS from: $SourceDir" -ForegroundColor Green

# Get all files
$files = Get-ChildItem -Path $SourceDir -Recurse -File | Where-Object { $_.Extension -ne '.pdb' }

Write-Host "Found $($files.Count) files" -ForegroundColor Cyan

# Generate unique GUIDs
function New-Guid {
    return [System.Guid]::NewGuid().ToString()
}

# Generate component ID from filename
function Get-ComponentId {
    param($file, $index)
    $name = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
    $name = $name -replace '[^a-zA-Z0-9_]', '_'
    return "Cmp_${name}_$index"
}

# Build XML content - Only generate fragments, not a full Package
$xml = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">

  <Fragment>
    <ComponentGroup Id="AllFiles" Directory="INSTALLFOLDER">
"@

# Group files by directory
$filesByDir = @{}
foreach ($file in $files) {
    $relPath = $file.FullName.Substring($SourceDir.Length + 1)
    $dir = [System.IO.Path]::GetDirectoryName($relPath)
    if ($dir -eq "") { $dir = "ROOT" }

    if (!$filesByDir.ContainsKey($dir)) {
        $filesByDir[$dir] = @()
    }
    $filesByDir[$dir] += $file
}

# Add components for each file
$componentCount = 0
foreach ($dir in $filesByDir.Keys | Sort-Object) {
    foreach ($file in $filesByDir[$dir]) {
        $relPath = $file.FullName.Substring($SourceDir.Length + 1)
        $componentCount++
        $compId = Get-ComponentId -file $file -index $componentCount
        $guid = New-Guid

        $keyPath = if ($file.Name -eq "WSMDeployer.exe") { ' KeyPath="yes"' } else { '' }

        $xml += @"

      <Component Id="$compId" Guid="$guid">
        <File Source="`$(var.DeployerDir)\$relPath"$keyPath />
      </Component>
"@
    }
}

$xml += @"

    </ComponentGroup>
  </Fragment>

</Wix>
"@

# Write to file
$xml | Out-File -FilePath $OutputFile -Encoding UTF8

Write-Host "Generated: $OutputFile" -ForegroundColor Green
Write-Host "Total components: $componentCount" -ForegroundColor Cyan
