using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OptiX.Models;

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
        private string iniSection;
        private bool isCimEnabled = false;
        private bool isEecpEnabled = false;
        private bool isEecpSummaryEnabled = false;
        private bool isValidationEnabled = false;
        private Dictionary<string, TextBox> dynamicMeasTextBoxes = new Dictionary<string, TextBox>();

        public CellIdInputWindow() : this(1)
        {
        }

        public CellIdInputWindow(int zoneNumber) : this(zoneNumber, false)
        {
        }

        public CellIdInputWindow(int zoneNumber, bool isDarkMode) : this(zoneNumber, isDarkMode, "MTP")
        {
        }

        public CellIdInputWindow(int zoneNumber, bool isDarkMode, string iniSection)
        {
            InitializeComponent();
            this.zoneNumber = zoneNumber;
            this.isDarkMode = isDarkMode;
            this.iniSection = iniSection;
            
            // 실행 파일 기준 상대 경로로 INI 파일 찾기
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDir = System.IO.Path.GetDirectoryName(exePath);
            string iniPath = @"D:\Project\Recipe\OptiX.ini";
            this.iniManager = new IniFileManager(iniPath);
            
            // INI 섹션에 따라 창 제목 설정
            string inspectionType = iniSection == "IPVS" ? "IPVS" : "OPTIC";
            this.Title = $"Zone {zoneNumber} {inspectionType} SETTING";
            
            // 다크모드 적용
            ApplyTheme();
            
            // 언어 적용
            ApplyLanguage();
            LoadCurrentValues();
            
            // 추가 초기화는 UI가 로드된 후 실행
            this.Loaded += (s, e) => {
                try 
                {
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
                // 구조체 기반으로 데이터 로드
                InspectionData data;
                if (iniSection == "IPVS")
                {
                    IPVSDataManager.LoadFromIni(iniManager);
                    data = IPVSDataManager.GetData(zoneNumber);
                }
                else
                {
                    MTPDataManager.LoadFromIni(iniManager);
                    data = MTPDataManager.GetData(zoneNumber);
                }

                // 텍스트 설정
                CellIdTextBox.Text = string.IsNullOrEmpty(data.CellId) ? "-" : data.CellId;
                InnerIdTextBox.Text = string.IsNullOrEmpty(data.InnerId) ? "..." : data.InnerId;
                
                // 텍스트박스 포커스 설정
                CellIdTextBox.CaretIndex = CellIdTextBox.Text.Length;
                InnerIdTextBox.CaretIndex = InnerIdTextBox.Text.Length;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"기존 값 로드 오류: {ex.Message}");
            }
        }



        private void LoadPortSettings()
        {
            try
            {
                // PG 포트 로드 (OPTIC은 MTP 섹션, IPVS는 IPVS 섹션)
                string pgKey = $"PG_PORT_{zoneNumber}";
                PgPortTextBox.Text = iniManager.ReadValue(iniSection, pgKey, "");

                // 동적 MEAS 포트들 로드 (OPTIC은 MTP 섹션, IPVS는 IPVS 섹션)
                foreach (var kvp in dynamicMeasTextBoxes)
                {
                    string measKey = kvp.Key;
                    TextBox textBox = kvp.Value;
                    string measSection = (iniSection == "IPVS") ? "IPVS" : "MTP";
                    string value = iniManager.ReadValue(measSection, measKey, "");
                    textBox.Text = value;
                }

                string measSectionForLog = (iniSection == "IPVS") ? "IPVS" : "MTP";
                System.Diagnostics.Debug.WriteLine($"포트 설정 로드 완료: PG({iniSection}) + {dynamicMeasTextBoxes.Count}개 MEAS({measSectionForLog}) 포트");
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
                
                // 포트 설정 저장
                SavePortSettings();

                // 전역 데이터 다시 로드
                GlobalDataManager.ReloadIniData();

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

        private void SaveCellInformation()
        {
            string cellId = CellIdTextBox.Text.Trim();
            string innerId = InnerIdTextBox.Text.Trim();

            // 구조체 기반으로 데이터 저장
            InspectionData data;
            if (iniSection == "IPVS")
            {
                data = IPVSDataManager.GetData(zoneNumber);
                data.CellId = cellId;
                data.InnerId = innerId;
                IPVSDataManager.SaveToIni(iniManager, zoneNumber, data);
            }
            else
            {
                data = MTPDataManager.GetData(zoneNumber);
                data.CellId = cellId;
                data.InnerId = innerId;
                MTPDataManager.SaveToIni(iniManager, zoneNumber, data);
            }

            System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} Cell 정보 저장됨 - Cell ID: {cellId}, Inner ID: {innerId}");
        }

        private void SaveFileGenerationSettings()
        {
            try
            {
                // T/F 형태로 INI 파일에 저장
                string cimValue = isCimEnabled ? "T" : "F";
                string eecpValue = isEecpEnabled ? "T" : "F";
                string eecpSummaryValue = isEecpSummaryEnabled ? "T" : "F";
                string validationValue = isValidationEnabled ? "T" : "F";
                
                iniManager.WriteValue(iniSection, "CREATE_CIM", cimValue);
                iniManager.WriteValue(iniSection, "CREATE_EECP", eecpValue);
                iniManager.WriteValue(iniSection, "CREATE_EECP_SUMMARY", eecpSummaryValue);
                iniManager.WriteValue(iniSection, "CREATE_VALIDATION", validationValue);
                
                System.Diagnostics.Debug.WriteLine($"파일 생성 설정 저장 완료 - CIM: {cimValue}, EECP: {eecpValue}, EECP_SUMMARY: {eecpSummaryValue}, VALIDATION: {validationValue}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"파일 생성 설정 저장 오류: {ex.Message}");
                throw; // 저장 실패 시 상위로 예외 전달
            }
        }


        private void SavePortSettings()
        {
            try
            {
                // 구조체 기반으로 데이터 저장
                InspectionData data;
                if (iniSection == "IPVS")
                {
                    data = IPVSDataManager.GetData(zoneNumber);
                    data.PgPort = PgPortTextBox.Text.Trim();
                    
                    // 동적 MEAS 포트들 업데이트
                    foreach (var kvp in dynamicMeasTextBoxes)
                    {
                        data.MeasPorts[kvp.Key] = kvp.Value.Text.Trim();
                    }
                    
                    IPVSDataManager.SaveToIni(iniManager, zoneNumber, data);
                }
                else
                {
                    data = MTPDataManager.GetData(zoneNumber);
                    data.PgPort = PgPortTextBox.Text.Trim();
                    
                    // 동적 MEAS 포트들 업데이트
                    foreach (var kvp in dynamicMeasTextBoxes)
                    {
                        data.MeasPorts[kvp.Key] = kvp.Value.Text.Trim();
                    }
                    
                    MTPDataManager.SaveToIni(iniManager, zoneNumber, data);
                }

                System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} 포트 설정 저장 완료: PG({iniSection}) + {dynamicMeasTextBoxes.Count}개 MEAS(MTP) 포트");
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
                // INI 섹션에 따라 WAD 값을 읽어서 동적으로 MEAS 포트 생성
                string wadValues;
                if (iniSection == "IPVS")
                {
                    wadValues = iniManager.ReadValue("IPVS", "WAD", "0,30,60,90,120");
                }
                else
                {
                    wadValues = iniManager.ReadValue("MTP", "WAD", "0,15,30,45,60");
                }
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

        // Connect 버튼 이벤트 핸들러
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Port 연결 로직 구현
                System.Windows.MessageBox.Show("Port 연결 기능이 구현되었습니다!", "Connect", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                // 여기에 실제 연결 로직을 추가할 수 있습니다
                // 예: TCP/IP 연결, 시리얼 포트 연결 등
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"연결 중 오류가 발생했습니다: {ex.Message}", "연결 오류", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // 언어 적용 메서드
        public void ApplyLanguage()
        {
            try
            {
                // 창 제목을 INI 섹션에 따라 동적으로 설정
                string inspectionType = iniSection == "IPVS" ? "IPVS" : "OPTIC";
                this.Title = $"Zone {zoneNumber} {inspectionType} SETTING";
                
                // Cell 정보 제목
                if (CellInfoTitle != null)
                    CellInfoTitle.Text = LanguageManager.GetText("CellIdInput.CellInfo");
                
                
                // Port 연결 제목
                if (PortConnectionTitle != null)
                    PortConnectionTitle.Text = LanguageManager.GetText("CellIdInput.PortConnection");
                
                // 취소 버튼
                if (CancelButton != null)
                    CancelButton.Content = LanguageManager.GetText("CellIdInput.Cancel");
                
                System.Diagnostics.Debug.WriteLine($"CellIdInputWindow 언어 적용 완료: {LanguageManager.CurrentLanguage}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CellIdInputWindow 언어 적용 오류: {ex.Message}");
            }
        }

        // 누락된 이벤트 핸들러 추가
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Non-Modal 창에서는 DialogResult 사용하지 않음
            this.Close();
        }
    }
}

