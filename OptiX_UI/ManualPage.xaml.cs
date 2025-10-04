using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using OptiX.Models;

namespace OptiX
{
    /// <summary>
    /// ManualPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ManualPage : UserControl
    {
        private ObservableCollection<ManualDataItem> dataItems;
        private bool isCie1931Selected = true;
        private bool _isDarkMode = false;
        private int selectedRowIndex = -1; // 선택된 행 인덱스
        
        // Connect 버튼 상태 관리
        private bool isPgConnected = false;
        private bool isMeasConnected = false;

        public ManualPage()
        {
            InitializeComponent();
            InitializeData();
            SetupEventHandlers();
            LoadThemeFromIni();
        }

        private void InitializeData()
        {
            // 데이터 테이블 초기화
            dataItems = new ObservableCollection<ManualDataItem>();
            DataTableGrid.ItemsSource = dataItems;

            // CIE1931을 기본으로 선택 (탭 스타일 적용)
            Cie1931Button.Style = (Style)FindResource("ActiveTabButtonStyle");
            Cie1976Button.Style = (Style)FindResource("TabButtonStyle");
            
            // 초기 connect 버튼 상태 설정 (보라색 CONNECT)
            PgConnectButton.Content = "CONNECT";
            PgConnectButton.Style = (Style)FindResource("InitialButtonStyle");
            MeasureConnectButton.Content = "CONNECT";
            MeasureConnectButton.Style = (Style)FindResource("InitialButtonStyle");
            
            // 샘플 데이터 추가 (테스트용)
            AddSampleData();
        }

        private void SetupEventHandlers()
        {
            // DataGrid 선택 변경 이벤트 핸들러 추가
            DataTableGrid.SelectionChanged += DataTableGrid_SelectionChanged;
        }

        private void AddSampleData()
        {
            // 초기 샘플 데이터 제거 - MEASURE 버튼으로만 데이터 추가
            // 데이터 테이블은 비어있는 상태로 시작
        }

        #region 이벤트 핸들러

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // 메인 페이지로 돌아가기
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.ShowMainPage();
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            // 데이터 테이블 초기화
            dataItems.Clear();
            
            // 색좌표 초기화
            ResetColorCoordinates();
            
            MessageBox.Show("데이터가 초기화되었습니다.", "초기화 완료", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }



        #endregion

        #region 색좌표 관련

        private void Cie1931Button_Click(object sender, RoutedEventArgs e)
        {
            // CIE1931 탭 활성화
            Cie1931Button.Style = (Style)FindResource("ActiveTabButtonStyle");
            
            // CIE1976 탭 비활성화
            Cie1976Button.Style = (Style)FindResource("TabButtonStyle");
            
            // 콘텐츠 전환
            Cie1931Area.Visibility = Visibility.Visible;
            Cie1976Area.Visibility = Visibility.Collapsed;
            
            // 상태 업데이트 및 좌표 표시
            isCie1931Selected = true;
            UpdateAllCoordinates();
        }

        private void Cie1976Button_Click(object sender, RoutedEventArgs e)
        {
            // CIE1976 탭 활성화
            Cie1976Button.Style = (Style)FindResource("ActiveTabButtonStyle");
            
            // CIE1931 탭 비활성화
            Cie1931Button.Style = (Style)FindResource("TabButtonStyle");
            
            // 콘텐츠 전환
            Cie1976Area.Visibility = Visibility.Visible;
            Cie1931Area.Visibility = Visibility.Collapsed;
            
            // 상태 업데이트 및 좌표 표시
            isCie1931Selected = false;
            UpdateAllCoordinates();
        }

        private void UpdateColorCoordinateDisplay()
        {
            if (isCie1931Selected)
            {
                Cie1931Area.Visibility = Visibility.Visible;
                Cie1976Area.Visibility = Visibility.Collapsed;
                
                // CIE1931 버튼 활성화, CIE1976 버튼 비활성화
                Cie1931Button.Background = System.Windows.Media.Brushes.DarkBlue;
                Cie1976Button.Background = System.Windows.Media.Brushes.Gray;
            }
            else
            {
                Cie1931Area.Visibility = Visibility.Collapsed;
                Cie1976Area.Visibility = Visibility.Visible;
                
                // CIE1976 버튼 활성화, CIE1931 버튼 비활성화
                Cie1931Button.Background = System.Windows.Media.Brushes.Gray;
                Cie1976Button.Background = System.Windows.Media.Brushes.DarkBlue;
            }
        }

        private void UpdateColorCoordinates(ManualDataItem item)
        {
            if (item != null)
            {
                // 좌표 표시 TextBlock이 제거되었으므로 다이어그램 위의 점만 업데이트
                // 향후 Canvas 위의 점 위치 업데이트 로직 구현 예정
                System.Diagnostics.Debug.WriteLine($"좌표 업데이트: x={item.X:F3}, y={item.Y:F3}, u={item.U:F3}, v={item.V:F3}");
            }
        }

        private void ResetColorCoordinates()
        {
            // 좌표 표시 TextBlock이 제거되었으므로 다이어그램 위의 점만 초기화
            // 향후 Canvas 위의 점 위치 초기화 로직 구현 예정
            System.Diagnostics.Debug.WriteLine("좌표 초기화 완료");
        }

        #endregion

        #region 컨트롤 패널 관련

        private void PgConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string portText = PgPortTextBox.Text.Trim();
            if (string.IsNullOrEmpty(portText))
            {
                MessageBox.Show("포트를 입력해주세요.", "입력 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // DLL 초기화 확인
            if (!DllManager.IsInitialized)
            {
                MessageBox.Show("DLL이 초기화되지 않았습니다. 메인 설정창에서 DLL 경로를 확인해주세요.", "오류",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 포트 번호를 정수로 변환
                if (!int.TryParse(portText, out int port))
                {
                    MessageBox.Show("포트 번호는 숫자여야 합니다.", "입력 오류",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // DLL 함수 호출
                bool success = DllManager.CallPGTurn(port);

                if (success)
                {
                    // 연결 성공 시 초록색 CONNECT 스타일 적용
                    PgConnectButton.Content = "CONNECT";
                    PgConnectButton.Style = (Style)FindResource("ConnectedButtonStyle");
                    isPgConnected = true;
                    
                    MessageBox.Show($"PG 포트 {port} 연결되었습니다.", "연결 성공", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // 연결 실패 시 빨간색 DISCONNECT 스타일 적용
                    PgConnectButton.Content = "DISCONNECT";
                    PgConnectButton.Style = (Style)FindResource("DisconnectedButtonStyle");
                    isPgConnected = false;
                    
                    MessageBox.Show($"PG 포트 {port} 연결에 실패했습니다.", "연결 실패",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // 오류 발생 시 빨간색 DISCONNECT 스타일 적용
                PgConnectButton.Content = "DISCONNECT";
                PgConnectButton.Style = (Style)FindResource("DisconnectedButtonStyle");
                isPgConnected = false;
                
                MessageBox.Show($"PG 포트 연결 중 오류가 발생했습니다: {ex.Message}", "오류",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PatternSendButton_Click(object sender, RoutedEventArgs e)
        {
            string patternText = PatternTextBox.Text.Trim();

            if (string.IsNullOrEmpty(patternText))
            {
                MessageBox.Show("패턴을 입력해주세요.", "입력 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // DLL 초기화 확인
            if (!DllManager.IsInitialized)
            {
                MessageBox.Show("DLL이 초기화되지 않았습니다. 메인 설정창에서 DLL 경로를 확인해주세요.", "오류",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 패턴 번호를 정수로 변환
                if (!int.TryParse(patternText, out int pattern))
                {
                    MessageBox.Show("패턴 번호는 숫자여야 합니다.", "입력 오류",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // DLL 함수 호출
                bool success = DllManager.CallPGPattern(pattern);

                if (success)
                {
                    MessageBox.Show($"패턴 {pattern} 전송 완료", "패턴 전송", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"패턴 {pattern} 전송에 실패했습니다.", "전송 실패",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"패턴 전송 중 오류가 발생했습니다: {ex.Message}", "오류",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void VoltageSendButton_Click(object sender, RoutedEventArgs e)
        {
            string voltage1Text = Voltage1TextBox.Text.Trim();
            string voltage2Text = Voltage2TextBox.Text.Trim();
            string voltage3Text = Voltage3TextBox.Text.Trim();

            if (string.IsNullOrEmpty(voltage1Text) || 
                string.IsNullOrEmpty(voltage2Text) || 
                string.IsNullOrEmpty(voltage3Text))
            {
                MessageBox.Show("전압을 모두 입력해주세요.", "입력 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // DLL 초기화 확인
            if (!DllManager.IsInitialized)
            {
                MessageBox.Show("DLL이 초기화되지 않았습니다. 메인 설정창에서 DLL 경로를 확인해주세요.", "오류",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 전압 값을 정수로 변환
                if (!int.TryParse(voltage1Text, out int RV) ||
                    !int.TryParse(voltage2Text, out int GV) ||
                    !int.TryParse(voltage3Text, out int BV))
                {
                    MessageBox.Show("전압 값은 숫자여야 합니다.", "입력 오류",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // DLL 함수 호출
                bool success = DllManager.CallPGVoltagesnd(RV, GV, BV);

                if (success)
                {
                    MessageBox.Show($"전압 전송 완료: R={RV}, G={GV}, B={BV}", "전압 전송", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"전압 전송에 실패했습니다.", "전송 실패",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"전압 전송 중 오류가 발생했습니다: {ex.Message}", "오류",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MeasureConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string portText = MeasurePortTextBox.Text.Trim();
            if (string.IsNullOrEmpty(portText))
            {
                MessageBox.Show("측정 포트를 입력해주세요.", "입력 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // DLL 초기화 확인
            if (!DllManager.IsInitialized)
            {
                MessageBox.Show("DLL이 초기화되지 않았습니다. 메인 설정창에서 DLL 경로를 확인해주세요.", "오류",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 포트 번호를 정수로 변환
                if (!int.TryParse(portText, out int port))
                {
                    MessageBox.Show("포트 번호는 숫자여야 합니다.", "입력 오류",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // DLL 함수 호출
                bool success = DllManager.CallMeasTurn(port);

                if (success)
                {
                    // 연결 성공 시 초록색 CONNECT 스타일 적용
                    MeasureConnectButton.Content = "CONNECT";
                    MeasureConnectButton.Style = (Style)FindResource("ConnectedButtonStyle");
                    isMeasConnected = true;
                    
                    MessageBox.Show($"측정 포트 {port} 연결되었습니다.", "연결 성공", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // 연결 실패 시 빨간색 DISCONNECT 스타일 적용
                    MeasureConnectButton.Content = "DISCONNECT";
                    MeasureConnectButton.Style = (Style)FindResource("DisconnectedButtonStyle");
                    isMeasConnected = false;
                    
                    MessageBox.Show($"측정 포트 {port} 연결에 실패했습니다.", "연결 실패",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // 오류 발생 시 빨간색 DISCONNECT 스타일 적용
                MeasureConnectButton.Content = "DISCONNECT";
                MeasureConnectButton.Style = (Style)FindResource("DisconnectedButtonStyle");
                isMeasConnected = false;
                
                MessageBox.Show($"측정 포트 연결 중 오류가 발생했습니다: {ex.Message}", "오류",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MeasureButton_Click(object sender, RoutedEventArgs e)
        {
            // 연결 상태 확인
            if (!isPgConnected && !isMeasConnected)
            {
                MessageBox.Show("PG_Port와 MEAS_Port 모두 연결되지 않았습니다.\n먼저 연결을 시도해주세요.", "연결 필요",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else if (!isPgConnected)
            {
                MessageBox.Show("PG_Port가 연결되지 않았습니다.\nPG_Port를 먼저 연결해주세요.", "PG_Port 연결 필요",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else if (!isMeasConnected)
            {
                MessageBox.Show("MEAS_Port가 연결되지 않았습니다.\nMEAS_Port를 먼저 연결해주세요.", "MEAS_Port 연결 필요",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // DLL 초기화 확인
            if (!DllManager.IsInitialized)
            {
                MessageBox.Show("DLL이 초기화되지 않았습니다. 메인 설정창에서 DLL 경로를 확인해주세요.", "오류",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                MessageBox.Show("측정을 시작합니다...", "측정", 
                              MessageBoxButton.OK, MessageBoxImage.Information);

                // DLL 함수 호출하여 측정 데이터 가져오기
                var (measureData, success) = DllManager.CallGetdata();

                if (success)
                {
                    // 측정 데이터를 데이터 테이블에 추가
                    AddMeasurementData(
                        measureData.L,     // L (휘도)
                        measureData.x,     // x (CIE 1931)
                        measureData.y,     // y (CIE 1931)
                        measureData.u,     // u (CIE 1976)
                        measureData.v,     // v (CIE 1976)
                        measureData.cur,   // Current (전류)
                        measureData.eff    // Efficiency (효율)
                    );

                    // 좌표 표시 업데이트
                    UpdateAllCoordinates();
                    
                    //MessageBox.Show($"측정 완료!\n" +
                    //              $"L: {measureData.L:F1}\n" +
                    //              $"x: {measureData.x:F3}, y: {measureData.y:F3}\n" +
                    //              $"u: {measureData.u:F3}, v: {measureData.v:F3}\n" +
                    //              $"전류: {measureData.cur:F1}\n" +
                    //              $"효율: {measureData.eff:F1}%", 
                    //              "측정 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("측정 데이터를 가져오는데 실패했습니다.", "측정 실패",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"측정 중 오류가 발생했습니다: {ex.Message}", "오류",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 테마 관리

        /// <summary>
        /// INI 파일에서 테마 설정 로드
        /// </summary>
        private void LoadThemeFromIni()
        {
            try
            {
                string themeValue = GlobalDataManager.GetValue("Settings", "THEME", "Light");
                _isDarkMode = themeValue.Equals("Dark", StringComparison.OrdinalIgnoreCase);
                ApplyTheme();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"테마 로드 오류: {ex.Message}");
                _isDarkMode = false; // 기본값은 라이트 모드
                ApplyTheme();
            }
        }

        /// <summary>
        /// 테마 적용 (OpticPage와 동일한 방식)
        /// </summary>
        private void ApplyTheme()
        {
            try
            {
                UpdateDynamicColors(_isDarkMode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"테마 적용 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 동적 색상 업데이트 (OpticPage와 동일)
        /// </summary>
        private void UpdateDynamicColors(bool isDark)
        {
            Dispatcher.Invoke(() =>
            {
                if (isDark)
                {
                    // 다크모드 색상으로 변경
                    Resources["DynamicBackgroundColor"] = new SolidColorBrush(Color.FromRgb(15, 23, 42)); // #0F172A
                    Resources["DynamicSurfaceColor"] = new SolidColorBrush(Color.FromRgb(51, 65, 85)); // #334155
                    Resources["DynamicCardColor"] = new SolidColorBrush(Color.FromRgb(51, 65, 85)); // #334155
                    Resources["DynamicBorderColor"] = new SolidColorBrush(Color.FromRgb(71, 85, 105)); // #475569
                    Resources["DynamicTextPrimaryColor"] = new SolidColorBrush(Color.FromRgb(241, 245, 249)); // #F1F5F9
                    Resources["DynamicTextSecondaryColor"] = new SolidColorBrush(Color.FromRgb(203, 213, 225)); // #CBD5E1
                    Resources["DynamicTextMutedColor"] = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // #94A3B8
                    Resources["DynamicTextColor"] = new SolidColorBrush(Color.FromRgb(241, 245, 249)); // #F1F5F9
                }
                else
                {
                    // 라이트모드 색상으로 변경
                    Resources["DynamicBackgroundColor"] = new SolidColorBrush(Color.FromRgb(248, 250, 252)); // #F8FAFC
                    Resources["DynamicSurfaceColor"] = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // #FFFFFF
                    Resources["DynamicCardColor"] = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // #FFFFFF
                    Resources["DynamicBorderColor"] = new SolidColorBrush(Color.FromRgb(226, 232, 240)); // #E2E8F0
                    Resources["DynamicTextPrimaryColor"] = new SolidColorBrush(Color.FromRgb(30, 41, 59)); // #1E293B
                    Resources["DynamicTextSecondaryColor"] = new SolidColorBrush(Color.FromRgb(100, 116, 139)); // #64748B
                    Resources["DynamicTextMutedColor"] = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // #94A3B8
                    Resources["DynamicTextColor"] = new SolidColorBrush(Color.FromRgb(30, 41, 59)); // #1E293B
                }
            });
        }

        /// <summary>
        /// 테마 토글 (외부에서 호출 가능)
        /// </summary>
        public void ToggleTheme()
        {
            _isDarkMode = !_isDarkMode;
            ApplyTheme();
        }

        /// <summary>
        /// 특정 테마로 설정 (외부에서 호출 가능)
        /// </summary>
        /// <param name="isDarkMode">다크모드 여부</param>
        public void SetTheme(bool isDarkMode)
        {
            _isDarkMode = isDarkMode;
            ApplyTheme();
        }

        // DataGrid 선택 변경 이벤트 핸들러 (토글 기능 포함)
        private void DataTableGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataTableGrid.SelectedItem is ManualDataItem selectedItem)
            {
                int newSelectedIndex = DataTableGrid.SelectedIndex;
                
                // 같은 행을 다시 클릭한 경우 토글 (선택 해제)
                if (selectedRowIndex == newSelectedIndex)
                {
                    selectedRowIndex = -1;
                    // 이벤트 핸들러를 일시적으로 제거하여 무한 루프 방지
                    DataTableGrid.SelectionChanged -= DataTableGrid_SelectionChanged;
                    DataTableGrid.SelectedIndex = -1;
                    DataTableGrid.SelectionChanged += DataTableGrid_SelectionChanged;
                }
                else
                {
                    // 다른 행을 클릭한 경우 선택
                    selectedRowIndex = newSelectedIndex;
                }
            }
            else
            {
                selectedRowIndex = -1;
            }
            
            // 항상 모든 좌표를 업데이트하여 현재 selectedRowIndex에 따라 강조 표시
            UpdateAllCoordinates();
        }

        // 모든 좌표를 업데이트
        private void UpdateAllCoordinates()
        {
            if (isCie1931Selected)
            {
                UpdateCIE1931Coordinates();
            }
            else
            {
                UpdateCIE1976Coordinates();
            }
        }

        // CIE1931 좌표 업데이트 (x,y 값 사용)
        private void UpdateCIE1931Coordinates()
        {
            Cie1931Canvas.Children.Clear();
            
            for (int i = 0; i < dataItems.Count; i++)
            {
                var item = dataItems[i];
                bool isSelected = (i == selectedRowIndex);
                AddPointToCanvas(Cie1931Canvas, item.X, item.Y, isSelected, item.Num);
            }
        }

        // CIE1976 좌표 업데이트 (u,v 값 사용)
        private void UpdateCIE1976Coordinates()
        {
            Cie1976Canvas.Children.Clear();
            
            for (int i = 0; i < dataItems.Count; i++)
            {
                var item = dataItems[i];
                bool isSelected = (i == selectedRowIndex);
                AddPointToCanvas(Cie1976Canvas, item.U, item.V, isSelected, item.Num);
            }
        }

        // Canvas에 점 추가
        private void AddPointToCanvas(Canvas canvas, double x, double y, bool isSelected, int pointNumber)
        {
            // CIE 다이어그램 좌표를 Canvas 좌표로 변환
            // 실제 CIE1931 다이어그램 이미지: x(0-0.8), y(0-0.9)
            // 실제 CIE1976 다이어그램 이미지: u(0-0.6), v(0-0.6)
            // Canvas 크기: 300x300, 여백 고려하여 실제 좌표 영역은 (30,30)~(270,270)
            
            double canvasX, canvasY;
            
            if (canvas == Cie1931Canvas)
            {
                // CIE1931: x(0-0.8), y(0-0.9) -> Canvas(30-270, 30-270)
                // x 좌표 변환: 0.0~0.8 -> 30~270 (실제 좌표 영역, 미세 조정)
                canvasX = 30 + (x / 0.8) * 240 + (x * 15); // x값 보정 추가
                // y 좌표 변환: 0.0~0.9 -> 270~30 (Y축 뒤집기 + 실제 좌표 영역)
                canvasY = 270 - (y / 0.9) * 240;
            }
            else
            {
                // CIE1976: u(0-0.6), v(0-0.6) -> Canvas(30-270, 30-270)
                // u 좌표 변환: 0.0~0.6 -> 30~270 (실제 좌표 영역)
                canvasX = 30 + (x / 0.6) * 240;
                // v 좌표 변환: 0.0~0.6 -> 270~30 (Y축 뒤집기 + 실제 좌표 영역)
                canvasY = 270 - (y / 0.6) * 240;
            }

            // 점 크기와 색상 설정
            double pointSize = isSelected ? 16 : 12;
            Brush pointColor = isSelected ? Brushes.Red : Brushes.Blue;
            Brush strokeColor = isSelected ? Brushes.DarkRed : Brushes.DarkBlue;
            double strokeThickness = isSelected ? 3 : 2;

            // 원형 점 추가
            var ellipse = new Ellipse
            {
                Width = pointSize,
                Height = pointSize,
                Fill = pointColor,
                Stroke = strokeColor,
                StrokeThickness = strokeThickness
            };

            Canvas.SetLeft(ellipse, canvasX - pointSize / 2);
            Canvas.SetTop(ellipse, canvasY - pointSize / 2);

            canvas.Children.Add(ellipse);

            // 선택된 점에만 좌표 라벨 추가
            if (isSelected)
            {
                var label = new TextBlock
                {
                    Text = $"({x:F3}, {y:F3})",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = strokeColor
                };

                Canvas.SetLeft(label, canvasX + pointSize / 2 + 5);
                Canvas.SetTop(label, canvasY - 10);

                canvas.Children.Add(label);
            }
        }

        #endregion

        #region 공개 메서드

        /// <summary>
        /// 외부에서 데이터를 추가할 때 사용
        /// </summary>
        public void AddMeasurementData(double l, double x, double y, double u, double v, double current, double efficiency)
        {
            var newItem = new ManualDataItem
            {
                Num = dataItems.Count + 1,
                L = l,
                X = x,
                Y = y,
                U = u,
                V = v,
                Current = current,
                Efficiency = efficiency
            };

            dataItems.Add(newItem);
            
            // 최신 데이터로 색좌표 업데이트
            UpdateColorCoordinates(newItem);
        }

        /// <summary>
        /// 데이터 테이블 초기화
        /// </summary>
        public void ClearData()
        {
            dataItems.Clear();
            ResetColorCoordinates();
        }

        #endregion
    }

    /// <summary>
    /// Manual 페이지 데이터 항목 모델
    /// </summary>
    public class ManualDataItem
    {
        public int Num { get; set; }
        
        private double _l;
        public double L 
        { 
            get => _l; 
            set => _l = Math.Round(value, 5); 
        }
        
        private double _x;
        public double X 
        { 
            get => _x; 
            set => _x = Math.Round(value, 5); 
        }
        
        private double _y;
        public double Y 
        { 
            get => _y; 
            set => _y = Math.Round(value, 5); 
        }
        
        private double _u;
        public double U 
        { 
            get => _u; 
            set => _u = Math.Round(value, 5); 
        }
        
        private double _v;
        public double V 
        { 
            get => _v; 
            set => _v = Math.Round(value, 5); 
        }
        
        private double _current;
        public double Current 
        { 
            get => _current; 
            set => _current = Math.Round(value, 5); 
        }
        
        private double _efficiency;
        public double Efficiency 
        { 
            get => _efficiency; 
            set => _efficiency = Math.Round(value, 5); 
        }
    }
}
