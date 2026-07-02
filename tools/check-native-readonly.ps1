$ErrorActionPreference = "Stop"

$nativeRoot = Join-Path $PSScriptRoot "..\Statlyn.NativeConnector"
$forbiddenPatterns = @(
  "WriteProcessMemory\s*\(",
  "CreateRemoteThread\s*\(",
  "VirtualAllocEx\s*\(",
  "SetThreadContext\s*\(",
  "SuspendThread\s*\(",
  "ResumeThread\s*\("
)

$files = Get-ChildItem -LiteralPath $nativeRoot -Recurse -File | Where-Object {
  $_.Extension -in @(".cpp", ".h", ".hpp")
}
$violations = @()

foreach ($file in $files) {
  $content = Get-Content -Raw -LiteralPath $file.FullName
  foreach ($pattern in $forbiddenPatterns) {
    if ($content -match $pattern) {
      $violations += "$($file.FullName): $pattern"
    }
  }
}

if ($violations.Count -gt 0) {
  Write-Error ("Forbidden native connector call found:`n" + ($violations -join "`n"))
}

Write-Output "Native connector read-only scan passed."
