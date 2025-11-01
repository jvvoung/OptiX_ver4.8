@echo off
echo 🚀 OptiX 클라이언트 빌드 시작 (.NET Framework 4.8)
echo ==========================================

REM .NET Framework 4.8 확인
reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ .NET Framework 4.8이 설치되지 않았습니다.
    echo .NET Framework 4.8을 설치해주세요.
    echo https://dotnet.microsoft.com/download/dotnet-framework/net48
    pause
    exit /b 1
)

echo ✅ .NET Framework 4.8 확인됨

REM 빌드 실행
echo 🔨 클라이언트 빌드 중...
dotnet build OptiXClient.csproj --configuration Release

if %errorlevel% neq 0 (
    echo ❌ 빌드 실패
    pause
    exit /b 1
)

echo ✅ 빌드 완료!
echo 📁 실행 파일: bin\Release\net48\OptiXClient.exe
echo.
echo 🚀 클라이언트 실행하려면: run_client.bat
pause

