<#
.Synopsis
    Build script

.Description
    TASKS AND REQUIREMENTS
    Initialize and clean repository
    Restore packages, workflows, tools, and workloads
    Format code
    Build projects and the solution
    Run tests
    Pack application artifacts
    Publish release artifacts
#>

#requires -Version 7.4
#requires -PSEdition Core

[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', '', Justification = 'Parameter is passed through InvokeBuild checkpoints.')]
param(
    [Parameter()]
    [string]
    $Version,
    [Parameter()]
    [string]
    $Instance
)

Set-StrictMode -Version Latest

$SolutionPath = 'CovenantCouncil.slnx'
$PackageId = 'CovenantCouncil'

# Synopsis: Initialize folders and variables
Task Init {
    if ([System.String]::IsNullOrWhiteSpace($Instance)) {
        $Instance = Get-Date -Format 'yyyyMMddHHmmss'
    }

    $trashFolder = Join-Path -Path . -ChildPath '.trash'
    $trashFolder = Join-Path -Path $trashFolder -ChildPath $Instance
    New-Item -Path $trashFolder -ItemType Directory -Force | Out-Null
    $trashFolder = Resolve-Path -Path $trashFolder

    $buildArtifactsFolder = Join-Path -Path $trashFolder -ChildPath 'artifacts'
    New-Item -Path $buildArtifactsFolder -ItemType Directory -Force | Out-Null

    $windowsBuildArtifactsFolder = Join-Path -Path $buildArtifactsFolder -ChildPath 'windows'
    New-Item -Path $windowsBuildArtifactsFolder -ItemType Directory -Force | Out-Null

    $state = [PSCustomObject]@{
        PackageId                   = $PackageId
        NextVersion                 = $null
        TrashFolder                 = $trashFolder
        BuildArtifactsFolder        = $buildArtifactsFolder
        WindowsBuildArtifactsFolder = $windowsBuildArtifactsFolder
    }

    $state | Export-Clixml -Path ".\.trash\$Instance\state.clixml"
    Write-Output $state
}

# Synopsis: Clean previous build leftovers
Task Clean Init, {
    Get-ChildItem -Directory |
        Where-Object { -not $_.Name.StartsWith('.') } |
        ForEach-Object { Get-ChildItem -Path $_ -Recurse -Directory } |
        Where-Object { ($_.Name -eq 'bin') -or ($_.Name -eq 'obj') } |
        ForEach-Object { Remove-Item -Path $_ -Recurse -Force }
}

# Synopsis: Ensure Central Package Versions compliance
Task EnsureCentralPackageVersions Clean, {
    $projectFiles = Get-ChildItem -Path . `
        -Recurse `
        -Include *.csproj, *.fsproj, *.vbproj `
        -File

    $violations = @()

    foreach ($projectFile in $projectFiles) {
        try {
            [xml]$xml = Get-Content $projectFile.FullName -Raw
        }
        catch {
            throw "Failed to parse XML: $($projectFile.FullName)"
        }

        $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
        $ns.AddNamespace('msb', $xml.DocumentElement.NamespaceURI)

        $nodes = $xml.SelectNodes('//*[@VersionOverride]', $ns)

        foreach ($node in $nodes) {
            $violations += [PSCustomObject]@{
                File  = $projectFile.FullName
                Node  = $node.Name
                Value = $node.GetAttribute('VersionOverride')
            }
        }
    }

    if ($violations.Count -gt 0) {
        throw "VersionOverride attributes are not allowed. File: $($violations[0].File) Node: <$($violations[0].Node)>"
    }
}

# Synopsis: Restore workloads
Task RestoreWorkloads Clean, {
    Exec { dotnet workload restore }
}

# Synopsis: Restore tools
Task RestoreTools Clean, {
    Exec { dotnet tool restore }
}

# Synopsis: Restore packages
Task RestorePackages Clean, EnsureCentralPackageVersions, {
    $solution = Resolve-Path -Path $SolutionPath
    Exec { dotnet restore $solution }
}

# Synopsis: Restore
Task Restore RestoreWorkloads, RestoreTools, RestorePackages

# Synopsis: Format XML files
Task FormatXmlFiles Clean, {
    Get-ChildItem -Include *.xml, *.config, *.props, *.targets, *.nuspec, *.resx, *.ruleset, *.vsixmanifest, *.vsct, *.xlf, *.csproj, *.fsproj, *.vbproj, *.slnx -Recurse -File |
        Where-Object { -not (git check-ignore $PSItem) } |
        ForEach-Object {
            Write-Output "Formatting XML File: $PSItem"
            $content = Get-Content -Path $PSItem -Raw
            $xml = [xml]$content
            $xml.Save($PSItem)
        }
}

# Synopsis: Format whitespace
Task FormatWhitespace Restore, {
    $solution = Resolve-Path -Path $SolutionPath
    Exec { dotnet format whitespace --verbosity diagnostic $solution }
}

# Synopsis: Format analyzers
Task FormatAnalyzers Restore, {
    $solution = Resolve-Path -Path $SolutionPath
    Exec { dotnet format analyzers --severity info --verbosity diagnostic $solution }
}

# Synopsis: Format style
Task FormatStyle Restore, {
    $solution = Resolve-Path -Path $SolutionPath
    Exec { dotnet format style --severity info --verbosity diagnostic $solution }
}

# Synopsis: Format
Task Format Restore, FormatXmlFiles, FormatWhitespace, FormatStyle, FormatAnalyzers

# Synopsis: Estimate next version
Task EstimateVersion Restore, {
    $state = Import-Clixml -Path ".\.trash\$Instance\state.clixml"
    if ($Version) {
        $state.NextVersion = [System.Management.Automation.SemanticVersion]$Version
    }
    else {
        $currentCommit = git rev-parse HEAD
        $headTagVersion = $null
        $headTag = git tag --points-at HEAD | Select-Object -First 1

        if (-not [System.String]::IsNullOrWhiteSpace($headTag)) {
            $headTagVersion = [System.Management.Automation.SemanticVersion]$headTag
        }

        if ($null -eq $headTagVersion) {
            $state.NextVersion = [System.Management.Automation.SemanticVersion]::New(0, 1, 0, 'alpha.1', $currentCommit)
        }
        else {
            $state.NextVersion = [System.Management.Automation.SemanticVersion]::New($headTagVersion.Major, $headTagVersion.Minor, $headTagVersion.Patch, $headTagVersion.PreReleaseLabel, $currentCommit)
        }
    }

    $state.NextVersion
    $state | Export-Clixml -Path ".\.trash\$Instance\state.clixml"
    Write-Output "Next version estimated to be $($state.NextVersion)"
    Write-Output $state
}

# Synopsis: Build solution
Task BuildSolution EstimateVersion, {
    $state = Import-Clixml -Path ".\.trash\$Instance\state.clixml"
    $solution = Resolve-Path -Path $SolutionPath
    $nextVersion = $state.NextVersion

    Exec { dotnet build $solution /p:Configuration=Release /p:Version=$nextVersion }
}

# Synopsis: Build Windows app artifacts
Task BuildWindowsApp EstimateVersion, {
    $state = Import-Clixml -Path ".\.trash\$Instance\state.clixml"
    $project = Resolve-Path -Path 'src/CovenantCouncil.App/CovenantCouncil.App.csproj'
    $nextVersion = $state.NextVersion
    $outputFolder = $state.WindowsBuildArtifactsFolder

    Exec { dotnet publish $project --framework net10.0-windows10.0.19041.0 /p:Configuration=Release /p:Version=$nextVersion /p:WindowsPackageType=None /p:PublishDir=$outputFolder }
}

# Synopsis: Build
Task Build Format, BuildSolution

# Synopsis: Test
Task Test UnitTest, FunctionalTest, IntegrationTest

# Synopsis: Unit Test
Task UnitTest Build, {
    Exec { dotnet test 'tests/CovenantCouncil.UnitTests/CovenantCouncil.UnitTests.csproj' --no-build /p:Configuration=Release }
}

# Synopsis: Functional Test
Task FunctionalTest Build, {
    Exec { dotnet test 'tests/CovenantCouncil.FunctionalTests/CovenantCouncil.FunctionalTests.csproj' --no-build /p:Configuration=Release }
}

# Synopsis: Integration Test
Task IntegrationTest Build, {
    Exec { dotnet test 'tests/CovenantCouncil.IntegrationTests/CovenantCouncil.IntegrationTests.csproj' --no-build /p:Configuration=Release }
}

# Synopsis: Pack application artifacts
Task Pack Build, BuildWindowsApp, Test, {
    $state = Import-Clixml -Path ".\.trash\$Instance\state.clixml"
    $trashFolder = $state.TrashFolder
    $buildArtifactsFolder = $state.BuildArtifactsFolder
    $nextVersion = $state.NextVersion
    $packagePath = Join-Path -Path $trashFolder -ChildPath "$($state.PackageId)-$nextVersion.zip"

    if (Test-Path -Path $packagePath) {
        Remove-Item -Path $packagePath -Force
    }

    Compress-Archive -Path (Join-Path -Path $buildArtifactsFolder -ChildPath '*') -DestinationPath $packagePath
    Write-Output "Packed application artifacts to $packagePath"
}

# Synopsis: Publish release artifacts
Task Publish Pack, {
    $state = Import-Clixml -Path ".\.trash\$Instance\state.clixml"
    $trashFolder = $state.TrashFolder
    $nextVersion = $state.NextVersion
    $packagePath = Join-Path -Path $trashFolder -ChildPath "$($state.PackageId)-$nextVersion.zip"

    if ($env:GITHUB_ACTIONS -and $env:GITHUB_REF_NAME) {
        Exec { gh release upload $env:GITHUB_REF_NAME $packagePath --clobber }
    }
    else {
        Write-Output "Release artifact ready: $packagePath"
    }
}
