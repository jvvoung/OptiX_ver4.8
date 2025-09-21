using System;
using System.Windows;

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
            this.Title = $"Zone {zoneNumber} 설정";
            
            // 다크모드 적용
            ApplyTheme();
            LoadCurrentValues();
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

                // TextBox 이름은 실제 XAML에 맞게 수정
                CellIdTextBox.Text = currentCellId;
                InnerIdTextBox.Text = currentInnerId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"기존 값 로드 오류: {ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string cellId = CellIdTextBox.Text.Trim();
                string innerId = InnerIdTextBox.Text.Trim();

                string cellIdKey = $"CELL_ID_ZONE_{zoneNumber}";
                string innerIdKey = $"INNER_ID_ZONE_{zoneNumber}";

                iniManager.WriteValue("MTP_PATHS", cellIdKey, cellId);
                iniManager.WriteValue("MTP_PATHS", innerIdKey, innerId);

                System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} 저장됨 - Cell ID: {cellId}, Inner ID: {innerId}");

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
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

