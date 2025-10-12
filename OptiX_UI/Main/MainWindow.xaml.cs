using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;
using System.Threading.Tasks;
using OptiX.Main;
using OptiX.Communication;
using OptiX.Common;

namespace OptiX
{

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private bool isDarkMode = false;
    public bool IsDarkMode => isDarkMode; // 외부에서 읽기 전용으로 접근 가능
    
    private CommunicationServer communicationServer;
    public event EventHandler<bool> CommunicationServerStatusChanged;
    
    // Manager 인스턴스들
    private PageNavigationManager pageNavigationManager;
    private TooltipManager tooltipManager;
    private WindowResizeManager windowResizeManager;

    public MainWindow()
    {
        InitializeComponent();
        LoadSettingsFromIni();
        InitializeCommunicationServer();
        
        // Manager 초기화
        pageNavigationManager = new PageNavigationManager(this);
        tooltipManager = new TooltipManager(this);
        windowResizeManager = new WindowResizeManager(this);
        
        // 언어 변경 이벤트 구독
        LanguageManager.LanguageChanged += OnLanguageChanged;
        
        // 초기 언어 적용
        ApplyLanguage();
    }

    private void InitializeCommunicationServer()
    {
        // CommunicationServer 초기화
        communicationServer = new CommunicationServer();
        communicationServer.LogMessage += OnCommunicationLogMessage;
        communicationServer.MessageReceived += OnCommunicationMessageReceived;
        communicationServer.ConnectionStatusChanged += OnCommunicationStatusChanged;
        
        // INI에서 서버 설정 로드
        string tcpIp = GlobalDataManager.GetValue("Settings", "TCP_IP", "127.0.0.1");
        string tcpPort = GlobalDataManager.GetValue("Settings", "TCP_PORT", "7777");
        
        // 서버 자동 시작 (설정에 따라)
        if (bool.TryParse(GlobalDataManager.GetValue("Settings", "AUTO_START_SERVER", "false"), out bool autoStart) && autoStart)
        {
            Task.Run(async () =>
            {
                await Task.Delay(1000); // UI 초기화 후 서버 시작
                if (int.TryParse(tcpPort, out int port))
                {
                    await communicationServer.StartServerAsync(tcpIp, port);
                }
            });
        }
    }

    private void OnCommunicationLogMessage(object sender, string message)
    {
        // 로그 메시지를 디버그 출력으로 표시
        System.Diagnostics.Debug.WriteLine($"[CommunicationServer] {message}");
    }

    private void OnCommunicationMessageReceived(object sender, string message)
    {
        // 클라이언트로부터 받은 메시지 처리
        System.Diagnostics.Debug.WriteLine($"[CommunicationServer] 메시지 수신: {message}");
        
        // 메시지에 따른 동작 처리
        ProcessClientMessage(message);
    }

    private void OnCommunicationStatusChanged(object sender, bool isConnected)
    {
        // 연결 상태 변경 처리
        System.Diagnostics.Debug.WriteLine($"[CommunicationServer] 연결 상태 변경: {isConnected}");
        
        // UI 스레드에서 실행
        Dispatcher.Invoke(() =>
        {
            // AUTO MODE 표시 업데이트
            UpdateAutoModeDisplay(isConnected);
            
            // CommunicationServerStatusChanged 이벤트 발생 (MainSettingsWindow에서 구독)
            CommunicationServerStatusChanged?.Invoke(this, isConnected);
            
            if (isConnected)
            {
                CommunicationLogger.WriteLog($"🟢 [CONNECTION_STATUS] 클라이언트 연결됨 - AUTO MODE 활성화");
            }
            else
            {
                CommunicationLogger.WriteLog($"🔴 [CONNECTION_STATUS] 클라이언트 연결 해제됨 - AUTO MODE 해제");
            }
        });
    }

