using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;

namespace OptiX
{

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private DispatcherTimer characteristicsTimer;
    private DispatcherTimer ipvsTimer;
    private bool isCharacteristicsHovered = false;
    private bool isIPVSHovered = false;
    private bool isDarkMode = false;
    private UserControl currentPage;
    private bool isMaximized = false;
    private bool isResizing = false;
    private Point resizeStartPoint;
    private Size resizeStartSize;
    private string resizeDirection = "";
    private IniFileManager iniManager;

    public MainWindow()
    {
        InitializeComponent();
        InitializeTimers();
        InitializeIniManager();
        LoadSettingsFromIni();
        
        // 언어 변경 이벤트 구독
        LanguageManager.LanguageChanged += OnLanguageChanged;
        
        // 초기 언어 적용
        ApplyLanguage();
    }

    private void InitializeTimers()
    {
        characteristicsTimer = new DispatcherTimer();
        characteristicsTimer.Interval = TimeSpan.FromMilliseconds(100);
        characteristicsTimer.Tick += (s, e) => CheckCharacteristicsHover();

        ipvsTimer = new DispatcherTimer();
        ipvsTimer.Interval = TimeSpan.FromMilliseconds(100);
        ipvsTimer.Tick += (s, e) => CheckIPVSHover();
    }

