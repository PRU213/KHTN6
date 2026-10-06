$content = Get-Content 'd:\University\7thSemester\KHTN6\ProjectSettings\EditorBuildSettings.asset' -Raw
$newScenes = @"
  - enabled: 1
    path: Assets/Scenes/Biology/TamDung.unity
    guid: 2bccdf71310b869499be502fdd37f161
  - enabled: 1
    path: Assets/Scenes/Biology/XacNhan.unity
    guid: be35d64ac630b904192bfd4f8f9f6189
  m_configObjects:
"@

$content = $content -replace '  m_configObjects:', $newScenes
Set-Content 'd:\University\7thSemester\KHTN6\ProjectSettings\EditorBuildSettings.asset' $content -NoNewline
