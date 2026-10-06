$path = "d:\University\7thSemester\KHTN6\Assets\Scenes\Biology\Bean_3_Biology.unity"
$lines = Get-Content $path
for ($i=0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match "--- \!u\!114 &1912172058") {
        for ($j=$i; $j -lt $i+15; $j++) {
            if ($lines[$j] -match "m_Enabled: 1") {
                $lines[$j] = $lines[$j] -replace "m_Enabled: 1", "m_Enabled: 0"
                break
            }
        }
        break
    }
}
Set-Content -Path $path -Value $lines
