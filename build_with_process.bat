@echo off
echo ========================================
echo OptiX + Process MFC DLL 빌드 및 배포
echo ========================================

echo.
echo 1. 기존 파일 정리 중...
if exist "publish" rmdir /s /q "publish"
mkdir "publish"

echo.
echo 2. Process MFC DLL 프로젝트 빌드 중...
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" x64
msbuild Process\Process.vcxproj /p:Configuration=Release /p:Platform=x64

if %ERRORLEVEL% neq 0 (
    echo 오류: Process MFC DLL 빌드 실패
    pause
    exit /b 1
)

echo.
echo 3. WPF 프로젝트 빌드 중...
dotnet build OptiX_UI/OptiX_UI.csproj --configuration Release

if %ERRORLEVEL% neq 0 (
    echo 오류: WPF 프로젝트 빌드 실패
    pause
    exit /b 1
)

echo.
echo 4. Process MFC DLL을 publish 폴더로 복사 중...
copy "x64\Release\Process.dll" "publish\"
copy "x64\Release\Process.lib" "publish\"
copy "x64\Release\Process.pdb" "publish\"

echo.
echo 5. MFC 의존 DLL 복사 (필요시)...
rem MFC140.dll, msvcp140.dll, vcruntime140.dll 등이 필요할 수 있습니다
rem copy "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Redist\MSVC\14.xx\x64\Microsoft.VC143.MFC\mfc140u.dll" "publish\"

echo.
echo 6. INI 파일 복사 중...
copy "OptiX.ini" "publish\"

echo.
echo ========================================
echo Release 빌드 완료!
echo 배포 파일 위치: D:\OptiX_48\publish\
echo 주요 DLL: Process.dll (MFC 공유 DLL)
echo ========================================
pause

