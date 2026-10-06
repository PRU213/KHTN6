$path = "d:\University\7thSemester\KHTN6\Assets\Scripts\TimerDisplay.cs"
$content = Get-Content $path -Raw
$content = $content -replace 'timerText\.alignment = TextAnchor\.UpperCenter;', 'timerText.alignment = TextAnchor.UpperLeft;'
$content = $content -replace 'new Vector2\(400, 100\);', 'new Vector2(130, 100);'
Set-Content -Path $path -Value $content -Encoding UTF8
