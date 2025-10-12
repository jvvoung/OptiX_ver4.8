using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OptiX.Main;

namespace OptiX.Common
{
    /// <summary>
    /// 테마 관리 유틸리티 클래스
    /// 모든 페이지에서 공통으로 사용하는 테마 관련 기능을 제공합니다.
    /// MainWindow의 테마 관련 로직을 캡슐화합니다.
    /// </summary>
    public static class ThemeManager
    {
        /// <summary>
        /// 동적 색상 업데이트
        /// </summary>
        /// <param name="window">대상 윈도우</param>
        /// <param name="isDark">다크모드 여부</param>
        public static void UpdateDynamicColors(System.Windows.FrameworkElement window, bool isDark)
        {
            if (window == null) return;

            window.Dispatcher.Invoke(() =>
            {
                if (isDark)
                {
                    // 다크모드 색상으로 변경
                    window.Resources["DynamicBackgroundColor"] = new SolidColorBrush(Color.FromRgb(15, 23, 42)); // #0F172A
                    window.Resources["DynamicSurfaceColor"] = new SolidColorBrush(Color.FromRgb(30, 41, 59)); // #1E293B
                    window.Resources["DynamicCardColor"] = new SolidColorBrush(Color.FromRgb(51, 65, 85)); // #334155
                    window.Resources["DynamicBorderColor"] = new SolidColorBrush(Color.FromRgb(71, 85, 105)); // #475569
                    window.Resources["DynamicTextPrimaryColor"] = new SolidColorBrush(Color.FromRgb(241, 245, 249)); // #F1F5F9
                    window.Resources["DynamicTextSecondaryColor"] = new SolidColorBrush(Color.FromRgb(203, 213, 225)); // #CBD5E1
                    window.Resources["DynamicTextMutedColor"] = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // #94A3B8
                    window.Resources["DynamicTextColor"] = new SolidColorBrush(Color.FromRgb(241, 245, 249)); // #F1F5F9
                }
                else
                {
                    // 라이트모드 색상으로 변경
                    window.Resources["DynamicBackgroundColor"] = new SolidColorBrush(Color.FromRgb(248, 250, 252)); // #F8FAFC
                    window.Resources["DynamicSurfaceColor"] = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // #FFFFFF
                    window.Resources["DynamicCardColor"] = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // #FFFFFF
                    window.Resources["DynamicBorderColor"] = new SolidColorBrush(Color.FromRgb(226, 232, 240)); // #E2E8F0
                    window.Resources["DynamicTextPrimaryColor"] = new SolidColorBrush(Color.FromRgb(30, 41, 59)); // #1E293B
                    window.Resources["DynamicTextSecondaryColor"] = new SolidColorBrush(Color.FromRgb(100, 116, 139)); // #64748B
                    window.Resources["DynamicTextMutedColor"] = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // #94A3B8
                    window.Resources["DynamicTextColor"] = new SolidColorBrush(Color.FromRgb(30, 41, 59)); // #1E293B
                }
            });
        }

        #region MainWindow 테마 관리 (MainWindow.xaml.cs에서 이동)

        /// <summary>
        /// MainWindow 라이트모드 설정
        /// </summary>
        public static void SetMainWindowLightMode(Window mainWindow, PageNavigationManager pageNav)
        {
            if (mainWindow == null) return;

            // 창 배경
            mainWindow.Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)); // #F8F9FA

