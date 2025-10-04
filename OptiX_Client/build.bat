@echo off
echo 🚀 OptiX 클라이언트 빌드 시작
echo ==========================================

REM .NET 설치 확인
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ .NET 6.0이 설치되지 않았습니다.
    echo .NET 6.0 Runtime을 설치해주세요.
    echo https://dotnet.microsoft.com/download/dotnet/6.0
    pause
    exit /b 1
)

echo ✅ .NET 6.0 확인됨

REM 빌드 실행
echo 🔨 클라이언트 빌드 중...
dotnet build --configuration Release

if %errorlevel% neq 0 (
    echo ❌ 빌드 실패
    pause
    exit /b 1
)

REM 실행 파일 복사
echo 📦 실행 파일 생성 중...
copy "bin\Release\net6.0-windows\OptiXClient.exe" "OptiXClient.exe" /Y

echo ✅ 빌드 완료!
echo 📁 실행 파일: OptiXClient.exe
echo.
echo 🚀 클라이언트 실행하려면: OptiXClient.exe
pause

