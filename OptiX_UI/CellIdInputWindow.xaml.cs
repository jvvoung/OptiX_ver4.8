using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OptiX
{
    /// <summary>
    /// CellIdInputWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CellIdInputWindow : Window
    {
        private int zoneNumber;
        private IniFileManager iniManager;
        private bool isDarkMode;
        private bool isCimEnabled = false;
        private bool isEecpEnabled = false;
        private Dictionary<string, TextBox> dynamicMeasTextBoxes = new Dictionary<string, TextBox>();

        public CellIdInputWindow() : this(1)
        {
        }

        public CellIdInputWindow(int zoneNumber) : this(zoneNumber, false)
        {
        }

        public CellIdInputWindow(int zoneNumber, bool isDarkMode)
        {
            InitializeComponent();
            this.zoneNumber = zoneNumber;
            this.isDarkMode = isDarkMode;
            this.iniManager = new IniFileManager(@"D:\OptiX\Recipe\OptiX.ini");
            this.Title = $"Zone {zoneNumber} OPTIC 설정";
            
            // 다크모드 적용
            ApplyTheme();
            LoadCurrentValues();
            
            // 추가 초기화는 UI가 로드된 후 실행
            this.Loaded += (s, e) => {
                try 
                {
                   LoadFileGenerationSettings();
                   LoadTcpIpSettings();
                   CreateDynamicMeasPorts();
                   LoadPortSettings();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"추가 초기화 오류: {ex.Message}");
                }
            };
        }

        private void ApplyTheme()
        {
            if (isDarkMode)
            {
                // 다크모드 색상 적용
                this.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42));
                
                // 동적 리소스 업데이트 - 다크모드
                Resources["DynamicWindowBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)); // #0F172A
                Resources["DynamicCardBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59)); // #1E293B
                Resources["DynamicBorderColor"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105)); // #475569
                Resources["DynamicTextColor"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249)); // #F1F5F9
                Resources["DynamicInputBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85)); // #334155
                Resources["DynamicInputBorder"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105)); // #475569
                Resources["DynamicSecondaryButtonBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85)); // #334155
                Resources["DynamicSecondaryButtonText"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225)); // #CBD5E1
            }
            else
            {
                // 라이트모드 색상 적용 (기본값)
                this.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252));
                
                // 동적 리소스 업데이트 - 라이트모드
                Resources["DynamicWindowBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)); // #F8FAFC
                Resources["DynamicCardBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)); // #FFFFFF
                Resources["DynamicBorderColor"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)); // #E2E8F0
                Resources["DynamicTextColor"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59)); // #1E293B
                Resources["DynamicInputBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)); // #FFFFFF
                Resources["DynamicInputBorder"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 213, 219)); // #D1D5DB
                Resources["DynamicSecondaryButtonBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 244, 246)); // #F3F4F6
                Resources["DynamicSecondaryButtonText"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(55, 65, 81)); // #374151
            }
        }

        private void LoadCurrentValues()
        {
            try
            {
                string cellIdKey = $"CELL_ID_ZONE_{zoneNumber}";
                string innerIdKey = $"INNER_ID_ZONE_{zoneNumber}";

                string currentCellId = iniManager.ReadValue("MTP_PATHS", cellIdKey, "");
                string currentInnerId = iniManager.ReadValue("MTP_PATHS", innerIdKey, "");

                CellIdTextBox.Text = currentCellId;
                InnerIdTextBox.Text = currentInnerId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"기존 값 로드 오류: {ex.Message}");
            }
        }

        private void LoadFileGenerationSettings()
        {
            try
            {
                // INI 파일에서 CREATE_CIM, CREATE_EECP 값 읽어오기
                string cimValue = iniManager.ReadValue("MTP", "CREATE_CIM", "F");
                string eecpValue = iniManager.ReadValue("MTP", "CREATE_EECP", "F");
                
                // T/F 값을 bool로 변환
                isCimEnabled = (cimValue.ToUpper() == "T");
                isEecpEnabled = (eecpValue.ToUpper() == "T");
                
                // 체크박스에 값 설정
                CimCheckBox.IsChecked = isCimEnabled;
                EecpCheckBox.IsChecked = isEecpEnabled;
                
                // 체크박스 이벤트 연결
                CimCheckBox.Checked += (s, e) => isCimEnabled = true;
                CimCheckBox.Unchecked += (s, e) => isCimEnabled = false;
                EecpCheckBox.Checked += (s, e) => isEecpEnabled = true;
                EecpCheckBox.Unchecked += (s, e) => isEecpEnabled = false;
                
                System.Diagnostics.Debug.WriteLine($"파일 생성 설정 로드 완료 - CIM: {isCimEnabled} ({cimValue}), EECP: {isEecpEnabled} ({eecpValue})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"파일 생성 설정 로드 오류: {ex.Message}");
                // 오류 시 기본값 설정
                isCimEnabled = false;
                isEecpEnabled = false;
                CimCheckBox.IsChecked = false;
                EecpCheckBox.IsChecked = false;
            }
        }

        private void LoadTcpIpSettings()
        {
            try
            {
                string tcpIp = iniManager.ReadValue("MTP", "TCP_IP", "2002");
                TcpIpTextBox.Text = tcpIp;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TCP/IP 설정 로드 오류: {ex.Message}");
            }
        }

        private void LoadPortSettings()
        {
            try
            {
                // PG 포트 로드
                string pgKey = $"PG_PORT_{zoneNumber}";
                PgPortTextBox.Text = iniManager.ReadValue("MTP", pgKey, "");

                // 동적 MEAS 포트들 로드
                foreach (var kvp in dynamicMeasTextBoxes)
                {
                    string measKey = kvp.Key;
                    TextBox textBox = kvp.Value;
                    string value = iniManager.ReadValue("MTP", measKey, "");
                    textBox.Text = value;
                }

                System.Diagnostics.Debug.WriteLine($"포트 설정 로드 완료: PG + {dynamicMeasTextBoxes.Count}개 MEAS 포트");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"포트 설정 로드 오류: {ex.Message}");
            }
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Cell 정보 저장
                SaveCellInformation();
                
                // 파일 생성 여부 저장 (현재는 메모리 변수로만 저장)
                SaveFileGenerationSettings();
                
                // TCP/IP 설정 저장
                SaveTcpIpSettings();
                
                // 포트 설정 저장
                SavePortSettings();

                MessageBox.Show("모든 설정이 성공적으로 저장되었습니다.", "저장 완료", 
                              MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 중 오류가 발생했습니다: {ex.Message}", "오류", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveCellInformation()
        {
            string cellId = CellIdTextBox.Text.Trim();
            string innerId = InnerIdTextBox.Text.Trim();

            string cellIdKey = $"CELL_ID_ZONE_{zoneNumber}";
            string innerIdKey = $"INNER_ID_ZONE_{zoneNumber}";

            iniManager.WriteValue("MTP_PATHS", cellIdKey, cellId);
            iniManager.WriteValue("MTP_PATHS", innerIdKey, innerId);

            System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} Cell 정보 저장됨 - Cell ID: {cellId}, Inner ID: {innerId}");
        }

        private void SaveFileGenerationSettings()
        {
            try
            {
                // T/F 형태로 INI 파일에 저장
                string cimValue = isCimEnabled ? "T" : "F";
                string eecpValue = isEecpEnabled ? "T" : "F";
                
                iniManager.WriteValue("MTP", "CREATE_CIM", cimValue);
                iniManager.WriteValue("MTP", "CREATE_EECP", eecpValue);
                
                System.Diagnostics.Debug.WriteLine($"파일 생성 설정 저장 완료 - CREATE_CIM: {cimValue}, CREATE_EECP: {eecpValue}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"파일 생성 설정 저장 오류: {ex.Message}");
                throw; // 저장 실패 시 상위로 예외 전달
            }
        }

        private void SaveTcpIpSettings()
        {
            string tcpIp = TcpIpTextBox.Text.Trim();
            iniManager.WriteValue("MTP", "TCP_IP", tcpIp);
            System.Diagnostics.Debug.WriteLine($"TCP/IP 설정 저장됨: {tcpIp}");
        }

        private void SavePortSettings()
        {
            try
            {
                // PG 포트 저장
                string pgKey = $"PG_PORT_{zoneNumber}";
                iniManager.WriteValue("MTP", pgKey, PgPortTextBox.Text.Trim());

                // 동적 MEAS 포트들 저장
                foreach (var kvp in dynamicMeasTextBoxes)
                {
                    string measKey = kvp.Key;
                    TextBox textBox = kvp.Value;
                    iniManager.WriteValue("MTP", measKey, textBox.Text.Trim());
                }

                System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} 포트 설정 저장 완료: PG + {dynamicMeasTextBoxes.Count}개 MEAS 포트");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"포트 설정 저장 오류: {ex.Message}");
            }
        }

        private void CreateDynamicMeasPorts()
        {
            try
            {
                // WAD 값을 읽어서 동적으로 MEAS 포트 생성
                string wadValues = iniManager.ReadValue("MTP", "WAD", "0,15,30,45,60");
                string[] wadNumbers = wadValues.Split(',');

                MeasPortsStackPanel.Children.Clear();
                dynamicMeasTextBoxes.Clear();

                foreach (string wadValue in wadNumbers)
                {
                    string trimmedValue = wadValue.Trim();
                    if (string.IsNullOrEmpty(trimmedValue)) continue;

                    // MEAS 포트 이름 결정 (0이면 "MEAS", 나머지는 "MEAS_숫자")
                    string measName = trimmedValue == "0" ? "MEAS" : $"MEAS_{trimmedValue}";
                    string measKey = trimmedValue == "0" ? $"MEAS_PORT_{zoneNumber}" : $"MEAS_PORT_{trimmedValue}_{zoneNumber}";

                    // Grid 생성
                    Grid measGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                    measGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                    measGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    // 라벨 생성
                    TextBlock label = new TextBlock
                    {
                        Text = measName,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontWeight = FontWeights.Bold,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B5CF6")),
                        Foreground = Brushes.White,
                        Padding = new Thickness(8, 4, 8, 4)
                    };
                    Grid.SetColumn(label, 0);

                    // TextBox 생성
                    TextBox textBox = new TextBox
                    {
                        Name = $"Dynamic{measName.Replace("_", "")}PortTextBox",
                        Margin = new Thickness(4, 0, 0, 0)
                    };

                    // PortTextBoxStyle 적용
                    try
                    {
                        Style portStyle = (Style)this.FindResource("PortTextBoxStyle");
                        textBox.Style = portStyle;
                    }
                    catch
                    {
                        // 스타일 적용 실패 시 기본 설정
                        textBox.FontSize = 12;
                        textBox.Height = 32;
                        textBox.Padding = new Thickness(8, 4, 8, 4);
                    }

                    Grid.SetColumn(textBox, 1);

                    // Grid에 추가
                    measGrid.Children.Add(label);
                    measGrid.Children.Add(textBox);

                    // StackPanel에 추가
                    MeasPortsStackPanel.Children.Add(measGrid);

                    // Dictionary에 저장
                    dynamicMeasTextBoxes[measKey] = textBox;
                }

                System.Diagnostics.Debug.WriteLine($"동적 MEAS 포트 생성 완료: {dynamicMeasTextBoxes.Count}개");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"동적 MEAS 포트 생성 오류: {ex.Message}");
            }
        }

        // 누락된 이벤트 핸들러 추가
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}