            // 타이틀바 - 라이트모드에서도 보라색 유지
            var titleBar = mainWindow.FindName("TitleBar") as Border;
            if (titleBar != null)
            {
                titleBar.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246)); // #8B5CF6
            }

            // 모드 토글 컨테이너 - 라이트모드에서도 보라색 유지
            var modeContainer = mainWindow.FindName("ModeToggleContainer") as Border;
            if (modeContainer != null)
            {
                modeContainer.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246)); // #8B5CF6
            }

            // 버튼 스타일 업데이트
            UpdateMainWindowButtonStyles(mainWindow, false);

            // 툴팁 스타일 업데이트
            UpdateMainWindowTooltipStyles(mainWindow, false);

            // PageNavigationManager에 다크모드 상태 전달
            pageNav?.SetDarkMode(false);
        }

        /// <summary>
        /// MainWindow 다크모드 설정
        /// </summary>
        public static void SetMainWindowDarkMode(Window mainWindow, PageNavigationManager pageNav)
        {
            if (mainWindow == null) return;

            // 창 배경
            mainWindow.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)); // #0F172A

            // 타이틀바 - 다크모드에서도 보라색 유지
            var titleBar = mainWindow.FindName("TitleBar") as Border;
            if (titleBar != null)
            {
                titleBar.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246)); // #8B5CF6
            }

            // 모드 토글 컨테이너 - 다크모드에서도 보라색 유지
            var modeContainer = mainWindow.FindName("ModeToggleContainer") as Border;
            if (modeContainer != null)
            {
                modeContainer.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246)); // #8B5CF6
            }

            // 버튼 스타일 업데이트
            UpdateMainWindowButtonStyles(mainWindow, true);

            // 툴팁 스타일 업데이트
            UpdateMainWindowTooltipStyles(mainWindow, true);

            // PageNavigationManager에 다크모드 상태 전달
            pageNav?.SetDarkMode(true);
        }

        /// <summary>
        /// MainWindow 버튼 스타일 업데이트
        /// </summary>
        private static void UpdateMainWindowButtonStyles(Window mainWindow, bool isDark)
        {
            // 주요 버튼들
            var buttonNames = new[] { "CharacteristicsButton", "IPVSButton", "ManualButton", "LUTButton", "SettingsButton" };

            foreach (var buttonName in buttonNames)
            {
                var button = mainWindow.FindName(buttonName) as Button;
                if (button != null)
                {
                    if (isDark)
                    {
                        button.Background = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                        button.Foreground = new SolidColorBrush(Colors.White);
                        button.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
                    }
                    else
                    {
                        button.Background = new SolidColorBrush(Colors.White);
                        button.Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80));
                        button.BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233));
                    }
                }
            }

            // 타이틀바 텍스트 업데이트
            UpdateMainWindowTitleBarText(mainWindow, isDark);
        }

        /// <summary>
        /// MainWindow 툴팁 스타일 업데이트
        /// </summary>
        private static void UpdateMainWindowTooltipStyles(Window mainWindow, bool isDark)
        {
            var tooltipNames = new[] { "CharacteristicsTooltip", "IPVSTooltip", "ManualTooltip", "LUTTooltip", "SettingsTooltip" };

            foreach (var tooltipName in tooltipNames)
            {
                var tooltip = mainWindow.FindName(tooltipName) as Border;
                if (tooltip != null)
                {
                    tooltip.Background = new SolidColorBrush(Colors.White);
                    tooltip.BorderBrush = new SolidColorBrush(Color.FromRgb(66, 133, 244));
                }
            }

            // 툴팁 텍스트 색상 업데이트
            UpdateMainWindowTooltipTextColors(mainWindow, isDark);
        }

        /// <summary>
        /// MainWindow 툴팁 텍스트 색상 업데이트
        /// </summary>
        private static void UpdateMainWindowTooltipTextColors(Window mainWindow, bool isDark)
        {
            var tooltips = new[]
            {
                "CharacteristicsTooltip",
                "IPVSTooltip",
                "ManualTooltip",
                "LUTTooltip",
                "SettingsTooltip"
            };

            foreach (var tooltipName in tooltips)
            {
                var tooltip = mainWindow.FindName(tooltipName) as Border;
                if (tooltip != null)
                {
                    // Title (첫 번째 행)
                    var title = FindTextBlockInTooltip(tooltip, 0);
                    if (title != null)
                    {
                        title.Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80));
                    }

                    // Description (두 번째 행)
                    var description = FindTextBlockInTooltip(tooltip, 1);
                    if (description != null)
                    {
                        description.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
                    }
                }
            }
        }

        /// <summary>
        /// 툴팁 내부 TextBlock 찾기
        /// </summary>
        private static TextBlock FindTextBlockInTooltip(Border tooltip, int rowIndex)
        {
            if (tooltip?.Child is Grid grid)
            {
                if (grid.Children.Count > rowIndex)
                {
                    if (grid.Children[rowIndex] is Grid innerGrid && innerGrid.Children.Count > 0)
                    {
                        return innerGrid.Children[0] as TextBlock;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// MainWindow 타이틀바 텍스트 업데이트
        /// </summary>
        private static void UpdateMainWindowTitleBarText(Window mainWindow, bool isDark)
        {
            // 타이틀바 텍스트 색상 업데이트
            var titleText = mainWindow.FindName("TitleText") as TextBlock;
            if (titleText != null)
            {
                titleText.Foreground = new SolidColorBrush(Colors.White);
            }

            // 최소화/최대화/닫기 버튼 텍스트 색상 업데이트
            var buttonNames = new[] { "MinimizeButton", "MaximizeButton", "CloseButton" };
            foreach (var buttonName in buttonNames)
            {
                var button = mainWindow.FindName(buttonName) as Button;
                if (button != null)
                {
                    button.Foreground = new SolidColorBrush(Colors.White);
                }
            }
        }

        #endregion
    }
}
