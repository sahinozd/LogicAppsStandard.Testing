# ============================================================
# set-package-version.ps1
#
# GitHub Actions equivalent of:
#   set.package.version.ps1  (Azure DevOps version)
#
# Difference: instead of writing a ##vso[task.setvariable]
# directive (Azure DevOps syntax), this script writes to
# $env:GITHUB_OUTPUT (GitHub Actions syntax).
#
# Usage in a GitHub Actions step:
#   - name: Set package version
#     id: version
#     shell: pwsh
#     run: ./.github/scripts/set-package-version.ps1 `
#            -CurrentBranch "${{ github.ref }}" `
#            -MajorVersion "1" `
#            -MinorVersion "1" `
#            -PatchVersion "${{ github.run_number }}"
#
# The version is then available in subsequent steps as:
#   ${{ steps.version.outputs.packageVersion }}
# ============================================================

param (
    [Parameter(Mandatory = $true)]
    [string] $CurrentBranch,

    [Parameter(Mandatory = $true)]
    [string] $MajorVersion,

    [Parameter(Mandatory = $true)]
    [string] $MinorVersion,

    [Parameter(Mandatory = $true)]
    [string] $PatchVersion
)

# Build the version string and strip any dashes (same logic as the ADO version)
$packageVersion = "$MajorVersion.$MinorVersion.$PatchVersion"
$packageVersion = $packageVersion.Replace("-", "")

Write-Host "Branch   : $CurrentBranch"
Write-Host "Version  : $packageVersion"

# Write the output variable for GitHub Actions
# (This file is set by the runner - do not hardcode the path)
"packageVersion=$packageVersion" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
