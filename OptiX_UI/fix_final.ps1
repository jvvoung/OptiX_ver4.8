# 완전히 새로운 MainWindow.xaml.cs 파일 생성
$originalPath = "D:\OptiX\OptiX_UI\MainWindow.xaml.cs"
$targetPath = "MainWindow.xaml.cs"

# 원본 파일 읽기
$content = Get-Content $originalPath -Raw

# .NET Framework 4.8 호환 변환
$content = $content -replace "using System.Text;", "using System;`nusing System.Text;"
$content = $content -replace "namespace OptiX;", "namespace OptiX`n{"
$content = $content -replace "DispatcherTimer\? ", "DispatcherTimer "
$content = $content -replace "UserControl\? ", "UserControl "
$content = $content -replace "IniFileManager\? ", "IniFileManager "
$content = $content -replace "TextBlock\? ", "TextBlock "

# switch expressions를 전통적인 switch로 변경
$content = $content -replace "resizeDirection = handle\.Name switch\s*\{[^}]+\}", @"
switch (handle.Name)
            {
                case "TopResizeHandle":
                    resizeDirection = "Top";
                    break;
                case "BottomResizeHandle":
                    resizeDirection = "Bottom";
                    break;
                case "LeftResizeHandle":
                    resizeDirection = "Left";
                    break;
                case "RightResizeHandle":
                    resizeDirection = "Right";
                    break;
                case "TopLeftResizeHandle":
                    resizeDirection = "TopLeft";
                    break;
                case "TopRightResizeHandle":
                    resizeDirection = "TopRight";
                    break;
                case "BottomLeftResizeHandle":
                    resizeDirection = "BottomLeft";
                    break;
                case "BottomRightResizeHandle":
                    resizeDirection = "BottomRight";
                    break;
                default:
                    resizeDirection = "";
                    break;
            }
"@

$content = $content -replace "this\.Cursor = resizeDirection switch\s*\{[^}]+\}", @"
switch (resizeDirection)
            {
                case "Top":
                case "Bottom":
                    this.Cursor = Cursors.SizeNS;
                    break;
                case "Left":
                case "Right":
                    this.Cursor = Cursors.SizeWE;
                    break;
                case "TopLeft":
                case "BottomRight":
                    this.Cursor = Cursors.SizeNWSE;
                    break;
                case "TopRight":
                case "BottomLeft":
                    this.Cursor = Cursors.SizeNESW;
                    break;
                default:
                    this.Cursor = Cursors.Arrow;
                    break;
            }
"@

# 파일 끝에 네임스페이스 닫는 브레이스 추가
if (-not $content.EndsWith("}")) {
    $content = $content.TrimEnd() + "`n}"
}

# 파일 저장 (UTF8, BOM 없음)
[System.IO.File]::WriteAllText($targetPath, $content, [System.Text.UTF8Encoding]::new($false))

