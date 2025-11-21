# Generate WXS file for Per-User Installation (No Admin Required)

param(
    [string]$SourceDir,
    [string]$OutputFile = "Generated-PerUser.wxs"
)

$ErrorActionPreference = "Stop"

if (!(Test-Path $SourceDir)) {
    Write-Host "ERROR: Source directory not found: $SourceDir" -ForegroundColor Red
    exit 1
}

Write-Host "Generating Per-User WXS from: $SourceDir" -ForegroundColor Green

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

# Build XML content
$xml = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs"
     xmlns:ui="http://wixtoolset.org/schemas/v4/wxs/ui"
     xmlns:util="http://wixtoolset.org/schemas/v4/wxs/util">

  <Package Name="Vertex Central Deployer (Per-User)"
           Version="1.0.0.0"
           Manufacturer="Vertex Security"
           UpgradeCode="b2c3d4e5-f6a7-4a5b-8c9d-1e2f3a4b5c6d"
           Compressed="yes"
           InstallerVersion="500"
           Scope="perUser">

    <MajorUpgrade DowngradeErrorMessage="A newer version of [ProductName] is already installed." />

    <MediaTemplate EmbedCab="yes" />

    <!-- Per-User Installation - No Admin Required -->
    <Property Id="ALLUSERS" Value="0" />

    <!-- UI Configuration -->
    <ui:WixUI Id="WixUI_InstallDir" />
    <Property Id="WIXUI_INSTALLDIR" Value="INSTALLFOLDER" />
    <WixVariable Id="WixUILicenseRtf" Value="$PSScriptRoot\License.rtf" />

    <!-- Close running applications before uninstall -->
    <util:CloseApplication Id="CloseDeployer" Target="WSMDeployer.exe"
                           CloseMessage="yes" RebootPrompt="no" />

    <!-- Features -->
    <Feature Id="MainFeature" Title="Vertex Central Deployer" Level="1">
      <ComponentGroupRef Id="AllFiles" />
      <ComponentRef Id="StartMenuShortcut" />
      <ComponentRef Id="DesktopShortcut" />
      <ComponentRef Id="DataFolderComponent" />
      <ComponentRef Id="DatabaseFolderComponent" />
      <ComponentRef Id="LogsFolderComponent" />
    </Feature>

    <!-- Per-User Directories -->
    <StandardDirectory Id="LocalAppDataFolder">
      <Directory Id="LocalAppDataProgramsFolder" Name="Programs">
        <Directory Id="INSTALLFOLDER" Name="VertexDeployer" />
      </Directory>
    </StandardDirectory>

    <StandardDirectory Id="ProgramMenuFolder">
      <Directory Id="ApplicationProgramsFolder" Name="Vertex Central Deployer" />
    </StandardDirectory>

    <StandardDirectory Id="DesktopFolder">
      <Directory Id="DesktopShortcutFolder" />
    </StandardDirectory>

    <StandardDirectory Id="AppDataFolder">
      <Directory Id="DeployerDataFolder" Name="VertexDeployer">
        <Directory Id="DatabaseFolder" Name="Database" />
        <Directory Id="LogsFolder" Name="Logs" />
      </Directory>
    </StandardDirectory>

  </Package>

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

    <!-- Start Menu Shortcut -->
    <DirectoryRef Id="ApplicationProgramsFolder">
      <Component Id="StartMenuShortcut" Guid="a6b7c8d9-e0f1-2a3b-4c5d-6e7f8a9b0c1d">
        <Shortcut Id="StartMenuDeployer"
                  Name="Vertex Central Deployer"
                  Description="Deploy and manage Vertex installations"
                  Target="[INSTALLFOLDER]WSMDeployer.exe"
                  WorkingDirectory="INSTALLFOLDER" />
        <RemoveFolder Id="RemoveApplicationProgramsFolder" On="uninstall" />
        <RegistryValue Root="HKCU"
                       Key="Software\Vertex\VertexDeployer"
                       Name="StartMenuShortcut"
                       Type="integer"
                       Value="1"
                       KeyPath="yes" />
      </Component>
    </DirectoryRef>

    <!-- Desktop Shortcut -->
    <DirectoryRef Id="DesktopShortcutFolder">
      <Component Id="DesktopShortcut" Guid="b7c8d9e0-f1a2-3b4c-5d6e-7f8a9b0c1d2e">
        <Shortcut Id="DesktopDeployer"
                  Name="Vertex Central Deployer"
                  Description="Deploy and manage Vertex installations"
                  Target="[INSTALLFOLDER]WSMDeployer.exe"
                  WorkingDirectory="INSTALLFOLDER" />
        <RegistryValue Root="HKCU"
                       Key="Software\Vertex\VertexDeployer"
                       Name="DesktopShortcut"
                       Type="integer"
                       Value="1"
                       KeyPath="yes" />
      </Component>
    </DirectoryRef>

    <!-- Create AppData Folder -->
    <DirectoryRef Id="DeployerDataFolder">
      <Component Id="DataFolderComponent" Guid="c8d9e0f1-a2b3-4c5d-6e7f-8a9b0c1d2e3f">
        <CreateFolder />
        <RemoveFolder Id="RemoveDeployerFolder" On="uninstall" />
        <RegistryValue Root="HKCU"
                       Key="Software\Vertex\VertexDeployer"
                       Name="DataFolder"
                       Type="integer"
                       Value="1"
                       KeyPath="yes" />
      </Component>
    </DirectoryRef>

    <!-- Create Database Folder -->
    <DirectoryRef Id="DatabaseFolder">
      <Component Id="DatabaseFolderComponent" Guid="d9e0f1a2-b3c4-5d6e-7f8a-9b0c1d2e3f4a">
        <CreateFolder />
        <RemoveFolder Id="RemoveDatabaseFolder" On="uninstall" />
        <RegistryValue Root="HKCU"
                       Key="Software\Vertex\VertexDeployer"
                       Name="DatabaseFolder"
                       Type="integer"
                       Value="1"
                       KeyPath="yes" />
      </Component>
    </DirectoryRef>

    <!-- Create Logs Folder -->
    <DirectoryRef Id="LogsFolder">
      <Component Id="LogsFolderComponent" Guid="e0f1a2b3-c4d5-6e7f-8a9b-0c1d2e3f4a5b">
        <CreateFolder />
        <RemoveFolder Id="RemoveLogsFolder" On="uninstall" />
        <RegistryValue Root="HKCU"
                       Key="Software\Vertex\VertexDeployer"
                       Name="LogsFolder"
                       Type="integer"
                       Value="1"
                       KeyPath="yes" />
      </Component>
    </DirectoryRef>

  </Fragment>

</Wix>
"@

# Write to file
$xml | Out-File -FilePath $OutputFile -Encoding UTF8

Write-Host "Generated: $OutputFile" -ForegroundColor Green
Write-Host "Total components: $componentCount" -ForegroundColor Cyan
