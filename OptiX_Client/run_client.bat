@echo off
echo 🚀 OptiX 클라이언트 실행 (.NET Framework 4.8)
echo ==========================================

set EXE_PATH=bin\Debug\net48\OptiXClient.exe

if not exist "%EXE_PATH%" (
    echo ❌ OptiXClient.exe 파일이 없습니다.
    echo 먼저 build.bat을 실행하여 빌드해주세요.
    pause
    exit /b 1
)

echo ✅ OptiX 클라이언트 실행 중...
"%EXE_PATH%"

echo.
echo 👋 클라이언트 종료
pause