    private void ProcessClientMessage(string message)
    {
        switch (message.ToUpper())
        {
            case "TEST_START":
                // 테스트 시작 명령 처리
                Dispatcher.Invoke(() =>
                {
                    // 팝업 제거 - 로그로만 기록
                    // MessageBox.Show("클라이언트로부터 TEST_START 명령을 받았습니다!", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                });
                break;
            case "TEST_STOP":
                // 테스트 중지 명령 처리
                Dispatcher.Invoke(() =>
                {
                    // 팝업 제거 - 로그로만 기록
                    // MessageBox.Show("클라이언트로부터 TEST_STOP 명령을 받았습니다!", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                });
                break;
            default:
                System.Diagnostics.Debug.WriteLine($"알 수 없는 명령: {message}");
                break;
        }
    }

    public async Task<bool> StartCommunicationServer(string tcpIp, int port)
    {
        if (communicationServer != null)
        {
            bool success = await communicationServer.StartServerAsync(tcpIp, port);
            if (success)
            {
                // 서버 시작 성공 - 클라이언트 연결 상태는 OnCommunicationStatusChanged에서 처리
                CommunicationLogger.WriteLog($"🟢 [SERVER_CONNECT] 서버 연결 성공 - IP: {tcpIp}, Port: {port}");
                CommunicationLogger.WriteLog($"✅ [CONNECT_COMPLETE] CONNECT 완료");
            }
            return success;
        }
        return false;
    }

    public async Task StopCommunicationServer()
    {
        if (communicationServer != null)
        {
            await communicationServer.StopServerAsync();
            CommunicationServerStatusChanged?.Invoke(this, false);
            UpdateAutoModeDisplay(false);
            CommunicationLogger.WriteLog($"🔴 [SERVER_DISCONNECT] 서버 연결 해제 - 사유: 사용자 요청");
        }
    }

    public bool IsCommunicationServerRunning()
    {
        return communicationServer?.IsRunning ?? false;
    }

    public bool HasConnectedClients()
    {
        return communicationServer?.ConnectedClientCount > 0;
    }

    private void UpdateAutoModeDisplay(bool isConnected)
    {
        if (AutoModeText != null)
        {
            if (isConnected)
            {
                AutoModeText.Text = "(AUTO MODE)";
                AutoModeText.Visibility = Visibility.Visible;
            }
            else
            {
                AutoModeText.Text = "";
                AutoModeText.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void CharacteristicsButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Optic 버튼 클릭됨");
        pageNavigationManager?.ShowOpticPage();
    }

    private void IPVSButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("IPVS 버튼 클릭됨");
        pageNavigationManager?.ShowIPVSPage();
    }

    private void ManualButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Manual 버튼 클릭됨");
        pageNavigationManager?.ShowManualPage();
    }

    private void LUTButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("LUT 버튼 클릭됨");
            pageNavigationManager?.ShowLUTPage();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LUT 페이지 전환 오류: {ex.Message}");
            MessageBox.Show($"LUT 페이지를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("설정 버튼 클릭됨");
        
        try
        {
            var settingsWindow = new MainSettingsWindow(isDarkMode);
            settingsWindow.Owner = this;
            settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Non-Modal로 열기 (메인 프로그램 계속 동작)
            settingsWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"설정 창을 여는 중 오류가 발생했습니다: {ex.Message}", "오류", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        windowResizeManager?.ToggleMaximize();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void LightModeButton_Click(object sender, RoutedEventArgs e)
    {
        isDarkMode = false;
        ThemeManager.SetMainWindowLightMode(this, pageNavigationManager);
        tooltipManager?.SetDarkMode(false);
    }

    private void DarkModeButton_Click(object sender, RoutedEventArgs e)
    {
        isDarkMode = true;
        ThemeManager.SetMainWindowDarkMode(this, pageNavigationManager);
        tooltipManager?.SetDarkMode(true);
    }

    #region 툴팁 이벤트 핸들러 (TooltipManager로 위임)

    private void CharacteristicsButton_MouseEnter(object sender, MouseEventArgs e)
    {
        tooltipManager?.OnCharacteristicsButtonMouseEnter(sender, e);
    }

    private void CharacteristicsButton_MouseLeave(object sender, MouseEventArgs e)
    {
        tooltipManager?.OnCharacteristicsButtonMouseLeave(sender, e);
    }

    private void IPVSButton_MouseEnter(object sender, MouseEventArgs e)
    {
        tooltipManager?.OnIPVSButtonMouseEnter(sender, e);
    }

    private void IPVSButton_MouseLeave(object sender, MouseEventArgs e)
    {
        tooltipManager?.OnIPVSButtonMouseLeave(sender, e);
    }

    private void ManualButton_MouseEnter(object sender, MouseEventArgs e)
    {
        tooltipManager?.OnManualButtonMouseEnter(sender, e);
    }

    private void ManualButton_MouseLeave(object sender, MouseEventArgs e)
    {
        tooltipManager?.OnManualButtonMouseLeave(sender, e);
    }

    private void LUTButton_MouseEnter(object sender, MouseEventArgs e)
    {
        tooltipManager?.OnLUTButtonMouseEnter(sender, e);
    }

    private void LUTButton_MouseLeave(object sender, MouseEventArgs e)
    {
        tooltipManager?.OnLUTButtonMouseLeave(sender, e);
    }

    private void SettingsButton_MouseEnter(object sender, MouseEventArgs e)
    {
        tooltipManager?.OnSettingsButtonMouseEnter(sender, e);
    }

    private void SettingsButton_MouseLeave(object sender, MouseEventArgs e)
    {
        tooltipManager?.OnSettingsButtonMouseLeave(sender, e);
    }

    #endregion




    public void ShowMainPage()
    {
        pageNavigationManager?.ShowMainPage();
    }

    #region 창 크기 조정 이벤트 핸들러 (WindowResizeManager로 위임)

    private void ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        windowResizeManager?.OnResizeHandleMouseLeftButtonDown(sender, e);
    }

    private void ResizeHandle_MouseMove(object sender, MouseEventArgs e)
    {
        windowResizeManager?.OnResizeHandleMouseMove(sender, e);
    }

    private void ResizeHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        windowResizeManager?.OnResizeHandleMouseLeftButtonUp(sender, e);
    }

    #endregion

    private void LoadSettingsFromIni()
    {
        try
        {
            // 창 크기 및 위치 설정 제거됨 - 하드코딩된 기본값 사용

            // 테마 설정 로드
            string isDarkModeStr = GlobalDataManager.GetValue("Theme", "IsDarkMode", "False");
            if (bool.TryParse(isDarkModeStr, out bool darkMode) && darkMode)
            {
                isDarkMode = true;
                ThemeManager.SetMainWindowDarkMode(this, pageNavigationManager);
                tooltipManager?.SetDarkMode(true);
            }

            // 언어 설정 로드
            string currentLanguage = GlobalDataManager.GetValue("Settings", "Language", "Korean");
            LanguageManager.SetLanguage(currentLanguage);

            // 타이틀바 색상 설정 제거됨 - XAML에서 하드코딩된 보라색 사용

            System.Diagnostics.Debug.WriteLine("INI 설정이 성공적으로 로드되었습니다.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"INI 설정 로드 오류: {ex.Message}");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        // 언어 변경 이벤트 구독 해제
        LanguageManager.LanguageChanged -= OnLanguageChanged;
        
        // CommunicationServer 정리
        if (communicationServer != null)
        {
            try
            {
                communicationServer.StopServerAsync().Wait(3000);
                communicationServer.SendMessageToAllClientsAsync("SERVER_SHUTDOWN").Wait(1000);
                communicationServer.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"서버 정리 중 오류: {ex.Message}");
            }
        }
        
        base.OnClosed(e);
    }

    /// <summary>
    /// 언어 변경 이벤트 핸들러
    /// </summary>
    private void OnLanguageChanged(object sender, EventArgs e)
    {
        ApplyLanguage();
        
        // 현재 페이지에도 언어 적용
        pageNavigationManager?.ApplyLanguageToCurrentPage();
    }

    /// <summary>
    /// MainWindow에 언어 적용 (LanguageHelper로 위임)
    /// </summary>
    public void ApplyLanguage()
    {
        LanguageHelper.ApplyToMainWindow(this);
    }

    /// <summary>
    /// 메인 콘텐츠 영역을 표시 (페이지에서 뒤로가기 시 사용)
    /// </summary>
    public void ShowMainContent()
    {
        pageNavigationManager?.ShowMainPage();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // 더블클릭 시 최대화/복원 토글
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }
        else
        {
            // 단일 클릭 시 창 드래그
            DragMove();
        }
    }
}
}
