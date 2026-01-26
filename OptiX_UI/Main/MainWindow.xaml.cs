using System;
using System.Collections.Generic;
using System.Linq;
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
using OptiX.DLL;

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
        
        // PageNavigationManager에 다크모드 상태 전달
        pageNavigationManager.SetDarkMode(isDarkMode);
        
        // 언어 변경 이벤트 구독
        LanguageManager.LanguageChanged += OnLanguageChanged;
        
        // 초기 언어 적용
        ApplyLanguage();
        
        // 자동 연결 기능 초기화 (UI 로드 후 실행)
        Loaded += (s, e) => InitializeAutoConnections();
    }

    private void InitializeCommunicationServer()
    {
        // CommunicationServer 초기화
        communicationServer = new CommunicationServer();
        communicationServer.LogMessage += OnCommunicationLogMessage;
        communicationServer.MessageReceived += OnCommunicationMessageReceived;
        communicationServer.ConnectionStatusChanged += OnCommunicationStatusChanged;
        communicationServer.OpticTestStartRequested += OnOpticTestStartRequested;
        communicationServer.IpvsTestStartRequested += OnIpvsTestStartRequested;
        communicationServer.OpticRestartRequested += OnOpticRestartRequested; //25.12.08 - RESTART 이벤트 구독
        
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
        // 구조체 메시지 타입은 CommunicationServer에서 이미 처리됨
        if (message == "OPTIC_START" || message == "IPVS_START")
        {
            // 구조체 메시지는 이미 처리되었으므로 여기서는 무시
            return;
        }

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

        //25.11.02 - OPTIC 자동 모드: 클라이언트에서 받은 Zone 데이터 수집 및 테스트 시작
        // 클라이언트가 2개의 구조체(Zone 1, Zone 2)를 전송하면 200ms 대기 후 한 번에 테스트 시작
        private Dictionary<int, (string cellID, string innerID)> pendingOpticZones = new Dictionary<int, (string, string)>();
        private System.Threading.Timer opticTestTimer;

        /// <summary>
        /// OPTIC 테스트 시작 요청 처리
        /// </summary>
        private void OnOpticTestStartRequested(object sender, OpticStartEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[MainWindow] OPTIC 구조체 수신 - Zone: {e.ZoneSelect}, CellID: {e.CellID}, InnerID: {e.InnerID}");
        CommunicationLogger.WriteLog($"📦 [OPTIC_DATA] Zone {e.ZoneSelect} 데이터 수신 - Cell: {e.CellID}, Inner: {e.InnerID}");

        // Zone 데이터 저장
        pendingOpticZones[e.ZoneSelect] = (e.CellID, e.InnerID);

        // 기존 타이머 취소
        opticTestTimer?.Dispose();

        // 200ms 후에 테스트 시작 (모든 Zone 데이터 수신 대기)
        opticTestTimer = new System.Threading.Timer(_ =>
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                StartOpticTestWithAllZones();
            }));
        }, null, 200, System.Threading.Timeout.Infinite);
    }

    /// <summary>
    /// 모든 Zone 데이터를 설정하고 테스트 시작
    /// </summary>
    private void StartOpticTestWithAllZones()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] ========== StartOpticTestWithAllZones 호출됨 ==========");
            System.Diagnostics.Debug.WriteLine($"[MainWindow] pendingOpticZones.Count = {pendingOpticZones.Count}");
            
            if (pendingOpticZones.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] ❌ Zone 데이터가 없습니다");
                return;
            }

            // 수집된 Zone 데이터 출력
            foreach (var kvp in pendingOpticZones)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Zone {kvp.Key}: CellID={kvp.Value.cellID}, InnerID={kvp.Value.innerID}");
            }

            System.Diagnostics.Debug.WriteLine($"[MainWindow] OPTIC 테스트 시작 - 총 {pendingOpticZones.Count}개 Zone");
            CommunicationLogger.WriteLog($"🚀 [AUTO_TEST_START] OPTIC 테스트 시작 - {pendingOpticZones.Count}개 Zone");

            // 1. OPTIC 페이지로 이동
            System.Diagnostics.Debug.WriteLine($"[MainWindow] OPTIC 페이지로 이동 중...");
            pageNavigationManager?.NavigateToOpticPage();

            // 2. 페이지 로드 완료 후 테스트 시작
            Dispatcher.BeginInvoke(new Action(() =>
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Dispatcher.BeginInvoke 실행됨");
                
                var opticPage = pageNavigationManager?.GetCurrentPage() as OpticPage;
                System.Diagnostics.Debug.WriteLine($"[MainWindow] opticPage = {(opticPage != null ? "찾음" : "null")}");
                
                if (opticPage != null)
                {
                    // 모든 Zone 데이터를 ViewModel에 설정
                    var viewModel = opticPage.DataContext as OptiX.OPTIC.OpticPageViewModel;
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] viewModel = {(viewModel != null ? "찾음" : "null")}");
                    
                    if (viewModel != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] Zone 데이터 설정 시작...");
                        
                        // 자동 모드 플래그 설정
                        viewModel.SetAutoMode(true);
                        
                        foreach (var kvp in pendingOpticZones)
                        {
                            int zoneNumber = kvp.Key;
                            string cellID = kvp.Value.cellID;
                            string innerID = kvp.Value.innerID;

                            System.Diagnostics.Debug.WriteLine($"[MainWindow] SetExternalInputData 호출: Zone={zoneNumber}, Cell={cellID}, Inner={innerID}");
                            viewModel.SetExternalInputData(zoneNumber, cellID, innerID);
                            System.Diagnostics.Debug.WriteLine($"[MainWindow] Zone {zoneNumber} 데이터 설정 완료");
                        }

                        // 테스트 시작 (한 번만!)
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] TestStartCommand.Execute 호출...");
                        viewModel.TestStartCommand.Execute(null);
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] 테스트 시작 명령 실행 완료");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] ❌ ViewModel을 찾을 수 없습니다");
                        CommunicationLogger.WriteLog($"❌ [AUTO_TEST_ERROR] ViewModel을 찾을 수 없습니다");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] ❌ OPTIC 페이지를 찾을 수 없습니다");
                    CommunicationLogger.WriteLog($"❌ [AUTO_TEST_ERROR] OPTIC 페이지를 찾을 수 없습니다");
                }

                // 데이터 초기화
                pendingOpticZones.Clear();
                System.Diagnostics.Debug.WriteLine($"[MainWindow] pendingOpticZones 초기화 완료");
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] ❌ 테스트 시작 실패: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MainWindow] StackTrace: {ex.StackTrace}");
            CommunicationLogger.WriteLog($"❌ [AUTO_TEST_ERROR] 테스트 시작 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// IPVS 테스트 시작 요청 처리
    /// </summary>
    private void OnIpvsTestStartRequested(object sender, IpvsStartEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[MainWindow] IPVS 테스트 시작 요청 - Select: {e.Select}, InnerID: {e.InnerID}");
        CommunicationLogger.WriteLog($"🚀 [AUTO_TEST_START] IPVS 자동 테스트 시작 - Select: {e.Select}, Inner: {e.InnerID}");

        // TODO: IPVS 페이지로 이동 및 테스트 시작
        // pageNavigationManager?.NavigateToIpvsPage();
    }

    //25.12.08 - OPTIC RESTART 요청 처리 (다음 SEQUENCE 진행)
    /// <summary>
    /// OPTIC RESTART 요청 처리 (Client가 다음 SEQUENCE 진행 명령)
    /// </summary>
    private void OnOpticRestartRequested(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[MainWindow] OPTIC RESTART 요청 수신 - 다음 SEQUENCE 진행");
        CommunicationLogger.WriteLog($"▶️ [OPTIC_RESTART] Client로부터 RESTART 명령 수신 - 다음 SEQUENCE 진행");
        
        // OpticSeqExecutor에서 이미 이벤트를 구독하고 있으므로 여기서는 로그만 기록
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

    //25.12.08 - CommunicationServer 인스턴스 반환 (OpticSeqExecutor에서 사용)
    public CommunicationServer GetCommunicationServer()
    {
        return communicationServer;
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
            string isDarkModeStr = GlobalDataManager.GetValue("Theme", "IsDarkMode", "F");
            isDarkMode = isDarkModeStr.ToUpper() == "T";
            if (isDarkMode)
            {
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

    #region 자동 연결 기능

    /// <summary>
    /// 프로그램 시작 시 자동 연결 기능 초기화
    /// </summary>
    private async void InitializeAutoConnections()
    {
        try
        {
            // 1. TCP/IP Communication 자동 연결
            string autoConnectStr = GlobalDataManager.GetValue("Settings", "AUTO_CONNECT", "F");
            System.Diagnostics.Debug.WriteLine($"[AutoConnect] INI에서 읽은 AUTO_CONNECT 원본 값: '{autoConnectStr}'");
            System.Diagnostics.Debug.WriteLine($"[AutoConnect] ToUpper() 변환 후: '{autoConnectStr.ToUpper()}'");
            bool autoConnect = autoConnectStr.ToUpper() == "T";
            System.Diagnostics.Debug.WriteLine($"[AutoConnect] 최종 판단 ('{autoConnectStr.ToUpper()}' == 'T'): {autoConnect}");
            
            if (autoConnect)
            {
                System.Diagnostics.Debug.WriteLine("[AutoConnect] TCP/IP 자동 연결 시작...");
                await Task.Delay(500); // UI 초기화 대기
                
                string tcpIp = GlobalDataManager.GetValue("Settings", "TCP_IP", "127.0.0.1");
                string tcpPortStr = GlobalDataManager.GetValue("Settings", "TCP_PORT", "7777");
                
                if (int.TryParse(tcpPortStr, out int tcpPort))
                {
                    bool success = await StartCommunicationServer(tcpIp, tcpPort);
                    
                    string message;
                    if (success)
                    {
                        // 서버 시작 성공 - 연결 상태 저장 (MainSettingsWindow 버튼 초록색 표시용)
                        GlobalDataManager.SetValue("ConnectionState", "TCP_SERVER_RUNNING", "T");
                        
                        message = $"✅ TCP/IP 통신 서버가 시작되었습니다.\n\n" +
                                  $"📡 IP: {tcpIp}\n" +
                                  $"🔌 Port: {tcpPort}\n\n" +
                                  $"⏳ 클라이언트 연결 대기 중...\n" +
                                  $"(클라이언트가 연결되면 'AUTO MODE'가 표시됩니다)";
                    }
                    else
                    {
                        message = $"❌ TCP/IP 통신 서버 시작에 실패했습니다.\n\n" +
                                  $"IP: {tcpIp}\n" +
                                  $"Port: {tcpPort}\n\n" +
                                  $"포트가 이미 사용 중이거나 권한이 없을 수 있습니다.";
                    }
                    
                    MessageBoxImage icon = success ? MessageBoxImage.Information : MessageBoxImage.Warning;
                    
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(message, "TCP/IP 서버 자동 시작", MessageBoxButton.OK, icon);
                    });
                    
                    System.Diagnostics.Debug.WriteLine($"[AutoConnect] TCP/IP 서버 시작 {(success ? "성공" : "실패")} (클라이언트 연결 대기 중)");
                }
            }
            
            // 2. OPTIC Port 자동 연결
            string opticPortConnectStr = GlobalDataManager.GetValue("Settings", "OPTIC_PORT_CONNECT", "F");
            System.Diagnostics.Debug.WriteLine($"[AutoConnect] INI에서 읽은 OPTIC_PORT_CONNECT 원본 값: '{opticPortConnectStr}'");
            System.Diagnostics.Debug.WriteLine($"[AutoConnect] ToUpper() 변환 후: '{opticPortConnectStr.ToUpper()}'");
            bool opticPortConnect = opticPortConnectStr.ToUpper() == "T";
            System.Diagnostics.Debug.WriteLine($"[AutoConnect] 최종 판단 ('{opticPortConnectStr.ToUpper()}' == 'T'): {opticPortConnect}");
            
            if (opticPortConnect)
            {
                System.Diagnostics.Debug.WriteLine("[AutoConnect] OPTIC Port 자동 연결 시작...");
                await Task.Delay(1000); // DLL 초기화 대기
                
                await AutoConnectOpticPorts();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AutoConnect] 오류: {ex.Message}");
            ErrorLogger.Log($"자동 연결 오류: {ex.Message}", ErrorLogger.LogLevel.ERROR);
        }
    }

    /// <summary>
    /// OPTIC Port All Connect 자동 실행 (CellIdInputWindow의 AllConnectButton_Click과 동일)
    /// </summary>
    private async Task AutoConnectOpticPorts()
    {
        await Task.Run(() =>
        {
            try
            {
                // DLL 초기화 확인
                if (!DllManager.IsInitialized)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("DLL이 초기화되지 않았습니다.\nOPTIC Port 자동 연결을 건너뜁니다.",
                            "OPTIC Port 자동 연결", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                    return;
                }

                var successList = new List<string>();
                var failureList = new List<string>();

                // HVI 모드 확인
                bool isHviMode = GlobalDataManager.IsHviModeEnabled();
                System.Diagnostics.Debug.WriteLine($"[AutoConnect] HVI 모드: {isHviMode}");

                // MTP 섹션에서 포트 정보 읽기
                string section = "MTP";
                var sectionData = GlobalDataManager.ReadSection(section);
                if (sectionData == null || sectionData.Count == 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("MTP 섹션에 포트 정보가 없습니다.",
                            "OPTIC Port 자동 연결", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                    return;
                }

                // Zone 개수 확인
                int zoneCount = int.Parse(GlobalDataManager.GetValue("Settings", "MTP_ZONE", "2"));

                // 포트 수집 (중복 제거용)
                var pgPorts = new Dictionary<int, List<int>>();  // Port -> Zone List
                var measPorts = new Dictionary<int, List<int>>();  // Port -> Zone List

                // 모든 Zone의 포트 수집
                for (int zone = 1; zone <= zoneCount; zone++)
                {
                    // PG Port
                    string pgPortKey = $"PG_PORT_{zone}";
                    if (sectionData.TryGetValue(pgPortKey, out string pgPortStr) && !string.IsNullOrWhiteSpace(pgPortStr))
                    {
                        if (int.TryParse(pgPortStr.Trim(), out int pgPort))
                        {
                            if (!pgPorts.ContainsKey(pgPort))
                                pgPorts[pgPort] = new List<int>();
                            pgPorts[pgPort].Add(zone);
                        }
                    }

                    // MEAS Port
                    string measPortKey = $"MEAS_PORT_{zone}";
                    if (sectionData.TryGetValue(measPortKey, out string measPortStr) && !string.IsNullOrWhiteSpace(measPortStr))
                    {
                        if (int.TryParse(measPortStr.Trim(), out int measPort))
                        {
                            if (!measPorts.ContainsKey(measPort))
                                measPorts[measPort] = new List<int>();
                            measPorts[measPort].Add(zone);
                        }
                    }
                }

                // PG Port 연결 (중복 제거됨)
                foreach (var kvp in pgPorts)
                {
                    int port = kvp.Key;
                    var zones = kvp.Value;
                    string zoneDisplay = zones.Count > 1 ? $"Zones {string.Join(", ", zones)}" : $"Zone {zones[0]}";

                    try
                    {
                        bool result = DllFunctions.CallPGTurn(port);
                        if (result)
                        {
                            string successMsg = $"PG Port {port}: 성공 ({zoneDisplay})";
                            successList.Add(successMsg);

                            // 연결 상태 저장 (CellIdInputWindow와 동일한 형식)
                            // StateKey 형식: "PG:MTP:PG_PORT_1|PG_PORT_2|PG_PORT_3" (HVI 모드) 또는 "PG:MTP:PG_PORT_1" (Normal 모드)
                            var iniKeys = zones.Select(z => $"PG_PORT_{z}").ToList();
                            string keyPart = string.Join("|", iniKeys);
                            string stateKey = $"PG:MTP:{keyPart}";
                            PortConnectionManager.Instance.SetConnectionState(stateKey, true);
                            System.Diagnostics.Debug.WriteLine($"[AutoConnect] PG 연결 상태 저장: {stateKey} = True");
                        }
                        else
                        {
                            failureList.Add($"PG Port {port}: 실패 ({zoneDisplay})");
                        }
                    }
                    catch (Exception ex)
                    {
                        failureList.Add($"PG Port {port}: 예외 - {ex.Message} ({zoneDisplay})");
                    }
                }

                // MEAS Port 연결 (중복 제거됨)
                foreach (var kvp in measPorts)
                {
                    int port = kvp.Key;
                    var zones = kvp.Value;
                    string zoneDisplay = zones.Count > 1 ? $"Zones {string.Join(", ", zones)}" : $"Zone {zones[0]}";

                    try
                    {
                        bool result = DllFunctions.CallMeasTurn(port);
                        if (result)
                        {
                            string successMsg = $"MEAS Port {port}: 성공 ({zoneDisplay})";
                            successList.Add(successMsg);

                            // 연결 상태 저장 (CellIdInputWindow와 동일한 형식)
                            // StateKey 형식: "MEAS:MTP:MEAS_PORT_1|MEAS_PORT_2|MEAS_PORT_3" (HVI 모드) 또는 "MEAS:MTP:MEAS_PORT_1" (Normal 모드)
                            var iniKeys = zones.Select(z => $"MEAS_PORT_{z}").ToList();
                            string keyPart = string.Join("|", iniKeys);
                            string stateKey = $"MEAS:MTP:{keyPart}";
                            PortConnectionManager.Instance.SetConnectionState(stateKey, true);
                            System.Diagnostics.Debug.WriteLine($"[AutoConnect] MEAS 연결 상태 저장: {stateKey} = True");
                        }
                        else
                        {
                            failureList.Add($"MEAS Port {port}: 실패 ({zoneDisplay})");
                        }
                    }
                    catch (Exception ex)
                    {
                        failureList.Add($"MEAS Port {port}: 예외 - {ex.Message} ({zoneDisplay})");
                    }
                }

                // 결과 팝업 표시
                Dispatcher.Invoke(() =>
                {
                    var sb = new StringBuilder();
                    
                    if (successList.Count > 0)
                    {
                        sb.AppendLine("✅ [성공]");
                        foreach (var msg in successList)
                            sb.AppendLine($"   - {msg}");
                    }
                    
                    if (failureList.Count > 0)
                    {
                        if (sb.Length > 0) sb.AppendLine();
                        sb.AppendLine("❌ [실패]");
                        foreach (var msg in failureList)
                            sb.AppendLine($"   - {msg}");
                    }

                    string message = sb.Length > 0 ? sb.ToString() : "연결할 포트가 없습니다.";
                    MessageBoxImage icon = failureList.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information;
                    
                    MessageBox.Show(message, "OPTIC Port 자동 연결", MessageBoxButton.OK, icon);
                });

                System.Diagnostics.Debug.WriteLine($"[AutoConnect] OPTIC Port 연결 완료 - 성공: {successList.Count}, 실패: {failureList.Count}, HVI 모드: {isHviMode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AutoConnect] OPTIC Port 연결 오류: {ex.Message}");
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"OPTIC Port 자동 연결 중 오류가 발생했습니다:\n{ex.Message}",
                        "OPTIC Port 자동 연결", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        });
    }

    #endregion
}
}
