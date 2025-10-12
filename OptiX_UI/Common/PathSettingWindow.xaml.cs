using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace OptiX.Common
{
    public partial class PathSettingWindow : Window
    {
        public string EECPPath { get; private set; } = "";
        public string CIMPath { get; private set; } = "";
        public string VALIDPath { get; private set; } = "";
        public string SequencePath { get; private set; } = "";

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
            
            LoadExistingPaths();
            LoadFileGenerationSettings();
            ApplyTheme(); // LoadThemeFromIni() 대신 바로 ApplyTheme() 호출
            ApplyLanguage(); // 언어 적용
        }

        private void LoadExistingPaths()
        {
            try
            {
                // INI 파일에서 기존 경로들을 로드 (지정된 섹션에서)
                EECPPath = GlobalDataManager.GetValue(iniSection, "EECP_FOLDER", "");
                CIMPath = GlobalDataManager.GetValue(iniSection, "CIM_FOLDER", "");
                VALIDPath = GlobalDataManager.GetValue(iniSection, "VALID_FOLDER", "");
                SequencePath = GlobalDataManager.GetValue(iniSection, "SEQUENCE_FOLDER", "");

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
                string darkModeStr = GlobalDataManager.GetValue("Theme", "IsDarkMode", "F");
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
                ThemeManager.UpdateDynamicColors(this, true);
            }
            else
            {
                // 라이트모드 색상 적용
                ThemeManager.UpdateDynamicColors(this, false);
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
                
                // INI 파일 저장 후 전역 캐시 갱신
                GlobalDataManager.Reload();
                
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
                GlobalDataManager.SetValue(iniSection, "EECP_FOLDER", EECPPath);
                GlobalDataManager.SetValue(iniSection, "CIM_FOLDER", CIMPath);
                GlobalDataManager.SetValue(iniSection, "VALID_FOLDER", VALIDPath);
                GlobalDataManager.SetValue(iniSection, "SEQUENCE_FOLDER", SequencePath);
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
                bool isEecpEnabled = GlobalDataManager.GetValue(targetSection, "CREATE_EECP", "F").ToUpper() == "T";
                bool isCimEnabled = GlobalDataManager.GetValue(targetSection, "CREATE_CIM", "F").ToUpper() == "T";
                bool isEecpSummaryEnabled = GlobalDataManager.GetValue(targetSection, "CREATE_EECP_SUMMARY", "F").ToUpper() == "T";
                bool isValidationEnabled = GlobalDataManager.GetValue(targetSection, "CREATE_VALIDATION", "F").ToUpper() == "T";

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
                
                GlobalDataManager.SetValue(targetSection, "CREATE_EECP", eecpValue);
                GlobalDataManager.SetValue(targetSection, "CREATE_CIM", cimValue);
                GlobalDataManager.SetValue(targetSection, "CREATE_EECP_SUMMARY", eecpSummaryValue);
                GlobalDataManager.SetValue(targetSection, "CREATE_VALIDATION", validationValue);
                
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
