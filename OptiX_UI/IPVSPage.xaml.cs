using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OptiX
{
    /// <summary>
    /// IPVSPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class IPVSPage : UserControl
    {
        public event EventHandler? BackRequested;
        
        public IPVSPage()
        {
            InitializeComponent();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void CellButton_Click(object sender, RoutedEventArgs e)
        {
            // Cell 버튼 활성화
            CellButton.Background = new SolidColorBrush(Colors.White);
            CellButton.Foreground = new SolidColorBrush(Color.FromRgb(66, 133, 244));
            
            // Total 버튼 비활성화
            TotalButton.Background = new SolidColorBrush(Colors.Transparent);
            TotalButton.Foreground = new SolidColorBrush(Colors.White);
            
            // 뷰 전환
            CellView.Visibility = Visibility.Visible;
            TotalView.Visibility = Visibility.Collapsed;
        }

        private void TotalButton_Click(object sender, RoutedEventArgs e)
        {
            // Total 버튼 활성화
            TotalButton.Background = new SolidColorBrush(Colors.White);
            TotalButton.Foreground = new SolidColorBrush(Color.FromRgb(66, 133, 244));
            
            // Cell 버튼 비활성화
            CellButton.Background = new SolidColorBrush(Colors.Transparent);
            CellButton.Foreground = new SolidColorBrush(Colors.White);
            
            // 뷰 전환
            TotalView.Visibility = Visibility.Visible;
            CellView.Visibility = Visibility.Collapsed;
        }
    }
}
