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

namespace OptiX;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private DispatcherTimer? characteristicsTimer;
    private DispatcherTimer? ipvsTimer;
    private bool isCharacteristicsHovered = false;
    private bool isIPVSHovered = false;
    private bool isDarkMode = false;
    private UserControl? currentPage;

    public MainWindow()
    {
        InitializeComponent();
        InitializeTimers();
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
        MessageBox.Show("설정 버튼이 클릭되었습니다!", "설정", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
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
        
        // 타이틀바
        var titleBar = (Border)this.FindName("TitleBar");
        if (titleBar != null)
        {
            titleBar.Background = new SolidColorBrush(Color.FromRgb(135, 206, 235));
        }
        
        // 모드 토글 컨테이너
        var modeContainer = (Border)this.FindName("ModeToggleContainer");
        if (modeContainer != null)
        {
            modeContainer.Background = new SolidColorBrush(Color.FromRgb(135, 206, 235));
        }
        
        // 버튼 스타일 업데이트
        UpdateButtonStyles(false);
        
        // 툴팁 스타일 업데이트
        UpdateTooltipStyles(false);
    }

    private void SetDarkMode()
    {
        isDarkMode = true;
        
        // 창 배경
        this.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        
        // 타이틀바
        var titleBar = (Border)this.FindName("TitleBar");
        if (titleBar != null)
        {
            titleBar.Background = new SolidColorBrush(Color.FromRgb(45, 45, 45));
        }
        
        // 모드 토글 컨테이너
        var modeContainer = (Border)this.FindName("ModeToggleContainer");
        if (modeContainer != null)
        {
            modeContainer.Background = new SolidColorBrush(Color.FromRgb(45, 45, 45));
        }
        
        // 버튼 스타일 업데이트
        UpdateButtonStyles(true);
        
        // 툴팁 스타일 업데이트
        UpdateTooltipStyles(true);
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

    private TextBlock? FindTextBlockInTooltip(Border tooltip, int rowIndex)
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
        
        // 최소화/닫기 버튼 텍스트 색상 업데이트
        var minimizeButton = (Button)this.FindName("MinimizeButton");
        var closeButton = (Button)this.FindName("CloseButton");
        
        if (minimizeButton != null)
        {
            minimizeButton.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.White);
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
        opticPage.BackRequested += (s, e) => ShowMainPage();
        
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
        ipvsPage.BackRequested += (s, e) => ShowMainPage();
        
        var mainContentGrid = (Grid)this.FindName("MainContent");
        if (mainContentGrid != null)
        {
            mainContentGrid.Children.Add(ipvsPage);
            currentPage = ipvsPage;
        }
    }

    private void ShowMainPage()
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
}