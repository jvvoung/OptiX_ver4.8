@echo off
echo ========================================
echo OptiX Debug 빌드 및 배포
echo ========================================

echo.
echo 1. 기존 파일 정리 중...
if exist "publish_debug" rmdir /s /q "publish_debug"
mkdir "publish_debug"

echo.
echo 2. WPF 프로젝트 빌드 중...
dotnet build OptiX_UI/OptiX_UI.csproj --configuration Debug

if %ERRORLEVEL% neq 0 (
    echo 오류: WPF 프로젝트 빌드 실패
    pause
    exit /b 1
)

echo.
echo 3. C++ DLL 프로젝트 빌드 중...
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" x64
msbuild TestDll\TestDll.vcxproj /p:Configuration=Debug /p:Platform=x64

if %ERRORLEVEL% neq 0 (
    echo 오류: C++ DLL 프로젝트 실패
    pause
    exit /b 1
)

echo.
echo 4. WPF 실행 파일을 publish_debug 폴더로 복사 중...
copy "OptiX_UI\bin\x64\Debug\OptiX.exe" "publish_debug\"
copy "OptiX_UI\bin\x64\Debug\OptiX.pdb" "publish_debug\"
copy "OptiX_UI\bin\x64\Debug\OptiX.exe.config" "publish_debug\"

echo.
echo 5. C++ DLL을 publish_debug 폴더로 복사 중...
copy "TestDll\x64\Debug\TestDll.dll" "publish_debug\"
copy "TestDll\x64\Debug\TestDll.lib" "publish_debug\"
copy "TestDll\x64\Debug\TestDll.pdb" "publish_debug\"

echo.
echo 6. INI 파일 복사 중...
copy "OptiX.ini" "publish_debug\"

echo.
echo ========================================
echo Debug 빌드 완료!
echo 배포 파일 위치: D:\OptiX\publish_debug\
echo 실행 파일: OptiX.exe
echo ========================================
pause
