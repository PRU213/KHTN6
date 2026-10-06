$path = "d:\University\7thSemester\KHTN6\Assets\Scripts\XepHangManager.cs"
$content = Get-Content $path -Raw
$content = $content -replace 'monID\.Trim\(\)\.Equals\(.*?\)', 'monID.Trim().Contains("Sinh")'
Set-Content -Path $path -Value $content -Encoding UTF8
