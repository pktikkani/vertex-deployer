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

# Build XML content
$xml = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs"
     xmlns:ui="http://wixtoolset.org/schemas/v4/wxs/ui"
     xmlns:util="http://wixtoolset.org/schemas/v4/wxs/util">

  <Package Name="Vertex Central Deployer"
           Version="1.0.0.0"
           Manufacturer="Vertex Security"
           UpgradeCode="a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d"
           Compressed="yes"
           InstallerVersion="500"
           Scope="perMachine">

    <MajorUpgrade DowngradeErrorMessage="A newer version of [ProductName] is already installed." />
    <MediaTemplate EmbedCab="yes" />

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

    <!-- Standard Directories -->
    <StandardDirectory Id="ProgramFiles64Folder">
      <Directory Id="INSTALLFOLDER" Name="VertexDeployer" />
    </StandardDirectory>

    <StandardDirectory Id="ProgramMenuFolder">
      <Directory Id="ApplicationProgramsFolder" Name="Vertex Central Deployer" />
    </StandardDirectory>

    <StandardDirectory Id="DesktopFolder">
      <Directory Id="DesktopShortcutFolder" />
    </StandardDirectory>

    <StandardDirectory Id="CommonAppDataFolder">
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
      <Component Id="StartMenuShortcut" Guid="f6a7b8c9-d0e1-2f3a-4b5c-6d7e8f9a0b1c">
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
      <Component Id="DesktopShortcut" Guid="a7b8c9d0-e1f2-3a4b-5c6d-7e8f9a0b1c2d">
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

    <!-- Create ProgramData Folder -->
    <DirectoryRef Id="DeployerDataFolder">
      <Component Id="DataFolderComponent" Guid="b8c9d0e1-f2a3-4b5c-6d7e-8f9a0b1c2d3e">
        <CreateFolder>
          <util:PermissionEx User="Administrators" GenericAll="yes" />
          <util:PermissionEx User="SYSTEM" GenericAll="yes" />
          <util:PermissionEx User="Users" GenericRead="yes" GenericWrite="yes" GenericExecute="yes" />
        </CreateFolder>
        <RemoveFolder Id="RemoveDeployerFolder" On="uninstall" />
        <RegistryValue Root="HKLM"
                       Key="Software\Vertex\VertexDeployer"
                       Name="DataFolder"
                       Type="integer"
                       Value="1"
                       KeyPath="yes" />
      </Component>
    </DirectoryRef>

    <!-- Create Database Folder -->
    <DirectoryRef Id="DatabaseFolder">
      <Component Id="DatabaseFolderComponent" Guid="c9d0e1f2-a3b4-5c6d-7e8f-9a0b1c2d3e4f">
        <CreateFolder />
        <RemoveFolder Id="RemoveDatabaseFolder" On="uninstall" />
        <RegistryValue Root="HKLM"
                       Key="Software\Vertex\VertexDeployer"
                       Name="DatabaseFolder"
                       Type="integer"
                       Value="1"
                       KeyPath="yes" />
      </Component>
    </DirectoryRef>

    <!-- Create Logs Folder -->
    <DirectoryRef Id="LogsFolder">
      <Component Id="LogsFolderComponent" Guid="d0e1f2a3-b4c5-6d7e-8f9a-0b1c2d3e4f5a">
        <CreateFolder />
        <RemoveFolder Id="RemoveLogsFolder" On="uninstall" />
        <RegistryValue Root="HKLM"
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
