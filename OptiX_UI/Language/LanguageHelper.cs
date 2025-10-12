using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OptiX
{
    /// <summary>
    /// 언어 적용을 위한 헬퍼 클래스
    /// LanguageManager와 UI Controls 사이의 연결 고리 역할
    /// </summary>
    public static class LanguageHelper
    {
        /// <summary>
        /// Button의 Content에 번역된 텍스트 적용
        /// </summary>
        /// <param name="parent">부모 컨트롤 (FindName을 호출할 대상)</param>
        /// <param name="controlName">컨트롤 이름</param>
        /// <param name="textKey">LanguageManager에서 가져올 키</param>
        public static void ApplyToButton(FrameworkElement parent, string controlName, string textKey)
        {
            try
            {
                var button = parent.FindName(controlName) as Button;
                if (button != null)
                {
                    button.Content = LanguageManager.GetText(textKey);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Button 언어 적용 오류 ({controlName}): {ex.Message}");
            }
        }

        /// <summary>
        /// TextBlock의 Text에 번역된 텍스트 적용
        /// </summary>
        /// <param name="parent">부모 컨트롤 (FindName을 호출할 대상)</param>
        /// <param name="controlName">컨트롤 이름</param>
        /// <param name="textKey">LanguageManager에서 가져올 키</param>
        public static void ApplyToTextBlock(FrameworkElement parent, string controlName, string textKey)
        {
            try
            {
                var textBlock = parent.FindName(controlName) as TextBlock;
                if (textBlock != null)
                {
                    textBlock.Text = LanguageManager.GetText(textKey);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TextBlock 언어 적용 오류 ({controlName}): {ex.Message}");
            }
        }

        /// <summary>
        /// Button 내부의 TextBlock에 번역된 텍스트 적용
        /// (BackButton처럼 Button.Content가 TextBlock인 경우)
        /// </summary>
        /// <param name="parent">부모 컨트롤</param>
        /// <param name="controlName">버튼 이름</param>
        /// <param name="textKey">LanguageManager에서 가져올 키</param>
        public static void ApplyToButtonWithTextBlock(FrameworkElement parent, string controlName, string textKey)
        {
            try
            {
                var button = parent.FindName(controlName) as Button;
                if (button != null)
                {
                    var textBlock = button.Content as TextBlock;
                    if (textBlock != null)
                    {
                        textBlock.Text = LanguageManager.GetText(textKey);
                    }
                    else
                    {
                        // TextBlock이 아니면 일반 Content로 설정
                        button.Content = LanguageManager.GetText(textKey);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Button with TextBlock 언어 적용 오류 ({controlName}): {ex.Message}");
            }
        }

        /// <summary>
        /// 여러 컨트롤에 한 번에 언어 적용
        /// </summary>
        /// <param name="parent">부모 컨트롤</param>
        /// <param name="mappings">컨트롤 이름 → 텍스트 키 매핑</param>
        public static void ApplyToButtons(FrameworkElement parent, params (string controlName, string textKey)[] mappings)
        {
            foreach (var (controlName, textKey) in mappings)
            {
                ApplyToButton(parent, controlName, textKey);
            }
        }

        /// <summary>
        /// 여러 TextBlock에 한 번에 언어 적용
        /// </summary>
        /// <param name="parent">부모 컨트롤</param>
        /// <param name="mappings">컨트롤 이름 → 텍스트 키 매핑</param>
        public static void ApplyToTextBlocks(FrameworkElement parent, params (string controlName, string textKey)[] mappings)
        {
            foreach (var (controlName, textKey) in mappings)
            {
                ApplyToTextBlock(parent, controlName, textKey);
            }
        }

        #region MainWindow 언어 적용 (MainWindow.xaml.cs에서 이동)

        /// <summary>
        /// MainWindow에 언어 적용
        /// </summary>
        public static void ApplyToMainWindow(Window mainWindow)
        {
            try
            {
                // 버튼 텍스트 업데이트
                ApplyToButtons(mainWindow,
                    ("CharacteristicsButton", "MainWindow.Characteristics"),
                    ("IPVSButton", "MainWindow.IPVS"),
                    ("ManualButton", "MainWindow.Manual"),
                    ("LUTButton", "MainWindow.LUT"),
                    ("SettingsButton", "MainWindow.Settings")
                );

                // 호버 툴팁 텍스트 업데이트
                UpdateMainWindowTooltipTexts(mainWindow);

                System.Diagnostics.Debug.WriteLine($"MainWindow 언어 적용 완료: {LanguageManager.CurrentLanguage}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow 언어 적용 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// MainWindow 호버 툴팁 텍스트 업데이트
        /// </summary>
        private static void UpdateMainWindowTooltipTexts(Window mainWindow)
        {
            try
            {
                // Characteristics 툴팁 업데이트
                var characteristicsTooltip = mainWindow.FindName("CharacteristicsTooltip") as DependencyObject;
                if (characteristicsTooltip != null)
                {
                    ApplyToVisualChild<TextBlock>(characteristicsTooltip, "CharacteristicsTooltipTitle", "MainWindow.CharacteristicsTooltip.Title");
                    ApplyToVisualChild<TextBlock>(characteristicsTooltip, "CharacteristicsTooltipDescription", "MainWindow.CharacteristicsTooltip.Description");
                    ApplyToVisualChild<TextBlock>(characteristicsTooltip, "CharacteristicsTooltipCenterPoint", "MainWindow.CharacteristicsTooltip.CenterPoint");
                }

                // IPVS 툴팁 업데이트
                var ipvsTooltip = mainWindow.FindName("IPVSTooltip") as DependencyObject;
                if (ipvsTooltip != null)
                {
                    ApplyToVisualChild<TextBlock>(ipvsTooltip, "IPVSTooltipTitle", "MainWindow.IPVSTooltip.Title");
                    ApplyToVisualChild<TextBlock>(ipvsTooltip, "IPVSTooltipDescription", "MainWindow.IPVSTooltip.Description");
                }

                // Manual 툴팁 업데이트
                var manualTooltip = mainWindow.FindName("ManualTooltip") as DependencyObject;
                if (manualTooltip != null)
                {
                    ApplyToVisualChild<TextBlock>(manualTooltip, "ManualTooltipDescription", "MainWindow.ManualTooltip.Description");
                }

                // LUT 툴팁 업데이트
                var lutTooltip = mainWindow.FindName("LUTTooltip") as DependencyObject;
                if (lutTooltip != null)
                {
                    ApplyToVisualChild<TextBlock>(lutTooltip, "LUTTooltipText", "MainWindow.LUTTooltip.Description");
                }

                // 설정 툴팁 업데이트
                var settingsTooltip = mainWindow.FindName("SettingsTooltip") as DependencyObject;
                if (settingsTooltip != null)
                {
                    ApplyToVisualChild<TextBlock>(settingsTooltip, "SettingsTooltipTitle", "MainWindow.SettingsTooltip.Title");
                    ApplyToVisualChild<TextBlock>(settingsTooltip, "SettingsTooltipDescription", "MainWindow.SettingsTooltip.Description");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"툴팁 텍스트 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 시각적 트리에서 특정 이름의 자식 요소를 찾아 텍스트 적용
        /// </summary>
        private static void ApplyToVisualChild<T>(DependencyObject parent, string name, string textKey) where T : DependencyObject
        {
            var element = FindVisualChild<T>(parent, name);
            if (element is TextBlock textBlock)
            {
                textBlock.Text = LanguageManager.GetText(textKey);
            }
        }

        /// <summary>
        /// 시각적 트리에서 특정 이름의 자식 요소 찾기
        /// </summary>
        public static T FindVisualChild<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T && (child as FrameworkElement)?.Name == name)
                    return child as T;

                var childOfChild = FindVisualChild<T>(child, name);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        #endregion
    }
}

