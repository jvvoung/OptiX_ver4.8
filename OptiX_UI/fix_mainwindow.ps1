# PowerShell 스크립트로 MainWindow.xaml.cs 파일을 .NET Framework 4.8에 맞게 수정

$content = Get-Content "MainWindow_original.xaml.cs" -Raw

# 1. System using 추가
$content = $content -replace "using System.Text;", "using System;`nusing System.Text;"

# 2. file-scoped namespace를 전통적인 형태로 변경
$content = $content -replace "namespace OptiX;", "namespace OptiX`n{"

# 3. nullable reference types 제거
$content = $content -replace "DispatcherTimer\? ", "DispatcherTimer "
$content = $content -replace "UserControl\? ", "UserControl "
$content = $content -replace "IniFileManager\? ", "IniFileManager "
$content = $content -replace "TextBlock\? ", "TextBlock "

# 4. null-conditional operators 제거
$content = $content -replace "characteristicsTimer\?\.", "if (characteristicsTimer != null) characteristicsTimer."
$content = $content -replace "ipvsTimer\?\.", "if (ipvsTimer != null) ipvsTimer."
$content = $content -replace "tooltip\?\.", "if (tooltip != null) tooltip."

# 5. switch expressions를 전통적인 switch statements로 변경 (이미 수정되어 있을 것임)

# 6. 파일 끝에 네임스페이스 닫는 브레이스 추가
$content = $content + "`n}"

# 7. 파일 저장
$content | Set-Content "MainWindow.xaml.cs" -Encoding UTF8

