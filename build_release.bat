@echo off
echo ========================================
echo OptiX Release 빌드 및 배포
echo ========================================

echo.
echo 1. 기존 파일 정리 중...
if exist "publish" rmdir /s /q "publish"
mkdir "publish"

echo.
echo 2. WPF 프로젝트 빌드 중...
dotnet build OptiX_UI/OptiX_UI.csproj --configuration Release

if %ERRORLEVEL% neq 0 (
    echo 오류: WPF 프로젝트 빌드 실패
    pause
    exit /b 1
)

echo.
echo 3. C++ DLL 프로젝트 빌드 중...
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" x64
msbuild TestDll\TestDll.vcxproj /p:Configuration=Release /p:Platform=x64

if %ERRORLEVEL% neq 0 (
    echo 오류: C++ DLL 프로젝트 빌드 실패
    pause
    exit /b 1
)

echo.
echo 4. C++ DLL을 publish 폴더로 복사 중...
copy "TestDll\x64\Release\TestDll.dll" "publish\"
copy "TestDll\x64\Release\TestDll.lib" "publish\"
copy "TestDll\x64\Release\TestDll.pdb" "publish\"

echo.
echo 5. INI 파일 복사 중...
copy "OptiX.ini" "publish\"

echo.
echo ========================================
echo Release 빌드 완료!
echo 배포 파일 위치: D:\OptiX\publish\
echo 실행 파일: OptiX.exe
echo ========================================
pause
