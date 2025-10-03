using System;
using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;

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
                // 전역 데이터 매니저 초기화 - INI 파일 경로 고정
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
                // 초기화 실패해도 프로그램은 계속 실행
            }
            
            base.OnStartup(e);
        }
    }
}

