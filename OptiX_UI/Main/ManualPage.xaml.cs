using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using OptiX.Common;
using OptiX.DLL;

namespace OptiX.Main
{
    /// <summary>
    /// ManualPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ManualPage : UserControl
    {
        private ObservableCollection<ManualDataItem> dataItems;
        private bool isCie1931Selected = true;
        private bool _isDarkMode = false;
        private ManualDataItem selectedDataItem = null; // 선택된 데이터 아이템
        
        // Connect 버튼 상태 관리
        private bool isPgConnected = false;
        private bool isMeasConnected = false;
        
        // 마지막 데이터 저장 (리사이즈 시 재그리기용)
        private ObservableCollection<ManualDataItem> _lastDataItems = null;

        // CIE1931 X축 미세 보정 (이미지 내 그리드 여백 보정)
        private const double CIE1931_X_SCALE = 1.12; // 우측으로 약 12% 확장
        private const double CIE1931_X_BIAS = 0.0;   // 필요시 소량 오프셋 추가

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
                bool success = DllFunctions.CallPGTurn(port);

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
                bool success = DllFunctions.CallPGPattern(pattern);

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
                bool success = DllFunctions.CallPGVoltagesnd(RV, GV, BV);

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
                bool success = DllFunctions.CallMeasTurn(port);

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
                // DLL 함수 호출하여 측정 데이터 가져오기
                var (measureData, success) = DllFunctions.CallGetdata();

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
                ThemeManager.UpdateDynamicColors(this, _isDarkMode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"테마 적용 오류: {ex.Message}");
            }
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
                // 같은 아이템을 다시 클릭한 경우 토글 (선택 해제)
                if (selectedDataItem == selectedItem)
                {
                    selectedDataItem = null;
                    // 이벤트 핸들러를 일시적으로 제거하여 무한 루프 방지
                    DataTableGrid.SelectionChanged -= DataTableGrid_SelectionChanged;
                    DataTableGrid.SelectedIndex = -1;
                    DataTableGrid.SelectionChanged += DataTableGrid_SelectionChanged;
                }
                else
                {
                    // 다른 아이템을 클릭한 경우 선택
                    selectedDataItem = selectedItem;
                }
            }
            else
            {
                selectedDataItem = null;
            }
            
            // 항상 모든 좌표를 업데이트하여 현재 selectedDataItem에 따라 강조 표시
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
            try
            {
                if (Cie1931Canvas == null)
                {
                    System.Diagnostics.Debug.WriteLine("Cie1931Canvas is null");
                    return;
                }
                
                Cie1931Canvas.Children.Clear();
                
                for (int i = 0; i < dataItems.Count; i++)
                {
                    var item = dataItems[i];
                    bool isSelected = (item == selectedDataItem);
                    AddPointToCanvas(Cie1931Canvas, item.X, item.Y, isSelected, item.Num);
                }
                
                // 마지막 데이터 저장 (리사이즈 시 재사용)
                _lastDataItems = new ObservableCollection<ManualDataItem>(dataItems);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCIE1931Coordinates 오류: {ex.Message}");
                MessageBox.Show($"CIE1931 좌표 업데이트 중 오류: {ex.Message}", "오류", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // CIE1976 좌표 업데이트 (u,v 값 사용)
        private void UpdateCIE1976Coordinates()
        {
            try
            {
                if (Cie1976Canvas == null)
                {
                    System.Diagnostics.Debug.WriteLine("Cie1976Canvas is null");
                    return;
                }
                
                Cie1976Canvas.Children.Clear();
                
                for (int i = 0; i < dataItems.Count; i++)
                {
                    var item = dataItems[i];
                    bool isSelected = (item == selectedDataItem);
                    AddPointToCanvas(Cie1976Canvas, item.U, item.V, isSelected, item.Num);
                }
                
                // 마지막 데이터 저장 (리사이즈 시 재사용)
                _lastDataItems = new ObservableCollection<ManualDataItem>(dataItems);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCIE1976Coordinates 오류: {ex.Message}");
                MessageBox.Show($"CIE1976 좌표 업데이트 중 오류: {ex.Message}", "오류", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Canvas에 점 추가
        private void AddPointToCanvas(Canvas canvas, double x, double y, bool isSelected, int pointNumber)
        {
            // CIE 다이어그램 좌표를 Canvas 좌표로 변환 (동적 크기 지원)
            // 실제 CIE1931 다이어그램 이미지: x(0-0.8), y(0-0.9)
            // 실제 CIE1976 다이어그램 이미지: u(0-0.6), v(0-0.6)
            
            // Canvas의 실제 크기 가져오기
            double canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : 300;
            double canvasHeight = canvas.ActualHeight > 0 ? canvas.ActualHeight : 300;
            
            // 여백 설정 (Canvas 크기에 비례)
            double margin = Math.Min(canvasWidth, canvasHeight) * 0.1; // 10% 여백
            if (margin < 10) margin = 10; // 최소 10px
            if (margin > 30) margin = 30; // 최대 30px
            
            // 실제 좌표 영역 계산
            double graphWidth = canvasWidth - 2 * margin;
            double graphHeight = canvasHeight - 2 * margin;
            
            double canvasX, canvasY;
            
            if (canvas == Cie1931Canvas)
            {
                // CIE1931: x(0-0.8), y(0-0.9)
                // X축 보정: 실제 이미지의 좌/우 내부 여백 차 때문에 약간 우측으로 당겨줌
                double normalizedX = x / 0.8; // 0~1
                normalizedX = normalizedX * CIE1931_X_SCALE + CIE1931_X_BIAS;
                normalizedX = Math.Max(0.0, Math.Min(1.0, normalizedX));
                canvasX = margin + normalizedX * graphWidth;
                // y 좌표 변환: 0.0~0.9 -> margin+graphHeight~margin (Y축 뒤집기)
                canvasY = margin + graphHeight - (y / 0.9) * graphHeight;
            }
            else
            {
                // CIE1976: u(0-0.6), v(0-0.6) -> 동적 Canvas 좌표
                // u 좌표 변환: 0.0~0.6 -> margin~margin+graphWidth
                canvasX = margin + (x / 0.6) * graphWidth;
                // v 좌표 변환: 0.0~0.6 -> margin+graphHeight~margin (Y축 뒤집기)
                canvasY = margin + graphHeight - (y / 0.6) * graphHeight;
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

            // 선택된 점은 맨 앞에 표시되도록 Z-Index 설정
            if (isSelected)
            {
                Panel.SetZIndex(ellipse, 1000); // 선택된 점은 최상위
            }
            else
            {
                Panel.SetZIndex(ellipse, 0); // 일반 점은 기본 레벨
            }

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

                // 라벨도 선택된 점과 함께 맨 앞에 표시
                Panel.SetZIndex(label, 1001); // 라벨은 점보다도 더 위에

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
    
    // Canvas 리사이즈 이벤트 핸들러 (ManualPage 클래스 내부)
    public partial class ManualPage : UserControl
    {
        // CIE Canvas 리사이즈 시 점들을 다시 그리기
        private void CieCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Canvas canvas && _lastDataItems != null && _lastDataItems.Count > 0)
            {
                canvas.Children.Clear();
                
                for (int i = 0; i < _lastDataItems.Count; i++)
                {
                    var item = _lastDataItems[i];
                    bool isSelected = (item == selectedDataItem);
                    
                    if (canvas == Cie1931Canvas)
                    {
                        AddPointToCanvas(Cie1931Canvas, item.X, item.Y, isSelected, item.Num);
                    }
                    else if (canvas == Cie1976Canvas)
                    {
                        AddPointToCanvas(Cie1976Canvas, item.U, item.V, isSelected, item.Num);
                    }
                }
            }
        }
    }
}
