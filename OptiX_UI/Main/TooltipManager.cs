using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace OptiX.Main
{
    /// <summary>
    /// MainWindow의 툴팁 관리 클래스
    /// 
    /// 역할:
    /// - 호버 이벤트 처리
    /// - 타이머 기반 툴팁 표시/숨김
    /// - 다크모드에 따른 Settings 버튼 색상 변경
    /// </summary>
    public class TooltipManager
    {
        private readonly Window mainWindow;
        private bool isDarkMode = false;

        // 타이머
        private DispatcherTimer characteristicsTimer;
        private DispatcherTimer ipvsTimer;

        // 호버 상태
        private bool isCharacteristicsHovered = false;
        private bool isIPVSHovered = false;

        // UI 요소들
        private Border characteristicsTooltip;
        private Border ipvsTooltip;
        private Border manualTooltip;
        private Border lutTooltip;
        private Border settingsTooltip;
        private Button settingsButton;

        public TooltipManager(Window window)
        {
            this.mainWindow = window;
            InitializeTimers();
            FindUIElements();
        }

        /// <summary>
        /// 타이머 초기화
        /// </summary>
        private void InitializeTimers()
        {
            characteristicsTimer = new DispatcherTimer();
            characteristicsTimer.Interval = TimeSpan.FromMilliseconds(100);
            characteristicsTimer.Tick += (s, e) => CheckCharacteristicsHover();

            ipvsTimer = new DispatcherTimer();
            ipvsTimer.Interval = TimeSpan.FromMilliseconds(100);
            ipvsTimer.Tick += (s, e) => CheckIPVSHover();
        }

        /// <summary>
        /// UI 요소 찾기
        /// </summary>
        private void FindUIElements()
        {
            characteristicsTooltip = mainWindow.FindName("CharacteristicsTooltip") as Border;
            ipvsTooltip = mainWindow.FindName("IPVSTooltip") as Border;
            manualTooltip = mainWindow.FindName("ManualTooltip") as Border;
            lutTooltip = mainWindow.FindName("LUTTooltip") as Border;
            settingsTooltip = mainWindow.FindName("SettingsTooltip") as Border;
            settingsButton = mainWindow.FindName("SettingsButton") as Button;
        }

        /// <summary>
        /// 다크모드 상태 설정
        /// </summary>
        public void SetDarkMode(bool darkMode)
        {
            this.isDarkMode = darkMode;
        }

        #region Characteristics 버튼 이벤트

        public void OnCharacteristicsButtonMouseEnter(object sender, MouseEventArgs e)
        {
            isCharacteristicsHovered = true;
            characteristicsTimer?.Start();
        }

        public void OnCharacteristicsButtonMouseLeave(object sender, MouseEventArgs e)
        {
            isCharacteristicsHovered = false;
            characteristicsTimer?.Start();
        }

        private void CheckCharacteristicsHover()
        {
            if (characteristicsTooltip != null)
            {
                characteristicsTooltip.Visibility = isCharacteristicsHovered ? Visibility.Visible : Visibility.Collapsed;
            }
            characteristicsTimer?.Stop();
        }

        #endregion

        #region IPVS 버튼 이벤트

        public void OnIPVSButtonMouseEnter(object sender, MouseEventArgs e)
        {
            isIPVSHovered = true;
            ipvsTimer?.Start();
        }

        public void OnIPVSButtonMouseLeave(object sender, MouseEventArgs e)
        {
            isIPVSHovered = false;
            ipvsTimer?.Start();
        }

        private void CheckIPVSHover()
        {
            if (ipvsTooltip != null)
            {
                ipvsTooltip.Visibility = isIPVSHovered ? Visibility.Visible : Visibility.Collapsed;
            }
            ipvsTimer?.Stop();
        }

        #endregion

        #region Manual 버튼 이벤트

        public void OnManualButtonMouseEnter(object sender, MouseEventArgs e)
        {
            if (manualTooltip != null)
            {
                manualTooltip.Visibility = Visibility.Visible;
            }
        }

        public void OnManualButtonMouseLeave(object sender, MouseEventArgs e)
        {
            if (manualTooltip != null)
            {
                manualTooltip.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region LUT 버튼 이벤트

        public void OnLUTButtonMouseEnter(object sender, MouseEventArgs e)
        {
            if (lutTooltip != null)
            {
                lutTooltip.Visibility = Visibility.Visible;
            }
        }

        public void OnLUTButtonMouseLeave(object sender, MouseEventArgs e)
        {
            if (lutTooltip != null)
            {
                lutTooltip.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Settings 버튼 이벤트

        public void OnSettingsButtonMouseEnter(object sender, MouseEventArgs e)
        {
            if (settingsTooltip != null)
            {
                settingsTooltip.Visibility = Visibility.Visible;
            }

            // 다크모드에서 호버 시 텍스트 색상을 검정색으로 강제 설정
            if (isDarkMode && settingsButton != null)
            {
                settingsButton.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0)); // 검정색
            }
        }

        public void OnSettingsButtonMouseLeave(object sender, MouseEventArgs e)
        {
            if (settingsTooltip != null)
            {
                settingsTooltip.Visibility = Visibility.Collapsed;
            }

            // 호버 해제 시 원래 색상으로 복원
            if (isDarkMode && settingsButton != null)
            {
                settingsButton.Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)); // 다크모드 기본 색상
            }
        }

        #endregion
    }
}


