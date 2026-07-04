param(
    [string]$Root = (Join-Path $PSScriptRoot "..\memory-maps")
)

$ErrorActionPreference = "Stop"

function Write-Step($Message) {
    Write-Host "[Statlyn] $Message"
}

function Fail($Message) {
    throw "[Statlyn] Memory-map validation failed: $Message"
}

function First-Text($Values) {
    foreach ($value in $Values) {
        if ($null -ne $value -and -not [string]::IsNullOrWhiteSpace([string]$value)) {
            return [string]$value
        }
    }

    return ""
}

$resolvedRoot = Resolve-Path -LiteralPath $Root -ErrorAction SilentlyContinue
if (-not $resolvedRoot) {
    Write-Step "Memory-map directory not found. No maps loaded."
    exit 0
}

$jsonFiles = Get-ChildItem -LiteralPath $resolvedRoot.Path -Recurse -Filter "*.json" -File
$mapFiles = $jsonFiles | Where-Object { $_.Name -like "*.map.json" }
$templateCount = 0
$validatedCount = 0

foreach ($file in $jsonFiles) {
    try {
        $content = Get-Content -Raw -LiteralPath $file.FullName
        $json = $content | ConvertFrom-Json
    }
    catch {
        Fail "Malformed JSON in $($file.Name)."
    }

    if ($file.Name -notlike "*.map.json") {
        continue
    }

    if ($json.isTemplate -eq $true -or $json.build -eq "template") {
        $templateCount++
    }

    if ($json.isValidated -eq $true -or $json.validated -eq $true -or $json.supported -eq $true) {
        $validatedCount++
    }

    $allowedUsage = First-Text @($json.allowedUsage)
    if ($allowedUsage -match "(?i)write|modify|inject") {
        Fail "Write-enabled map usage is not allowed in $($file.Name)."
    }

    $fields = @($json.fields)
    foreach ($field in $fields) {
        if ($null -eq $field) {
            continue
        }

        if ($field.isReadOnly -eq $false) {
            Fail "Write-enabled field access is not allowed in $($file.Name)."
        }

        $visibility = First-Text @($field.visibility, $field.visibilityCategory)
        $fieldName = First-Text @($field.fieldName, $field.fieldKey)
        $compactName = ($fieldName -replace "[-_\s]", "")
        $isHidden = $field.isHidden -eq $true `
            -or $visibility -match "(?i)hidden|neverVisible|blocked|forbidden" `
            -or $compactName -match "(?i)currentability|potentialability|professionalism|personality" `
            -or $fieldName -match "^(?i:CA|PA)$"

        if ($isHidden -and ($field.canDisplay -eq $true -or $field.canStore -eq $true -or $field.canScore -eq $true)) {
            Fail "Blocked field policy is not enforced in $($file.Name)."
        }

        if ([string]::IsNullOrWhiteSpace($visibility)) {
            Fail "Field visibility must be declared in $($file.Name)."
        }
    }
}

Write-Step "Validated $($jsonFiles.Count) JSON file(s), $($mapFiles.Count) map file(s), $templateCount template map(s), $validatedCount validated map(s)."
