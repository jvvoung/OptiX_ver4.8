@echo off
echo 🚀 OptiX 클라이언트 실행
echo ==========================================

if not exist "OptiXClient.exe" (
    echo ❌ OptiXClient.exe 파일이 없습니다.
    echo 먼저 build.bat을 실행하여 빌드해주세요.
    pause
    exit /b 1
)

echo ✅ OptiX 클라이언트 실행 중...
OptiXClient.exe

echo.
echo 👋 클라이언트 종료
pause