    private void CharacteristicsButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Optic 버튼 클릭됨");
        ShowCharacteristicsPage();
    }

    private void IPVSButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("IPVS 버튼 클릭됨");
        ShowIPVSPage();
    }

    private void ManualButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Manual 버튼 클릭됨");
        MessageBox.Show("Manual 버튼이 클릭되었습니다!", "Manual", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void LUTButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("LUT 버튼 클릭됨");
        MessageBox.Show("LUT 버튼이 클릭되었습니다!", "LUT", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("설정 버튼 클릭됨");
        
        try
        {
            var settingsWindow = new MainSettingsWindow(isDarkMode);
            settingsWindow.Owner = this;
            settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Non-Modal로 열기 (메인 프로그램 계속 동작)
            settingsWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"설정 창을 여는 중 오류가 발생했습니다: {ex.Message}", "오류", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void CharacteristicsButton_MouseEnter(object sender, MouseEventArgs e)
    {
        isCharacteristicsHovered = true;
        characteristicsTimer?.Start();
    }

    private void CharacteristicsButton_MouseLeave(object sender, MouseEventArgs e)
    {
        isCharacteristicsHovered = false;
        characteristicsTimer?.Start();
    }

    private void IPVSButton_MouseEnter(object sender, MouseEventArgs e)
    {
        isIPVSHovered = true;
        ipvsTimer?.Start();
    }

    private void IPVSButton_MouseLeave(object sender, MouseEventArgs e)
    {
        isIPVSHovered = false;
        ipvsTimer?.Start();
    }

    private void CheckCharacteristicsHover()
    {
        if (isCharacteristicsHovered)
        {
            CharacteristicsTooltip.Visibility = Visibility.Visible;
        }
        else
        {
            CharacteristicsTooltip.Visibility = Visibility.Collapsed;
        }
        characteristicsTimer?.Stop();
    }

    private void CheckIPVSHover()
    {
        if (isIPVSHovered)
        {
            IPVSTooltip.Visibility = Visibility.Visible;
        }
        else
        {
            IPVSTooltip.Visibility = Visibility.Collapsed;
        }
        ipvsTimer?.Stop();
    }

    private void ManualButton_MouseEnter(object sender, MouseEventArgs e)
    {
        ManualTooltip.Visibility = Visibility.Visible;
    }

    private void ManualButton_MouseLeave(object sender, MouseEventArgs e)
    {
        ManualTooltip.Visibility = Visibility.Collapsed;
    }

    private void LUTButton_MouseEnter(object sender, MouseEventArgs e)
    {
        LUTTooltip.Visibility = Visibility.Visible;
    }

    private void LUTButton_MouseLeave(object sender, MouseEventArgs e)
    {
        LUTTooltip.Visibility = Visibility.Collapsed;
    }

    private void SettingsButton_MouseEnter(object sender, MouseEventArgs e)
    {
        SettingsTooltip.Visibility = Visibility.Visible;
    }

    private void SettingsButton_MouseLeave(object sender, MouseEventArgs e)
    {
        SettingsTooltip.Visibility = Visibility.Collapsed;
    }

    private void LightModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (isDarkMode)
        {
            SetLightMode();
        }
    }

    private void DarkModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (!isDarkMode)
        {
            SetDarkMode();
        }
    }

    private void SetLightMode()
    {
        isDarkMode = false;

        // 창 배경
        this.Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));

        // 타이틀바 - 보라색 유지
        var titleBar = (Border)this.FindName("TitleBar");
        if (titleBar != null)
        {
            titleBar.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246)); // #8B5CF6
        }

        // 모드 토글 컨테이너 - 보라색 유지
        var modeContainer = (Border)this.FindName("ModeToggleContainer");
        if (modeContainer != null)
        {
            modeContainer.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246)); // #8B5CF6
        }

        // 버튼 스타일 업데이트
        UpdateButtonStyles(false);

        // 툴팁 스타일 업데이트
        UpdateTooltipStyles(false);

        // 현재 페이지 다크모드 상태 업데이트
        if (currentPage is OpticPage opticPage)
        {
            opticPage.SetDarkMode(false);
        }
        else if (currentPage is IPVSPage ipvsPage)
        {
            ipvsPage.SetDarkMode(false);
        }
    }

    private void SetDarkMode()
    {
        isDarkMode = true;

        // 창 배경
        this.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)); // #0F172A

        // 타이틀바 - 다크모드에서도 보라색 유지
        var titleBar = (Border)this.FindName("TitleBar");
        if (titleBar != null)
        {
            titleBar.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246)); // #8B5CF6
        }

        // 모드 토글 컨테이너 - 다크모드에서도 보라색 유지
        var modeContainer = (Border)this.FindName("ModeToggleContainer");
        if (modeContainer != null)
        {
            modeContainer.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246)); // #8B5CF6
        }

        // 버튼 스타일 업데이트
        UpdateButtonStyles(true);

        // 툴팁 스타일 업데이트
        UpdateTooltipStyles(true);

        // 현재 페이지 다크모드 상태 업데이트
        if (currentPage is OpticPage opticPage)
        {
            opticPage.SetDarkMode(true);
        }
        else if (currentPage is IPVSPage ipvsPage)
        {
            ipvsPage.SetDarkMode(true);
        }
    }

    private void UpdateButtonStyles(bool isDark)
    {
        // 모든 버튼의 스타일을 업데이트
        var buttons = new[] { CharacteristicsButton, IPVSButton };

        foreach (var button in buttons)
        {
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

        // 하단 버튼들도 업데이트
        UpdateBottomButtonStyles(isDark);

        // 타이틀바 텍스트 업데이트
        UpdateTitleBarText(isDark);
    }

    private void UpdateBottomButtonStyles(bool isDark)
    {
        // 하단 버튼들을 찾아서 스타일 업데이트
        var bottomButtons = new[] { "ManualButton", "LUTButton", "SettingsButton" };

        foreach (var buttonName in bottomButtons)
        {
            var button = (Button)this.FindName(buttonName);
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
    }

    private void UpdateTooltipStyles(bool isDark)
    {
        // 상단 툴팁 스타일 업데이트
        if (isDark)
        {
            CharacteristicsTooltip.Background = new SolidColorBrush(Colors.White);
            CharacteristicsTooltip.BorderBrush = new SolidColorBrush(Color.FromRgb(66, 133, 244));
            IPVSTooltip.Background = new SolidColorBrush(Colors.White);
            IPVSTooltip.BorderBrush = new SolidColorBrush(Color.FromRgb(66, 133, 244));

            // 하단 툴팁도 흰색 배경으로 설정
            ManualTooltip.Background = new SolidColorBrush(Colors.White);
            ManualTooltip.BorderBrush = new SolidColorBrush(Color.FromRgb(66, 133, 244));
            LUTTooltip.Background = new SolidColorBrush(Colors.White);
            LUTTooltip.BorderBrush = new SolidColorBrush(Color.FromRgb(66, 133, 244));
            SettingsTooltip.Background = new SolidColorBrush(Colors.White);
            SettingsTooltip.BorderBrush = new SolidColorBrush(Color.FromRgb(66, 133, 244));

            // 다크모드에서 툴팁 텍스트를 검은색으로 변경
            UpdateTooltipTextColors(true);
        }
        else
        {
            CharacteristicsTooltip.Background = new SolidColorBrush(Colors.White);
            CharacteristicsTooltip.BorderBrush = new SolidColorBrush(Color.FromRgb(66, 133, 244));
            IPVSTooltip.Background = new SolidColorBrush(Colors.White);
            IPVSTooltip.BorderBrush = new SolidColorBrush(Color.FromRgb(66, 133, 244));

            // 하단 툴팁도 흰색 배경으로 설정
            ManualTooltip.Background = new SolidColorBrush(Colors.White);
            ManualTooltip.BorderBrush = new SolidColorBrush(Color.FromRgb(66, 133, 244));
            LUTTooltip.Background = new SolidColorBrush(Colors.White);
            LUTTooltip.BorderBrush = new SolidColorBrush(Color.FromRgb(66, 133, 244));
            SettingsTooltip.Background = new SolidColorBrush(Colors.White);
            SettingsTooltip.BorderBrush = new SolidColorBrush(Color.FromRgb(66, 133, 244));

            // 라이트모드에서 툴팁 텍스트를 기본 색상으로 변경
            UpdateTooltipTextColors(false);
        }
    }

    private void UpdateTooltipTextColors(bool isDark)
    {
        // 특성 툴팁 텍스트 색상 업데이트
        var characteristicsTitle = FindTextBlockInTooltip(CharacteristicsTooltip, 0);
        var characteristicsDescription = FindTextBlockInTooltip(CharacteristicsTooltip, 1);

        if (characteristicsTitle != null)
        {
            characteristicsTitle.Foreground = isDark ? new SolidColorBrush(Color.FromRgb(44, 62, 80)) : new SolidColorBrush(Color.FromRgb(44, 62, 80));
        }

        if (characteristicsDescription != null)
        {
            characteristicsDescription.Foreground = isDark ? new SolidColorBrush(Color.FromRgb(102, 102, 102)) : new SolidColorBrush(Color.FromRgb(102, 102, 102));
        }

        // IPVS 툴팁 텍스트 색상 업데이트
        var ipvsTitle = FindTextBlockInTooltip(IPVSTooltip, 0);
        var ipvsDescription = FindTextBlockInTooltip(IPVSTooltip, 1);

        if (ipvsTitle != null)
        {
            ipvsTitle.Foreground = isDark ? new SolidColorBrush(Color.FromRgb(44, 62, 80)) : new SolidColorBrush(Color.FromRgb(44, 62, 80));
        }

        if (ipvsDescription != null)
        {
            ipvsDescription.Foreground = isDark ? new SolidColorBrush(Color.FromRgb(102, 102, 102)) : new SolidColorBrush(Color.FromRgb(102, 102, 102));
        }

        // 하단 툴팁 텍스트 색상 업데이트
        UpdateBottomTooltipTextColors(isDark);
    }

    private void UpdateBottomTooltipTextColors(bool isDark)
    {
        // Manual 툴팁 텍스트 색상 업데이트
        var manualTitle = FindTextBlockInTooltip(ManualTooltip, 0);
        var manualDescription = FindTextBlockInTooltip(ManualTooltip, 1);

        if (manualTitle != null)
        {
            manualTitle.Foreground = isDark ? new SolidColorBrush(Color.FromRgb(44, 62, 80)) : new SolidColorBrush(Color.FromRgb(44, 62, 80));
        }

        if (manualDescription != null)
        {
            manualDescription.Foreground = isDark ? new SolidColorBrush(Color.FromRgb(102, 102, 102)) : new SolidColorBrush(Color.FromRgb(102, 102, 102));
        }

        // LUT 툴팁 텍스트 색상 업데이트
        var lutTitle = FindTextBlockInTooltip(LUTTooltip, 0);
        var lutDescription = FindTextBlockInTooltip(LUTTooltip, 1);

        if (lutTitle != null)
        {
            lutTitle.Foreground = isDark ? new SolidColorBrush(Color.FromRgb(44, 62, 80)) : new SolidColorBrush(Color.FromRgb(44, 62, 80));
        }

        if (lutDescription != null)
        {
            lutDescription.Foreground = isDark ? new SolidColorBrush(Color.FromRgb(102, 102, 102)) : new SolidColorBrush(Color.FromRgb(102, 102, 102));
        }

        // 설정 툴팁 텍스트 색상 업데이트
        var settingsTitle = FindTextBlockInTooltip(SettingsTooltip, 0);
        var settingsDescription = FindTextBlockInTooltip(SettingsTooltip, 1);

        if (settingsTitle != null)
        {
            settingsTitle.Foreground = isDark ? new SolidColorBrush(Color.FromRgb(44, 62, 80)) : new SolidColorBrush(Color.FromRgb(44, 62, 80));
        }

        if (settingsDescription != null)
        {
            settingsDescription.Foreground = isDark ? new SolidColorBrush(Color.FromRgb(102, 102, 102)) : new SolidColorBrush(Color.FromRgb(102, 102, 102));
        }
    }

    private TextBlock FindTextBlockInTooltip(Border tooltip, int rowIndex)
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

    private void UpdateTitleBarText(bool isDark)
    {
        // 타이틀바 텍스트 색상 업데이트
        var titleText = (TextBlock)this.FindName("TitleText");
        if (titleText != null)
        {
            titleText.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.White);
        }

        // 최소화/최대화/닫기 버튼 텍스트 색상 업데이트
        var minimizeButton = (Button)this.FindName("MinimizeButton");
        var maximizeButton = (Button)this.FindName("MaximizeButton");
        var closeButton = (Button)this.FindName("CloseButton");

        if (minimizeButton != null)
        {
            minimizeButton.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.White);
        }

        if (maximizeButton != null)
        {
            maximizeButton.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.White);
        }

        if (closeButton != null)
        {
            closeButton.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.White);
        }
    }

    private void ShowCharacteristicsPage()
    {
        // 기존 페이지 제거
        if (currentPage != null)
        {
            var mainContent = (Grid)this.FindName("MainContent");
            if (mainContent != null)
            {
                mainContent.Children.Remove(currentPage);
            }
        }

        // 메인 페이지 콘텐츠 숨기기
        var mainPageContent = (Grid)this.FindName("MainPageContent");
        if (mainPageContent != null)
        {
            mainPageContent.Visibility = Visibility.Collapsed;
        }

        // Optic 페이지 생성 및 표시
        var opticPage = new OpticPage();

        // 현재 다크모드 상태를 OpticPage에 전달
        opticPage.SetDarkMode(isDarkMode);

        var mainContentGrid = (Grid)this.FindName("MainContent");
        if (mainContentGrid != null)
        {
            mainContentGrid.Children.Add(opticPage);
            currentPage = opticPage;
        }
    }

    private void ShowIPVSPage()
    {
        // 기존 페이지 제거
        if (currentPage != null)
        {
            var mainContent = (Grid)this.FindName("MainContent");
            if (mainContent != null)
            {
                mainContent.Children.Remove(currentPage);
            }
        }

        // 메인 페이지 콘텐츠 숨기기
        var mainPageContent = (Grid)this.FindName("MainPageContent");
        if (mainPageContent != null)
        {
            mainPageContent.Visibility = Visibility.Collapsed;
        }

        // IPVS 페이지 생성 및 표시
        var ipvsPage = new IPVSPage();

        // 현재 다크모드 상태를 IPVSPage에 전달
        ipvsPage.SetDarkMode(isDarkMode);
        
        // 현재 언어 상태를 IPVSPage에 전달
        ipvsPage.ApplyLanguage();

        // IPVSPage의 뒤로가기 이벤트 처리
        ipvsPage.BackRequested += (s, e) => ShowMainPage();

        var mainContentGrid = (Grid)this.FindName("MainContent");
        if (mainContentGrid != null)
        {
            mainContentGrid.Children.Add(ipvsPage);
            currentPage = ipvsPage;
        }
    }

    public void ShowMainPage()
    {
        // 기존 페이지 제거
        if (currentPage != null)
        {
            var mainContent = (Grid)this.FindName("MainContent");
            if (mainContent != null)
            {
                mainContent.Children.Remove(currentPage);
            }
            currentPage = null;
        }

        // 메인 페이지 콘텐츠 다시 표시
        var mainPageContent = (Grid)this.FindName("MainPageContent");
        if (mainPageContent != null)
        {
            mainPageContent.Visibility = Visibility.Visible;
        }
    }

    private void ToggleMaximize()
    {
        if (isMaximized)
        {
            // 복원
            this.WindowState = WindowState.Normal;
            isMaximized = false;
            UpdateMaximizeButton();
        }
        else
        {
            // 최대화
            this.WindowState = WindowState.Maximized;
            isMaximized = true;
            UpdateMaximizeButton();
        }
    }

    private void UpdateMaximizeButton()
    {
        var maximizeButton = (Button)this.FindName("MaximizeButton");
        if (maximizeButton != null)
        {
            maximizeButton.Content = isMaximized ? "❐" : "□";
        }
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);

        // 창 상태가 변경될 때 최대화 상태 업데이트
        if (this.WindowState == WindowState.Maximized)
        {
            isMaximized = true;
        }
        else if (this.WindowState == WindowState.Normal)
        {
            isMaximized = false;
        }

        UpdateMaximizeButton();
    }

    private void ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (isMaximized) return; // 최대화 상태에서는 크기 조정 불가

        isResizing = true;
        resizeStartPoint = e.GetPosition(this);
        resizeStartSize = new Size(this.Width, this.Height);

        var handle = sender as Border;
        if (handle != null)
        {
            switch (handle.Name)
            {
                case "TopResizeHandle":
                    resizeDirection = "Top";
                    break;
                case "BottomResizeHandle":
                    resizeDirection = "Bottom";
                    break;
                case "LeftResizeHandle":
                    resizeDirection = "Left";
                    break;
                case "RightResizeHandle":
                    resizeDirection = "Right";
                    break;
                case "TopLeftResizeHandle":
                    resizeDirection = "TopLeft";
                    break;
                case "TopRightResizeHandle":
                    resizeDirection = "TopRight";
                    break;
                case "BottomLeftResizeHandle":
                    resizeDirection = "BottomLeft";
                    break;
                case "BottomRightResizeHandle":
                    resizeDirection = "BottomRight";
                    break;
                default:
                    resizeDirection = "";
                    break;
            }

            // 클릭한 상태에서만 커서 변경
            if (resizeDirection == "Top" || resizeDirection == "Bottom")
            {
                this.Cursor = Cursors.SizeNS;
            }
            else if (resizeDirection == "Left" || resizeDirection == "Right")
            {
                this.Cursor = Cursors.SizeWE;
            }
            else if (resizeDirection == "TopLeft" || resizeDirection == "BottomRight")
            {
                this.Cursor = Cursors.SizeNWSE;
            }
            else if (resizeDirection == "TopRight" || resizeDirection == "BottomLeft")
            {
                this.Cursor = Cursors.SizeNESW;
            }
            else
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        this.CaptureMouse();
        e.Handled = true;
    }

    private void ResizeHandle_MouseMove(object sender, MouseEventArgs e)
    {
        if (!isResizing || isMaximized) return;

        var currentPoint = e.GetPosition(this);
        var deltaX = currentPoint.X - resizeStartPoint.X;
        var deltaY = currentPoint.Y - resizeStartPoint.Y;

        // 부드러운 크기 조정을 위해 직접 계산
        var newWidth = resizeStartSize.Width;
        var newHeight = resizeStartSize.Height;
        var newLeft = this.Left;
        var newTop = this.Top;

        switch (resizeDirection)
        {
            case "Top":
                newHeight = Math.Max(MinHeight, resizeStartSize.Height - deltaY);
                newTop = this.Top + (resizeStartSize.Height - newHeight);
                break;
            case "Bottom":
                newHeight = Math.Max(MinHeight, resizeStartSize.Height + deltaY);
                break;
            case "Left":
                newWidth = Math.Max(MinWidth, resizeStartSize.Width - deltaX);
                newLeft = this.Left + (resizeStartSize.Width - newWidth);
                break;
            case "Right":
                newWidth = Math.Max(MinWidth, resizeStartSize.Width + deltaX);
                break;
            case "TopLeft":
                newWidth = Math.Max(MinWidth, resizeStartSize.Width - deltaX);
                newHeight = Math.Max(MinHeight, resizeStartSize.Height - deltaY);
                newLeft = this.Left + (resizeStartSize.Width - newWidth);
                newTop = this.Top + (resizeStartSize.Height - newHeight);
                break;
            case "TopRight":
                newWidth = Math.Max(MinWidth, resizeStartSize.Width + deltaX);
                newHeight = Math.Max(MinHeight, resizeStartSize.Height - deltaY);
                newTop = this.Top + (resizeStartSize.Height - newHeight);
                break;
            case "BottomLeft":
                newWidth = Math.Max(MinWidth, resizeStartSize.Width - deltaX);
                newHeight = Math.Max(MinHeight, resizeStartSize.Height + deltaY);
                newLeft = this.Left + (resizeStartSize.Width - newWidth);
                break;
            case "BottomRight":
                newWidth = Math.Max(MinWidth, resizeStartSize.Width + deltaX);
                newHeight = Math.Max(MinHeight, resizeStartSize.Height + deltaY);
                break;
        }

        // 즉시 크기 업데이트
        this.Width = newWidth;
        this.Height = newHeight;
        this.Left = newLeft;
        this.Top = newTop;

        e.Handled = true;
    }

    private void ResizeHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (isResizing)
        {
            isResizing = false;
            this.Cursor = Cursors.Arrow; // 커서를 기본 화살표로 되돌리기
            this.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void InitializeIniManager()
    {
        string iniPath = @"D:\\Project\\Recipe\\OptiX.ini";
        iniManager = new IniFileManager(iniPath);

        // INI 파일이 없으면 기본 파일 생성
            // INI 파일이 없으면 기본값 사용
    }

    private void LoadSettingsFromIni()
    {
        if (iniManager == null) return;

        try
        {
            // 창 크기 및 위치 설정 제거됨 - 하드코딩된 기본값 사용

            // 테마 설정 로드
            string isDarkModeStr = iniManager.ReadValue("Theme", "IsDarkMode", "False");
            if (bool.TryParse(isDarkModeStr, out bool darkMode) && darkMode)
            {
                SetDarkMode();
            }

            // 언어 설정 로드
            string currentLanguage = iniManager.ReadValue("Settings", "Language", "Korean");
            LanguageManager.SetLanguage(currentLanguage);

            // 타이틀바 색상 설정 제거됨 - XAML에서 하드코딩된 보라색 사용

            System.Diagnostics.Debug.WriteLine("INI 설정이 성공적으로 로드되었습니다.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"INI 설정 로드 오류: {ex.Message}");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        // 언어 변경 이벤트 구독 해제
        LanguageManager.LanguageChanged -= OnLanguageChanged;
        base.OnClosed(e);
    }

    /// <summary>
    /// 언어 변경 이벤트 핸들러
    /// </summary>
    private void OnLanguageChanged(object sender, EventArgs e)
    {
        ApplyLanguage();
        
        // 현재 페이지에도 언어 적용
        if (currentPage is OpticPage opticPage)
        {
            opticPage.ApplyLanguage();
        }
        else if (currentPage is IPVSPage ipvsPage)
        {
            ipvsPage.ApplyLanguage();
        }
    }

    /// <summary>
    /// MainWindow에 언어 적용
    /// </summary>
    public void ApplyLanguage()
    {
        try
        {
            // 버튼 텍스트 업데이트
            if (CharacteristicsButton != null)
                CharacteristicsButton.Content = LanguageManager.GetText("MainWindow.Characteristics");
            
            if (IPVSButton != null)
                IPVSButton.Content = LanguageManager.GetText("MainWindow.IPVS");
            
            if (ManualButton != null)
                ManualButton.Content = LanguageManager.GetText("MainWindow.Manual");
            
            if (LUTButton != null)
                LUTButton.Content = LanguageManager.GetText("MainWindow.LUT");
            
            if (SettingsButton != null)
                SettingsButton.Content = LanguageManager.GetText("MainWindow.Settings");
            
            // 호버 툴팁 텍스트 업데이트
            UpdateTooltipTexts();
            
            System.Diagnostics.Debug.WriteLine($"MainWindow 언어 적용 완료: {LanguageManager.CurrentLanguage}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainWindow 언어 적용 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 호버 툴팁 텍스트 업데이트
    /// </summary>
    private void UpdateTooltipTexts()
    {
        try
        {
            // Characteristics 툴팁 업데이트
            if (CharacteristicsTooltip != null)
            {
                var titleTextBlock = FindVisualChild<TextBlock>(CharacteristicsTooltip, "CharacteristicsTooltipTitle");
                if (titleTextBlock != null)
                    titleTextBlock.Text = LanguageManager.GetText("MainWindow.CharacteristicsTooltip.Title");
                
                var descriptionTextBlock = FindVisualChild<TextBlock>(CharacteristicsTooltip, "CharacteristicsTooltipDescription");
                if (descriptionTextBlock != null)
                    descriptionTextBlock.Text = LanguageManager.GetText("MainWindow.CharacteristicsTooltip.Description");
                
                var centerPointTextBlock = FindVisualChild<TextBlock>(CharacteristicsTooltip, "CharacteristicsTooltipCenterPoint");
                if (centerPointTextBlock != null)
                    centerPointTextBlock.Text = LanguageManager.GetText("MainWindow.CharacteristicsTooltip.CenterPoint");
            }
            
                // IPVS 툴팁 업데이트
                if (IPVSTooltip != null)
                {
                    var titleTextBlock = FindVisualChild<TextBlock>(IPVSTooltip, "IPVSTooltipTitle");
                    if (titleTextBlock != null)
                        titleTextBlock.Text = LanguageManager.GetText("MainWindow.IPVSTooltip.Title");
                    
                    var descriptionTextBlock = FindVisualChild<TextBlock>(IPVSTooltip, "IPVSTooltipDescription");
                    if (descriptionTextBlock != null)
                        descriptionTextBlock.Text = LanguageManager.GetText("MainWindow.IPVSTooltip.Description");
                }
                
                // 설정 툴팁 업데이트
                if (SettingsTooltip != null)
                {
                    var titleTextBlock = FindVisualChild<TextBlock>(SettingsTooltip, "SettingsTooltipTitle");
                    if (titleTextBlock != null)
                        titleTextBlock.Text = LanguageManager.GetText("MainWindow.SettingsTooltip.Title");
                    
                    var descriptionTextBlock = FindVisualChild<TextBlock>(SettingsTooltip, "SettingsTooltipDescription");
                    if (descriptionTextBlock != null)
                        descriptionTextBlock.Text = LanguageManager.GetText("MainWindow.SettingsTooltip.Description");
                }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"툴팁 텍스트 업데이트 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 시각적 트리에서 특정 이름의 자식 요소 찾기
    /// </summary>
    private T FindVisualChild<T>(DependencyObject parent, string name) where T : DependencyObject
    {
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

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // 더블클릭 시 최대화/복원 토글
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }
        else
        {
            // 단일 클릭 시 창 드래그
            DragMove();
        }
    }
}
}
