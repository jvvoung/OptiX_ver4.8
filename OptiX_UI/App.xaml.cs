using System;
using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;
using OptiX.DLL;
using OptiX.Common;

namespace OptiX
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // ErrorLogger 초기화 (가장 먼저 초기화하여 이후 모든 오류 캡처)
                ErrorLogger.Initialize();
                System.Diagnostics.Debug.WriteLine("ErrorLogger 초기화 완료");
                
                // 전역 데이터 매니저 초기화 - INI 파일 경로 고정 (설비 표준 경로)
                string iniPath = @"D:\Project\Recipe\OptiX.ini";
                
                GlobalDataManager.Initialize(iniPath);
                System.Diagnostics.Debug.WriteLine($"GlobalDataManager 초기화 완료: {iniPath}");
                
                // DLL 매니저 초기화
                bool dllInitialized = DllManager.Initialize();
                System.Diagnostics.Debug.WriteLine($"DllManager 초기화: {(dllInitialized ? "성공" : "실패")}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"초기화 실패: {ex.Message}");
                ErrorLogger.LogException(ex, "애플리케이션 초기화 중 치명적 오류");
                // 초기화 실패해도 프로그램은 계속 실행
            }
            
            base.OnStartup(e);
        }

    //25.10.30 - App 종료 시 로거들 정리 (남은 로그 플러시)
    //25.02.08 - DLL 리소스 해제 추가
    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("========== Application 종료 프로세스 시작 ==========");
            
            // ===== 1. DLL 리소스 해제 (MainWindow.OnClosed에서도 호출되지만 안전을 위해 중복 호출) =====
            try
            {
                System.Diagnostics.Debug.WriteLine("[App.OnExit] DLL 리소스 해제 시작...");
                DllManager.Dispose();
                System.Diagnostics.Debug.WriteLine("[App.OnExit] DLL 리소스 해제 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App.OnExit] DLL 리소스 해제 실패: {ex.Message}");
            }
            
            // ===== 2. MonitorLogService 종료 =====
            try
            {
                System.Diagnostics.Debug.WriteLine("[App.OnExit] MonitorLogService 종료 시작...");
                MonitorLogService.Instance.Dispose();
                System.Diagnostics.Debug.WriteLine("[App.OnExit] MonitorLogService 종료 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App.OnExit] MonitorLogService 종료 실패: {ex.Message}");
            }
            
            // ===== 3. ErrorLogger 종료 (남은 로그를 파일에 플러시) =====
            try
            {
                System.Diagnostics.Debug.WriteLine("[App.OnExit] ErrorLogger 종료 시작...");
                ErrorLogger.Dispose();
                System.Diagnostics.Debug.WriteLine("[App.OnExit] ErrorLogger 종료 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App.OnExit] ErrorLogger 종료 실패: {ex.Message}");
            }
            
            System.Diagnostics.Debug.WriteLine("========== Application 종료 프로세스 완료 ==========");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App.OnExit] 종료 프로세스 중 예외 발생: {ex.Message}");
        }
        finally
        {
            // 짧은 대기 (로그 플러시 시간 확보)
            System.Threading.Thread.Sleep(100);
            base.OnExit(e);
        }
    }
    }
}

