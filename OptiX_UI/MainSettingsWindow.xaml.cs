using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace OptiX
{
    /// <summary>
    /// MainSettingsWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainSettingsWindow : Window
    {
        private IniFileManager iniManager;
        private bool isDarkMode;
        private string selectedLanguage = "Korean";

        public MainSettingsWindow() : this(false)
        {
        }

        public MainSettingsWindow(bool isDarkMode)
        {
            InitializeComponent();
            this.isDarkMode = isDarkMode;
            
            // 실행 파일 기준 상대 경로로 INI 파일 찾기
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDir = System.IO.Path.GetDirectoryName(exePath);
            string iniPath = @"D:\\Project\\Recipe\\OptiX.ini";
            this.iniManager = new IniFileManager(iniPath);
            
            // 다크모드 적용
            ApplyTheme();
            LoadCurrentSettings();
            
            // 체크박스 이벤트 연결
            SetupCheckBoxEvents();
            
            // 텍스트박스 색상 적용
            ApplyTextBoxColors();
            
            // MainWindow의 CommunicationServer 상태 변경 이벤트 구독
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.CommunicationServerStatusChanged += OnCommunicationServerStatusChanged;
                UpdateConnectionStatus(); // 초기 상태 업데이트
            }
            
            // 언어 적용
            ApplyLanguage();
            
            // 언어 변경 이벤트 구독
            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        // 언어 적용 메서드
        public void ApplyLanguage()
        {
            try
            {
                // TCP/IP 연결 제목
                if (TcpIpConnectionTitle != null)
                    TcpIpConnectionTitle.Text = LanguageManager.GetText("MainSettings.TCPIPConnection");
                
                // DLL 폴더 설정 제목
                if (DllFolderSettingsTitle != null)
                    DllFolderSettingsTitle.Text = LanguageManager.GetText("MainSettings.DLLFolderSettings");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"언어 적용 오류: {ex.Message}");
            }
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
        }

        private void ApplyTheme()
        {
            if (isDarkMode)
            {
                // 다크모드 색상 적용
                this.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                
                // 동적 리소스 업데이트 - 다크모드
                Resources["DynamicBackground"] = new SolidColorBrush(Color.FromRgb(15, 23, 42)); // #0F172A
                Resources["DynamicCardBackground"] = new SolidColorBrush(Color.FromRgb(30, 41, 59)); // #1E293B
                Resources["DynamicBorderColor"] = new SolidColorBrush(Color.FromRgb(71, 85, 105)); // #475569
                Resources["DynamicTextColor"] = new SolidColorBrush(Color.FromRgb(241, 245, 249)); // #F1F5F9
                Resources["DynamicInputBackground"] = new SolidColorBrush(Color.FromRgb(51, 65, 85)); // #334155
                Resources["DynamicInputBorder"] = new SolidColorBrush(Color.FromRgb(71, 85, 105)); // #475569
                Resources["DynamicTextColor"] = new SolidColorBrush(Color.FromRgb(241, 245, 249)); // #F1F5F9
            }
            else
            {
                // 라이트모드 색상 적용 (기본값)
                this.Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));
                
                // 동적 리소스 업데이트 - 라이트모드
                Resources["DynamicBackground"] = new SolidColorBrush(Color.FromRgb(248, 250, 252)); // #F8FAFC
                Resources["DynamicCardBackground"] = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // #FFFFFF
                Resources["DynamicBorderColor"] = new SolidColorBrush(Color.FromRgb(226, 232, 240)); // #E2E8F0
                Resources["DynamicTextColor"] = new SolidColorBrush(Color.FromRgb(30, 41, 59)); // #1E293B
                Resources["DynamicInputBackground"] = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // #FFFFFF
                Resources["DynamicInputBorder"] = new SolidColorBrush(Color.FromRgb(209, 213, 219)); // #D1D5DB
                Resources["DynamicTextColor"] = new SolidColorBrush(Color.FromRgb(30, 41, 59)); // #1E293B
            }
        }

        private void ApplyTextBoxColors()
        {
            if (isDarkMode)
            {
                // 다크모드: 어두운 배경에 밝은 글자
                TcpIpTextBox.Background = new SolidColorBrush(Color.FromRgb(51, 65, 85)); // #334155
                TcpIpTextBox.Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)); // #F1F5F9
                TcpIpTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105)); // #475569
                TcpIpTextBox.CaretBrush = new SolidColorBrush(Color.FromRgb(241, 245, 249)); // #F1F5F9
                TcpIpTextBox.SelectionBrush = new SolidColorBrush(Color.FromRgb(139, 92, 246)); // #8B5CF6
                TcpIpTextBox.SelectionTextBrush = new SolidColorBrush(Colors.White);
                
                TcpPortTextBox.Background = new SolidColorBrush(Color.FromRgb(51, 65, 85)); // #334155
                TcpPortTextBox.Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)); // #F1F5F9
                TcpPortTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105)); // #475569
                TcpPortTextBox.CaretBrush = new SolidColorBrush(Color.FromRgb(241, 245, 249)); // #F1F5F9
                TcpPortTextBox.SelectionBrush = new SolidColorBrush(Color.FromRgb(139, 92, 246)); // #8B5CF6
                TcpPortTextBox.SelectionTextBrush = new SolidColorBrush(Colors.White);
            }
            else
            {
                // 라이트모드: 밝은 배경에 어두운 글자
                TcpIpTextBox.Background = new SolidColorBrush(Colors.White);
                TcpIpTextBox.Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)); // #1E293B
                TcpIpTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219)); // #D1D5DB
                TcpIpTextBox.CaretBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59)); // #1E293B
                TcpIpTextBox.SelectionBrush = new SolidColorBrush(Color.FromRgb(139, 92, 246)); // #8B5CF6
                TcpIpTextBox.SelectionTextBrush = new SolidColorBrush(Colors.White);
                
                TcpPortTextBox.Background = new SolidColorBrush(Colors.White);
                TcpPortTextBox.Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)); // #1E293B
                TcpPortTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219)); // #D1D5DB
                TcpPortTextBox.CaretBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59)); // #1E293B
                TcpPortTextBox.SelectionBrush = new SolidColorBrush(Color.FromRgb(139, 92, 246)); // #8B5CF6
                TcpPortTextBox.SelectionTextBrush = new SolidColorBrush(Colors.White);
            }
        }

        private void LoadCurrentSettings()
        {
            try
            {
                // 언어 설정 로드
                string currentLanguage = iniManager.ReadValue("Settings", "Language", "Korean");
                selectedLanguage = currentLanguage;
                
                // 언어 체크박스 설정
                KoreanCheckBox.IsChecked = (currentLanguage == "Korean");
                EnglishCheckBox.IsChecked = (currentLanguage == "English");
                VietnameseCheckBox.IsChecked = (currentLanguage == "Vietnamese");
                
                // TCP/IP 설정 로드
                LoadTcpIpSettings();
                ApplyTextBoxColors(); // 텍스트박스 색상 적용
                
                // DLL 폴더 설정 로드
                string dllFolder = iniManager.ReadValue("Settings", "DLL_FOLDER", "");
                DllFolderTextBox.Text = string.IsNullOrEmpty(dllFolder) ? "DLL 폴더를 선택하세요" : dllFolder;
                
                System.Diagnostics.Debug.WriteLine($"설정 로드 완료 - Language: {currentLanguage}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"설정 로드 오류: {ex.Message}");
                
                // 오류 시 기본값 설정
                KoreanCheckBox.IsChecked = true;
                EnglishCheckBox.IsChecked = false;
                VietnameseCheckBox.IsChecked = false;
                selectedLanguage = "Korean";
                TcpIpTextBox.Text = "2002";
            }
        }

        private void SetupCheckBoxEvents()
        {
            // 언어 선택 체크박스 이벤트 (라디오 버튼처럼 동작)
            KoreanCheckBox.Checked += (s, e) => {
                EnglishCheckBox.IsChecked = false;
                VietnameseCheckBox.IsChecked = false;
                selectedLanguage = "Korean";
            };
            
            EnglishCheckBox.Checked += (s, e) => {
                KoreanCheckBox.IsChecked = false;
                VietnameseCheckBox.IsChecked = false;
                selectedLanguage = "English";
            };
            
            VietnameseCheckBox.Checked += (s, e) => {
                KoreanCheckBox.IsChecked = false;
                EnglishCheckBox.IsChecked = false;
                selectedLanguage = "Vietnamese";
            };
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 언어 설정 저장
                SaveLanguageSettings();
                
                // TCP/IP 설정 저장
                SaveTcpIpSettings();

                // 전역 데이터 다시 로드
                GlobalDataManager.ReloadIniData();

                // DLL 재로드 (DLL 경로가 변경되었을 수 있으므로)
                bool dllReloaded = DllManager.Reload();
                if (!dllReloaded)
                {
                    MessageBox.Show("DLL 로드에 실패했습니다. DLL 경로를 확인해주세요.", "경고", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                MessageBox.Show("모든 설정이 성공적으로 저장되었습니다.", "저장 완료", 
                              MessageBoxButton.OK, MessageBoxImage.Information);

                // Non-Modal 창에서는 DialogResult 사용하지 않음
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 중 오류가 발생했습니다: {ex.Message}", "오류", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveLanguageSettings()
        {
            iniManager.WriteValue("Settings", "Language", selectedLanguage);
            
            // LanguageManager에 언어 변경 알림
            LanguageManager.SetLanguage(selectedLanguage);
            
            System.Diagnostics.Debug.WriteLine($"언어 설정 저장됨: {selectedLanguage}");
        }

        private void SaveTcpIpSettings()
        {
            string tcpIp = TcpIpTextBox.Text.Trim();
            if (string.IsNullOrEmpty(tcpIp))
            {
                tcpIp = "127.0.0.1"; // 기본값
            }
            
            iniManager.WriteValue("Settings", "TCP_IP", tcpIp);
            
            string tcpPort = TcpPortTextBox.Text.Trim();
            if (string.IsNullOrEmpty(tcpPort))
            {
                tcpPort = "7777"; // 기본값
            }
            
            iniManager.WriteValue("Settings", "TCP_PORT", tcpPort);
            
            System.Diagnostics.Debug.WriteLine($"TCP/IP 설정 저장됨: {tcpIp}:{tcpPort}");
        }

        private void LoadTcpIpSettings()
        {
            string tcpIp = iniManager.ReadValue("Settings", "TCP_IP", "127.0.0.1");
            TcpIpTextBox.Text = tcpIp;
            
            string tcpPort = iniManager.ReadValue("Settings", "TCP_PORT", "7777");
            TcpPortTextBox.Text = tcpPort;
            
            System.Diagnostics.Debug.WriteLine($"TCP/IP 설정 로드됨: {tcpIp}:{tcpPort}");
        }

        private void TcpIpButton_Click(object sender, RoutedEventArgs e)
        {
            // TCP/IP 버튼 클릭 시 포커스를 TCP/IP 입력 필드로 이동
            TcpIpTextBox.Focus();
            TcpIpTextBox.SelectAll();
        }

        private void PortButton_Click(object sender, RoutedEventArgs e)
        {
            // Port 버튼 클릭 시 포커스를 Port 입력 필드로 이동
            TcpPortTextBox.Focus();
            TcpPortTextBox.SelectAll();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // MainWindow의 CommunicationServer와 연동
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    if (mainWindow.IsCommunicationServerRunning())
                    {
                        // 서버가 실행 중이면 중지
                        mainWindow.StopCommunicationServer();
                        CommunicationLogger.WriteLog("서버 중지됨 - 사용자 요청");
                    }
                    else
                    {
                        // 서버가 중지되어 있으면 시작
                        string tcpIp = TcpIpTextBox.Text.Trim();
                        string portText = TcpPortTextBox.Text.Trim();
                        
                        if (string.IsNullOrEmpty(tcpIp) || string.IsNullOrEmpty(portText))
                        {
                            MessageBox.Show("TCP/IP 주소와 포트를 입력해주세요.", "입력 오류", 
                                          MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        
                        if (!int.TryParse(portText, out int port))
                        {
                            MessageBox.Show("올바른 포트 번호를 입력해주세요.", "입력 오류", 
                                          MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        
                        await mainWindow.StartCommunicationServer(tcpIp, port);
                        CommunicationLogger.WriteLog("서버 시작됨 - 사용자 요청");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"연결 중 오류가 발생했습니다: {ex.Message}", "연결 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
                CommunicationLogger.WriteLog($"ConnectButton_Click 오류: {ex.Message}");
            }
        }

        private void OnCommunicationServerStatusChanged(object sender, bool isConnected)
        {
            UpdateConnectionStatus();
        }

        private void UpdateConnectionStatus()
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    bool isRunning = mainWindow.IsCommunicationServerRunning();
                    
                    if (isRunning)
                    {
                        ConnectButton.Content = "Disconnect";
                        ConnectButton.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)); // Red
                    }
                    else
                    {
                        ConnectButton.Content = "Connect";
                        ConnectButton.Background = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // Green
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateConnectionStatus 오류: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // MainWindow의 CommunicationServer 상태 변경 이벤트 해제
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.CommunicationServerStatusChanged -= OnCommunicationServerStatusChanged;
                }
                
                // 언어 변경 이벤트 해제
                LanguageManager.LanguageChanged -= OnLanguageChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnClosed 오류: {ex.Message}");
            }
            
            base.OnClosed(e);
        }

        /// <summary>
        /// DLL 폴더 선택
        /// </summary>
        private void DllFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "DLL 폴더를 선택하세요",
                Filter = "폴더 선택|*.",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "폴더를 선택하세요"
            };

            if (dialog.ShowDialog() == true)
            {
                string selectedFolder = System.IO.Path.GetDirectoryName(dialog.FileName) ?? "";
                DllFolderTextBox.Text = string.IsNullOrEmpty(selectedFolder) ? "DLL 폴더를 선택하세요" : selectedFolder;
                
                // INI 파일에 DLL 폴더 경로 저장
                iniManager.WriteValue("Settings", "DLL_FOLDER", selectedFolder);
                
                // 전역 데이터 다시 로드
                GlobalDataManager.ReloadIniData();
            }
        }

        private void DllFolderTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
    }
}
