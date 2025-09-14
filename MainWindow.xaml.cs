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
        System.Diagnostics.Debug.WriteLine("특성 버튼 클릭됨");
        MessageBox.Show("특성 버튼이 클릭되었습니다!", "특성", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void IPVSButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("IPVS 버튼 클릭됨");
        MessageBox.Show("IPVS 버튼이 클릭되었습니다!", "IPVS", MessageBoxButton.OK, MessageBoxImage.Information);
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
}