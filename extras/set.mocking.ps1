param (
    [Parameter()]
    [string]
    $AppSettingName = "isMockEnabled",

    [Parameter()]
    [Boolean]
    $EnableMocking = $true,

    [Parameter(Mandatory = $True)]
    [string]
    $LogicAppName,

    [Parameter(Mandatory = $True)]
    [string]
    $ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [string]
    $SubscriptionId
)

# Set URI for Azure Management API
$managementUri = "https://management.azure.com/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName" +
                 "/providers/Microsoft.Web/sites/$LogicAppName/config/appsettings?api-version=2024-11-01"

# Get access token for calling Azure Management API
$accessToken = (Get-AzAccessToken -ResourceUrl 'https://management.azure.com/' -AsSecureString).Token
$headers = @{
    'Authorization' = "Bearer $(ConvertFrom-SecureString $accessToken -AsPlainText)"
    'Content-Type'  = "application/json"
}

# Build the app settings body - PATCH only updates the specified setting,
# all other app settings remain unchanged.
$appSettingsBody = @{
    properties = @{
        $AppSettingName = "$($EnableMocking)"
    }
}

$jsonBody = $appSettingsBody | ConvertTo-Json

Write-Output "Invoking management uri: $managementUri"
Write-Output "Setting '$AppSettingName' to '$EnableMocking' on '$LogicAppName'"

try {
    Invoke-RestMethod -Method PATCH -Uri $managementUri -Headers $headers -Body $jsonBody `
                      -ErrorAction Stop -ErrorVariable appSettingsError
    Write-Output "Successfully updated '$AppSettingName'."
}
catch {
    # If the invoke fails (404 or another HTTP error), the app setting will not be changed.
    Write-Output "Failed to set app setting '$AppSettingName' for '$LogicAppName' through management URL '$managementUri'."
    Write-Output $appSettingsError
}
