$ErrorActionPreference = 'Stop'
$publishRoot = 'D:\PRU_Git\KHTN6\outputs\backend-publish'
$candidateRoot = Join-Path $publishRoot 'candidate'
$baselineRoot = Join-Path $publishRoot 'baseline'
$entries = Get-Content (Join-Path $publishRoot 'backend-tree.json') -Raw | ConvertFrom-Json
$results = $entries | ForEach-Object -ThrottleLimit 5 -Parallel {
    $entry = $_
    $encoded = gh api ('repos/PRU213/KHTN6/git/blobs/' + $entry.sha) --jq .content
    if ($LASTEXITCODE -ne 0) { throw ('Failed fetching ' + $entry.path) }
    $target = Join-Path $using:baselineRoot $entry.path
    New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
    [IO.File]::WriteAllBytes($target, [Convert]::FromBase64String(($encoded -join '')))
    [pscustomobject]@{path=$entry.path; ok=$true}
}
if ($results.Count -ne $entries.Count) { throw 'Incomplete baseline' }
New-Item -ItemType Directory -Force -Path $candidateRoot | Out-Null
Copy-Item -LiteralPath (Join-Path $baselineRoot 'PlanetServer') -Destination $candidateRoot -Recurse
$selected = @('server.mjs','football.mjs','tutor.mjs','tutor-auth.mjs','duel-review.test.mjs','tutor-guidance.test.mjs','tutor.test.mjs','practice-tutor-gate.test.mjs','football.test.mjs','essay.test.mjs','DUEL-REVIEW.md','ESSAY-TUTOR.md')
$changed = @()
foreach ($name in $selected) {
    $source = Join-Path 'D:\PRU_Git\KHTN6\PlanetServer' $name
    $baseline = Join-Path $baselineRoot ('PlanetServer\' + $name)
    $current = [IO.File]::ReadAllText($source).Replace("`r`n", "`n")
    $previous = if (Test-Path -LiteralPath $baseline) { [IO.File]::ReadAllText($baseline).Replace("`r`n", "`n") } else { $null }
    if ($current -ne $previous) {
        Copy-Item -LiteralPath $source -Destination (Join-Path $candidateRoot ('PlanetServer\' + $name))
        $changed += ('PlanetServer/' + $name)
    }
}
$changed | ConvertTo-Json | Set-Content (Join-Path $publishRoot 'changed-files.json')
Write-Output ('Fetched baseline files: ' + $results.Count)
Write-Output 'Changed backend files:'
$changed
