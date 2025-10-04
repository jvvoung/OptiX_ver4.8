using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace OptiX
{
    public partial class PathSettingWindow : Window
    {
        public string EECPPath { get; private set; } = "";
        public string CIMPath { get; private set; } = "";
        public string VALIDPath { get; private set; } = "";
        public string SequencePath { get; private set; } = "";

        private IniFileManager iniManager;
        private bool isDarkMode = false;
        private string iniSection = "MTP_PATHS"; // 기본값은 MTP_PATHS

        public PathSettingWindow(string section = "MTP_PATHS", bool darkMode = false)
        {
            InitializeComponent();
            iniSection = section;
            isDarkMode = darkMode; // 메인 프로그램의 테마 상태를 받아옴
            
            // INI 섹션에 따라 창 제목 설정
            string inspectionType = iniSection == "IPVS_PATHS" ? "IPVS" : "OPTIC";
            this.Title = $"{inspectionType} Path Settings";
            
            InitializeIniManager();
            LoadExistingPaths();
            LoadFileGenerationSettings();
            ApplyTheme(); // LoadThemeFromIni() 대신 바로 ApplyTheme() 호출
            ApplyLanguage(); // 언어 적용
        }

        private void InitializeIniManager()
        {
            // 실행 파일 기준 상대 경로로 INI 파일 찾기
        string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string exeDir = System.IO.Path.GetDirectoryName(exePath);
        string iniPath = @"D:\\Project\\Recipe\\OptiX.ini";
            iniManager = new IniFileManager(iniPath);
        }

        private void LoadExistingPaths()
        {
            try
            {
                // INI 파일에서 기존 경로들을 로드 (지정된 섹션에서)
                EECPPath = iniManager.ReadValue(iniSection, "EECP_FOLDER", "");
                CIMPath = iniManager.ReadValue(iniSection, "CIM_FOLDER", "");
                VALIDPath = iniManager.ReadValue(iniSection, "VALID_FOLDER", "");
                SequencePath = iniManager.ReadValue(iniSection, "SEQUENCE_FOLDER", "");

                // 텍스트박스에 표시
                EECPTextBox.Text = string.IsNullOrEmpty(EECPPath) ? "EECP 폴더를 선택하세요" : EECPPath;
                CIMTextBox.Text = string.IsNullOrEmpty(CIMPath) ? "CIM 폴더를 선택하세요" : CIMPath;
                VALIDTextBox.Text = string.IsNullOrEmpty(VALIDPath) ? "VALID 폴더를 선택하세요" : VALIDPath;
                SequenceTextBox.Text = string.IsNullOrEmpty(SequencePath) ? "Sequence 파일을 선택하세요" : SequencePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"기존 경로를 로드하는 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadThemeFromIni()
        {
            try
            {
                string darkModeStr = iniManager.ReadValue("Theme", "IsDarkMode", "F");
                isDarkMode = darkModeStr.ToUpper() == "T";
                ApplyTheme();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"테마 설정을 읽는 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        private void ApplyTheme()
        {
            if (isDarkMode)
            {
                // 다크모드 색상 적용
                UpdateDynamicColors(true);
            }
            else
            {
                // 라이트모드 색상 적용
                UpdateDynamicColors(false);
            }
        }

        private void UpdateDynamicColors(bool isDark)
        {
            // 동적 색상 팔레트 업데이트
            if (isDark)
            {
                // 다크모드 색상으로 변경
                Resources["DynamicBackgroundColor"] = new SolidColorBrush(Color.FromRgb(15, 23, 42)); // #0F172A
                Resources["DynamicSurfaceColor"] = new SolidColorBrush(Color.FromRgb(30, 41, 59)); // #1E293B
                Resources["DynamicCardColor"] = new SolidColorBrush(Color.FromRgb(51, 65, 85)); // #334155
                Resources["DynamicBorderColor"] = new SolidColorBrush(Color.FromRgb(71, 85, 105)); // #475569
                Resources["DynamicTextPrimaryColor"] = new SolidColorBrush(Color.FromRgb(241, 245, 249)); // #F1F5F9
                Resources["DynamicTextSecondaryColor"] = new SolidColorBrush(Color.FromRgb(203, 213, 225)); // #CBD5E1
                Resources["DynamicTextMutedColor"] = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // #94A3B8
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
            }
        }

        private void EECPButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "EECP 파일이 생성될 폴더를 선택하세요",
                Filter = "폴더 선택|*.",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "폴더를 선택하세요"
            };

            if (dialog.ShowDialog() == true)
            {
                EECPPath = Path.GetDirectoryName(dialog.FileName) ?? "";
                EECPTextBox.Text = EECPPath;
            }
        }

        private void CIMButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "CIM 파일이 생성될 폴더를 선택하세요",
                Filter = "폴더 선택|*.",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "폴더를 선택하세요"
            };

            if (dialog.ShowDialog() == true)
            {
                CIMPath = Path.GetDirectoryName(dialog.FileName) ?? "";
                CIMTextBox.Text = CIMPath;
            }
        }

        private void VALIDButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "VALID 폴더를 선택하세요",
                Filter = "폴더 선택|*.",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "폴더를 선택하세요"
            };

            if (dialog.ShowDialog() == true)
            {
                VALIDPath = Path.GetDirectoryName(dialog.FileName) ?? "";
                VALIDTextBox.Text = VALIDPath;
            }
        }

        private void SequenceButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Sequence 파일을 선택하세요",
                Filter = "Sequence 파일 (*.ini)|*.ini|모든 파일 (*.*)|*.*",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                SequencePath = dialog.FileName;
                SequenceTextBox.Text = SequencePath;
            }
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 경로들을 INI 파일에 저장
                SavePathsToIni();
                SaveFileGenerationSettings();
                
                // 전역 데이터 다시 로드
                GlobalDataManager.ReloadIniData();
                
                MessageBox.Show("경로가 성공적으로 저장되었습니다.", "저장 완료", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Non-Modal 창에서는 DialogResult 사용하지 않음
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"경로 저장 중 오류가 발생했습니다: {ex.Message}", "오류", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePathsToIni()
        {
            try
            {
                // IniFileManager를 사용하여 경로 저장 (지정된 섹션에)
                iniManager.WriteValue(iniSection, "EECP_FOLDER", EECPPath);
                iniManager.WriteValue(iniSection, "CIM_FOLDER", CIMPath);
                iniManager.WriteValue(iniSection, "VALID_FOLDER", VALIDPath);
                iniManager.WriteValue(iniSection, "SEQUENCE_FOLDER", SequencePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"INI 파일 저장 실패: {ex.Message}");
            }
        }

        private void LoadFileGenerationSettings()
        {
            try
            {
                // iniSection에 따라 MTP 또는 IPVS 섹션 사용
                string targetSection = (iniSection == "IPVS_PATHS") ? "IPVS" : "MTP";
                
                // 파일 생성 여부 설정 로드 (MTP/IPVS 섹션에서 읽기)
                bool isEecpEnabled = iniManager.ReadValue(targetSection, "CREATE_EECP", "F").ToUpper() == "T";
                bool isCimEnabled = iniManager.ReadValue(targetSection, "CREATE_CIM", "F").ToUpper() == "T";
                bool isEecpSummaryEnabled = iniManager.ReadValue(targetSection, "CREATE_EECP_SUMMARY", "F").ToUpper() == "T";
                bool isValidationEnabled = iniManager.ReadValue(targetSection, "CREATE_VALIDATION", "F").ToUpper() == "T";

                // 체크박스 상태 설정
                if (CreateEecpCheckBox != null) CreateEecpCheckBox.IsChecked = isEecpEnabled;
                if (CreateCimCheckBox != null) CreateCimCheckBox.IsChecked = isCimEnabled;
                if (CreateEecpSummaryCheckBox != null) CreateEecpSummaryCheckBox.IsChecked = isEecpSummaryEnabled;
                if (CreateValidationCheckBox != null) CreateValidationCheckBox.IsChecked = isValidationEnabled;

                System.Diagnostics.Debug.WriteLine($"파일 생성 설정 로드 완료 ({targetSection}) - EECP: {isEecpEnabled}, CIM: {isCimEnabled}, EECP_SUMMARY: {isEecpSummaryEnabled}, VALIDATION: {isValidationEnabled}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"파일 생성 설정 로드 오류: {ex.Message}");
            }
        }

        private void SaveFileGenerationSettings()
        {
            try
            {
                // iniSection에 따라 MTP 또는 IPVS 섹션 사용
                string targetSection = (iniSection == "IPVS_PATHS") ? "IPVS" : "MTP";
                
                // 체크박스 상태 읽기
                bool isEecpEnabled = CreateEecpCheckBox?.IsChecked ?? false;
                bool isCimEnabled = CreateCimCheckBox?.IsChecked ?? false;
                bool isEecpSummaryEnabled = CreateEecpSummaryCheckBox?.IsChecked ?? false;
                bool isValidationEnabled = CreateValidationCheckBox?.IsChecked ?? false;

                // T/F 형태로 INI 파일에 저장 (MTP/IPVS 섹션에 저장)
                string eecpValue = isEecpEnabled ? "T" : "F";
                string cimValue = isCimEnabled ? "T" : "F";
                string eecpSummaryValue = isEecpSummaryEnabled ? "T" : "F";
                string validationValue = isValidationEnabled ? "T" : "F";
                
                iniManager.WriteValue(targetSection, "CREATE_EECP", eecpValue);
                iniManager.WriteValue(targetSection, "CREATE_CIM", cimValue);
                iniManager.WriteValue(targetSection, "CREATE_EECP_SUMMARY", eecpSummaryValue);
                iniManager.WriteValue(targetSection, "CREATE_VALIDATION", validationValue);
                
                System.Diagnostics.Debug.WriteLine($"파일 생성 설정 저장 완료 ({targetSection}) - EECP: {eecpValue}, CIM: {cimValue}, EECP_SUMMARY: {eecpSummaryValue}, VALIDATION: {validationValue}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"파일 생성 설정 저장 오류: {ex.Message}");
                throw; // 저장 실패 시 상위로 예외 전달
            }
        }

        // 언어 적용 메서드
        public void ApplyLanguage()
        {
            try
            {
                // 파일 생성 여부 제목
                if (FileGenerationTitle != null)
                    FileGenerationTitle.Text = LanguageManager.GetText("PathSettings.FileGenerationStatus");
                
                // 폴더 경로 설정 제목
                if (FolderPathSettingsTitle != null)
                    FolderPathSettingsTitle.Text = LanguageManager.GetText("PathSettings.FolderPathSettings");
                
                // 파일 경로 설정 제목
                if (FilePathSettingsTitle != null)
                    FilePathSettingsTitle.Text = LanguageManager.GetText("PathSettings.FilePathSettings");
                
                // 각 TextBox의 placeholder 텍스트 업데이트
                if (EECPTextBox != null)
                    EECPTextBox.Text = $"EECP {LanguageManager.GetText("PathSettings.SelectFolder")}";
                
                if (CIMTextBox != null)
                    CIMTextBox.Text = $"CIM {LanguageManager.GetText("PathSettings.SelectFolder")}";
                
                if (VALIDTextBox != null)
                    VALIDTextBox.Text = $"VALID {LanguageManager.GetText("PathSettings.SelectFolder")}";
                
                
                if (SequenceTextBox != null)
                    SequenceTextBox.Text = $"Seq. {LanguageManager.GetText("PathSettings.SelectFolder")}";
                
                System.Diagnostics.Debug.WriteLine($"PathSettingWindow 언어 적용 완료: {LanguageManager.CurrentLanguage}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PathSettingWindow 언어 적용 오류: {ex.Message}");
            }
        }
    }
}
