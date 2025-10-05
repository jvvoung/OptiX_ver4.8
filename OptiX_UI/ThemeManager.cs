using System;
using System.Windows;
using System.Windows.Media;

namespace OptiX
{
    /// <summary>
    /// 테마 관리 유틸리티 클래스
    /// 모든 페이지에서 공통으로 사용하는 테마 관련 기능을 제공합니다.
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

    }
}
