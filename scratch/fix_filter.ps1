$path = "d:\University\7thSemester\KHTN6\Assets\Scripts\ChonBaiManager.cs"
$content = Get-Content $path -Raw
$content = $content -replace 'q\.monId\.Trim\(\)\.Equals\(.*?\)', 'q.monId.Trim().Contains("Sinh")'
$content = $content -replace 'public string monHocFilter = ".*";', 'public string monHocFilter = "Sinh";'
Set-Content -Path $path -Value $content -Encoding UTF8
