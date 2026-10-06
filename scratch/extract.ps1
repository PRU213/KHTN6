$sourceFile = "d:\University\7thSemester\KHTN6\Assets\Scenes\Biology\bean_1.unity"
$targetFiles = @(
    "d:\University\7thSemester\KHTN6\Assets\Scenes\Biology\Bean_2_Biology.unity",
    "d:\University\7thSemester\KHTN6\Assets\Scenes\Biology\Bean_3_Biology.unity",
    "d:\University\7thSemester\KHTN6\Assets\Scenes\Biology\bean_tong.unity"
)

# Read source scene
$content = Get-Content -Path $sourceFile -Raw
# Split by blocks
$blocks = [regex]::Split($content, '(?m)^(?=--- !u!)')

# Find btnTamDung GameObject block
$btnGoBlock = $null
foreach ($block in $blocks) {
    if ($block -match "^--- !u!1 " -and $block -match "m_Name: btnTamDung") {
        $btnGoBlock = $block
        break
    }
}

if (-not $btnGoBlock) {
    Write-Host "btnTamDung not found!"
    exit 1
}

$btnGoId = [regex]::Match($btnGoBlock, '^--- !u!1 &(\d+)').Groups[1].Value
Write-Host "GameObject ID: $btnGoId"

$compIds = @()
$compMatches = [regex]::Matches($btnGoBlock, '- component: \{fileID: (\d+)\}')
foreach ($match in $compMatches) {
    $compIds += $match.Groups[1].Value
}
Write-Host "Component IDs: $($compIds -join ', ')"

$blocksToCopy = @($btnGoBlock)
foreach ($block in $blocks) {
    $fidMatch = [regex]::Match($block, '^--- !u!\d+ &(\d+)')
    if ($fidMatch.Success) {
        $fid = $fidMatch.Groups[1].Value
        if ($compIds -contains $fid) {
            $blocksToCopy += $block
        }
    }
}

Write-Host "Total blocks to copy: $($blocksToCopy.Count)"

function Generate-Id {
    return (Get-Random -Minimum 1000000000 -Maximum 2000000000).ToString()
}

foreach ($target in $targetFiles) {
    Write-Host "Processing $target"
    
    $targetContent = Get-Content -Path $target -Raw
    $targetBlocks = [regex]::Split($targetContent, '(?m)^(?=--- !u!)')
    
    # Generate new IDs for this target
    $idMap = @{}
    $idMap[$btnGoId] = Generate-Id
    foreach ($c in $compIds) {
        $idMap[$c] = Generate-Id
    }
    
    # 1. Update the copied blocks with new IDs
    $newBlocks = @()
    $newRectId = $null
    foreach ($block in $blocksToCopy) {
        $newBlock = $block
        
        # Replace block definition ID
        $oldId = [regex]::Match($newBlock, '^--- !u!\d+ &(\d+)').Groups[1].Value
        if ($idMap.ContainsKey($oldId)) {
            $newId = $idMap[$oldId]
            $newBlock = $newBlock -replace "^(--- !u!\d+ &)$oldId", "`${1}$newId"
        }
        
        # Replace references inside block
        foreach ($key in $idMap.Keys) {
            $val = $idMap[$key]
            $newBlock = $newBlock -replace "\{fileID: $key\}", "{fileID: $val}"
        }
        
        if ($newBlock -match "--- !u!224 ") {
            $newRectId = $idMap[$oldId]
        }
        
        $newBlocks += $newBlock
    }
    
    # 2. Find Canvas in target
    $canvasGoId = $null
    foreach ($tblock in $targetBlocks) {
        if ($tblock -match "^--- !u!1 " -and $tblock -match "m_Name: Canvas") {
            $canvasGoId = [regex]::Match($tblock, '^--- !u!1 &(\d+)').Groups[1].Value
            break
        }
    }
    
    if (-not $canvasGoId) {
        Write-Host "Could not find Canvas in $target"
        continue
    }
    
    # 3. Find Canvas RectTransform
    $canvasRectId = $null
    $canvasRectBlockIdx = -1
    for ($i = 0; $i -lt $targetBlocks.Count; $i++) {
        $tblock = $targetBlocks[$i]
        if ($tblock -match "^--- !u!224 " -and $tblock -match "m_GameObject: \{fileID: $canvasGoId\}") {
            $canvasRectId = [regex]::Match($tblock, '^--- !u!224 &(\d+)').Groups[1].Value
            $canvasRectBlockIdx = $i
            break
        }
    }
    
    if (-not $canvasRectId) {
        Write-Host "Could not find Canvas RectTransform in $target"
        continue
    }
    
    # 4. Modify Canvas RectTransform to include our new rect ID in m_Children
    $rectBlock = $targetBlocks[$canvasRectBlockIdx]
    
    # Place it at the END of m_Children so it renders on top
    if ($rectBlock -match "m_Children: \[\]") {
        $rectBlock = $rectBlock -replace "m_Children: \[\]", "m_Children:`n  - {fileID: $newRectId}"
    } else {
        $rectBlock = $rectBlock -replace "(?m)^  m_Father:", "  - {fileID: $newRectId}`n  m_Father:"
    }
    $targetBlocks[$canvasRectBlockIdx] = $rectBlock
    
    # 5. Set m_Father of our new RectTransform to canvasRectId
    for ($i = 0; $i -lt $newBlocks.Count; $i++) {
        if ($newBlocks[$i] -match "--- !u!224 ") {
            $newBlocks[$i] = $newBlocks[$i] -replace "m_Father: \{fileID: \d+\}", "m_Father: {fileID: $canvasRectId}"
        }
    }
    
    # 6. Reassemble the target content and append new blocks
    $finalContent = $targetBlocks -join ""
    $finalContent += $newBlocks -join ""
    
    Set-Content -Path $target -Value $finalContent -NoNewline
    Write-Host "Successfully updated $target"
}
