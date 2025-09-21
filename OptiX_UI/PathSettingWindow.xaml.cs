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
        public string RecipePath { get; private set; } = "";
        public string DLLPath { get; private set; } = "";

        private IniFileManager iniManager;
        private bool isDarkMode = false;
        private string iniSection = "MTP_PATHS"; // 기본값은 MTP_PATHS

        public PathSettingWindow(string section = "MTP_PATHS", bool darkMode = false)
        {
            InitializeComponent();
            iniSection = section;
            isDarkMode = darkMode; // 메인 프로그램의 테마 상태를 받아옴
            InitializeIniManager();
            LoadExistingPaths();
            ApplyTheme(); // LoadThemeFromIni() 대신 바로 ApplyTheme() 호출
        }

        private void InitializeIniManager()
        {
            string iniPath = @"D:\OptiX\Recipe\OptiX.ini";
            iniManager = new IniFileManager(iniPath);
        }

        private void LoadExistingPaths()
        {
            try
            {
                // INI 파일에서 기존 경로들을 로드 (지정된 섹션에서)
                EECPPath = iniManager.ReadValue(iniSection, "EECP_FOLDER", "");
                CIMPath = iniManager.ReadValue(iniSection, "CIM_FOLDER", "");
                RecipePath = iniManager.ReadValue(iniSection, "RECIPE_FOLDER", "");
                DLLPath = iniManager.ReadValue(iniSection, "DLL_FOLDER", "");

                // 텍스트박스에 표시
                EECPTextBox.Text = string.IsNullOrEmpty(EECPPath) ? "EECP 폴더를 선택하세요" : EECPPath;
                CIMTextBox.Text = string.IsNullOrEmpty(CIMPath) ? "CIM 폴더를 선택하세요" : CIMPath;
                RecipeTextBox.Text = string.IsNullOrEmpty(RecipePath) ? "Recipe 파일을 선택하세요" : RecipePath;
                DLLTextBox.Text = string.IsNullOrEmpty(DLLPath) ? "DLL 폴더를 선택하세요" : DLLPath;
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

        private void RecipeButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Recipe 파일을 선택하세요",
                Filter = "Recipe 파일 (*.ini)|*.ini|모든 파일 (*.*)|*.*",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                RecipePath = dialog.FileName;
                RecipeTextBox.Text = RecipePath;
            }
        }

        private void DLLButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "DLL 파일들이 있는 폴더를 선택하세요",
                Filter = "폴더 선택|*.",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "폴더를 선택하세요"
            };

            if (dialog.ShowDialog() == true)
            {
                DLLPath = Path.GetDirectoryName(dialog.FileName) ?? "";
                DLLTextBox.Text = DLLPath;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 경로들을 INI 파일에 저장
                SavePathsToIni();
                
                MessageBox.Show("경로가 성공적으로 저장되었습니다.", "저장 완료", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
                
                this.DialogResult = true;
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
                iniManager.WriteValue(iniSection, "RECIPE_FOLDER", RecipePath);
                iniManager.WriteValue(iniSection, "DLL_FOLDER", DLLPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"INI 파일 저장 실패: {ex.Message}");
            }
        }
    }
}
