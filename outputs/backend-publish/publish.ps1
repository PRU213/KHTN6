$ErrorActionPreference = 'Stop'
$publishRoot = 'D:\PRU_Git\KHTN6\outputs\backend-publish'
$parent = 'c0b0dd3f843ed63fe00d2eb94cc972a7ce745447'
$current = gh api repos/PRU213/KHTN6/git/ref/heads/main --jq .object.sha
if ($LASTEXITCODE -ne 0 -or $current.Trim() -ne $parent) { throw 'Main changed; prepare and test again before publishing.' }
$baseline = Get-Content (Join-Path $publishRoot 'remote-tree.json') -Raw | ConvertFrom-Json
$files = Get-Content (Join-Path $publishRoot 'changed-files.json') -Raw | ConvertFrom-Json
$entries = foreach ($path in $files) {
    if ($path -notlike 'PlanetServer/*' -or $path -match '/\.env|/\.cache/|/data/') { throw 'Unexpected publishing path.' }
    $content = [IO.File]::ReadAllText((Join-Path (Join-Path $publishRoot 'candidate') $path)).Replace("`r`n", "`n")
    if ($content -match 'AIza[0-9A-Za-z_-]{30,}|gh[pousr]_[0-9A-Za-z]{20,}|sk-[A-Za-z0-9_-]{20,}|-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----') { throw ('Credential detected in ' + $path) }
    @{path=$path; mode='100644'; type='blob'; content=$content}
}
$treeBody = @{base_tree=$baseline.sha; tree=@($entries)} | ConvertTo-Json -Depth 8
[IO.File]::WriteAllText((Join-Path $publishRoot 'tree-request.json'), $treeBody, [Text.UTF8Encoding]::new($false))
$treeText = gh api repos/PRU213/KHTN6/git/trees --method POST --input (Join-Path $publishRoot 'tree-request.json')
if ($LASTEXITCODE -ne 0) { throw 'Cannot create candidate Git tree.' }
$tree = $treeText | ConvertFrom-Json
$commitBody = @{message='feat(planet): add post-match remediation and adaptive step hints'; tree=$tree.sha; parents=@($parent)} | ConvertTo-Json -Depth 4
[IO.File]::WriteAllText((Join-Path $publishRoot 'commit-request.json'), $commitBody, [Text.UTF8Encoding]::new($false))
$commitText = gh api repos/PRU213/KHTN6/git/commits --method POST --input (Join-Path $publishRoot 'commit-request.json')
if ($LASTEXITCODE -ne 0) { throw 'Cannot create backend commit.' }
$commit = $commitText | ConvertFrom-Json
$current = gh api repos/PRU213/KHTN6/git/ref/heads/main --jq .object.sha
if ($LASTEXITCODE -ne 0 -or $current.Trim() -ne $parent) { throw 'Main advanced; non-fast-forward update not attempted.' }
$refBody = @{sha=$commit.sha; force=$false} | ConvertTo-Json
[IO.File]::WriteAllText((Join-Path $publishRoot 'ref-request.json'), $refBody, [Text.UTF8Encoding]::new($false))
$updatedText = gh api repos/PRU213/KHTN6/git/refs/heads/main --method PATCH --input (Join-Path $publishRoot 'ref-request.json')
if ($LASTEXITCODE -ne 0) { throw 'Cannot fast-forward main to backend commit.' }
$updated = $updatedText | ConvertFrom-Json
if ($updated.object.sha -ne $commit.sha) { throw 'Unexpected main result.' }
$result = [pscustomobject]@{repository='PRU213/KHTN6'; branch='main'; previous=$parent; sha=$commit.sha; url=$commit.html_url; files=$files; relatedTests='79/79 passed'; secretScan='passed'}
$result | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $publishRoot 'published-commit.json')
$result | Select-Object sha,url,branch,relatedTests,secretScan | ConvertTo-Json -Compress
