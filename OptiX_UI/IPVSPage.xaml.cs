using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using OptiX.Models;

namespace OptiX
{
    /// <summary>
    /// IPVSPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class IPVSPage : System.Windows.Controls.UserControl
    {
        public event EventHandler? BackRequested;
        
        private IniFileManager iniManager;
        private ObservableCollection<DataTableItem> dataItems;
        private bool isDarkMode = false;
        
        public IPVSPage()
        {
            InitializeComponent();
            InitializeIniManager();
            LoadDataFromIni();
            InitializeDataTable();
            LoadThemeFromIni();
        }

        private void InitializeIniManager()
        {
            string iniPath = @"D:\OptiX\Recipe\OptiX.ini";
            iniManager = new IniFileManager(iniPath);
        }

        private void LoadDataFromIni()
        {
            try
            {
                // IPVS 섹션에서 Zone과 MAX_POINT 읽기
                string zoneCountStr = iniManager.ReadValue("IPVS", "Zone", "2");
                string maxPointStr = iniManager.ReadValue("IPVS", "MAX_POINT", "5");

                int zoneCount = int.Parse(zoneCountStr);
                int maxPoint = int.Parse(maxPointStr);

                dataItems = new ObservableCollection<DataTableItem>();

                // Zone과 Point에 따라 데이터 생성
                for (int zone = 1; zone <= zoneCount; zone++)
                {
                    for (int point = 1; point <= maxPoint; point++)
                    {
                        dataItems.Add(new DataTableItem
                        {
                            Zone = zone.ToString(), // 모든 행에 Zone 표시 (그룹화를 위해)
                            InnerId = "", // Inner ID는 빈 값으로 설정
                            CellId = "", // Cell ID는 빈 값으로 설정
                            Category = point.ToString(), // Point 값 (1, 2, 3, 4, 5)
                            X = "",
                            Y = "",
                            L = "",
                            Current = "",
                            Efficiency = "",
                            ErrorName = "",
                            Tact = "",
                            Judgment = "",
                            IsFirstInGroup = point == 1, // 그룹의 첫 번째 행인지 표시
                            GroupSize = maxPoint // 그룹 크기
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"INI 파일을 읽는 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeDataTable()
        {
            CreateCustomTable();
            CreateZoneButtons();
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
                MessageBox.Show($"테마 설정을 읽는 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyTheme()
        {
            if (isDarkMode)
            {
                // 다크모드 색상 적용
                UpdateDynamicColors(true);
                UpdateTableColors(true);
            }
            else
            {
                // 라이트모드 색상 적용
                UpdateDynamicColors(false);
                UpdateTableColors(false);
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

        private void UpdateTableColors(bool isDark)
        {
            // 테이블을 다시 생성하여 올바른 색상 적용
            CreateCustomTable();
        }

        public void ToggleDarkMode()
        {
            isDarkMode = !isDarkMode;
            ApplyTheme();
        }
        
        public void SetDarkMode(bool darkMode)
        {
            isDarkMode = darkMode;
            ApplyTheme();
        }

        private void CreateZoneButtons()
        {
            // 기존 Zone 버튼들 제거
            ZoneButtonsPanel.Children.Clear();

            try
            {
                // INI 파일에서 Zone 개수 읽기
                string zoneCountStr = iniManager.ReadValue("IPVS", "Zone", "2");
                int zoneCount = int.Parse(zoneCountStr);

                // Zone 개수만큼 모던 버튼 생성
                for (int i = 1; i <= zoneCount; i++)
                {
                    var zoneButton = new Button
                    {
                        Content = i.ToString(),
                        MinWidth = 28,
                        MinHeight = 28,
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(3, 0, 3, 0),
                        Tag = i
                    };

                    // 첫 번째 버튼은 활성화 상태
                    if (i == 1)
                    {
                        zoneButton.Style = (Style)FindResource("ActiveZoneButtonStyle");
                    }
                    else
                    {
                        zoneButton.Style = (Style)FindResource("ZoneButtonStyle");
                    }

                    zoneButton.Click += ZoneButton_Click;
                    ZoneButtonsPanel.Children.Add(zoneButton);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Zone 버튼을 생성하는 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateCustomTable()
        {
            try
            {
                // 기존 행들 제거
                DataTableGrid.RowDefinitions.Clear();
                DataTableGrid.Children.Clear();

                // 헤더 행 추가
                DataTableGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });

                // 헤더 생성
                CreateHeaderRow();

                // INI에서 MAX_POINT 개수 읽기
                string maxPointStr = iniManager.ReadValue("IPVS", "MAX_POINT", "5");
                int maxPoint = int.Parse(maxPointStr);

                // Zone별로 그룹화하여 처리 (빈 Zone 제외하지 않음)
                var zoneGroups = dataItems.GroupBy(item => item.Zone).ToList();

                foreach (var zoneGroup in zoneGroups)
                {
                    var groupItems = zoneGroup.ToList();

                    // 실제 데이터 개수만큼 행 추가
                    for (int i = 0; i < groupItems.Count; i++)
                    {
                        DataTableGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
                    }

                    // 첫 번째 행에서 병합된 셀들 생성
                    var firstItem = groupItems.First();

                    // Zone 열 (행 병합) - 실제 데이터 개수만큼 병합
                    var zoneBorder = new Border
                    {
                        Background = (SolidColorBrush)FindResource("DynamicSurfaceColor"),
                        BorderBrush = (SolidColorBrush)FindResource("DynamicBorderColor"),
                        BorderThickness = new Thickness(0, 0, 1, 1)
                    };
                    var zoneText = new TextBlock
                    {
                        Text = firstItem.Zone,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontWeight = FontWeights.Bold,
                        FontSize = 14,
                        Foreground = (SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                    };
                    zoneBorder.Child = zoneText;
                    Grid.SetRow(zoneBorder, DataTableGrid.RowDefinitions.Count - groupItems.Count);
                    Grid.SetColumn(zoneBorder, 0);
                    Grid.SetRowSpan(zoneBorder, groupItems.Count);
                    DataTableGrid.Children.Add(zoneBorder);

                    // Cell ID 열 (행 병합) - 실제 데이터 개수만큼 병합
                    var cellIdBorder = new Border
                    {
                        Background = (SolidColorBrush)FindResource("DynamicSurfaceColor"),
                        BorderBrush = (SolidColorBrush)FindResource("DynamicBorderColor"),
                        BorderThickness = new Thickness(0, 0, 1, 1)
                    };
                    var cellIdText = new TextBlock
                    {
                        Text = firstItem.CellId,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 14,
                        Foreground = (SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                    };
                    cellIdBorder.Child = cellIdText;
                    Grid.SetRow(cellIdBorder, DataTableGrid.RowDefinitions.Count - groupItems.Count);
                    Grid.SetColumn(cellIdBorder, 1);
                    Grid.SetRowSpan(cellIdBorder, groupItems.Count);
                    DataTableGrid.Children.Add(cellIdBorder);

                    // Inner ID 열 (행 병합) - 실제 데이터 개수만큼 병합
                    var innerIdBorder = new Border
                    {
                        Background = (SolidColorBrush)FindResource("DynamicSurfaceColor"),
                        BorderBrush = (SolidColorBrush)FindResource("DynamicBorderColor"),
                        BorderThickness = new Thickness(0, 0, 1, 1)
                    };
                    var innerIdText = new TextBlock
                    {
                        Text = firstItem.InnerId,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 14,
                        Foreground = (SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                    };
                    innerIdBorder.Child = innerIdText;
                    Grid.SetRow(innerIdBorder, DataTableGrid.RowDefinitions.Count - groupItems.Count);
                    Grid.SetColumn(innerIdBorder, 2);
                    Grid.SetRowSpan(innerIdBorder, groupItems.Count);
                    DataTableGrid.Children.Add(innerIdBorder);

                    // Error Name 열 (행 병합) - 실제 데이터 개수만큼 병합
                    var errorNameBorder = new Border
                    {
                        Background = (SolidColorBrush)FindResource("DynamicSurfaceColor"),
                        BorderBrush = (SolidColorBrush)FindResource("DynamicBorderColor"),
                        BorderThickness = new Thickness(0, 0, 1, 1)
                    };
                    var errorNameText = new TextBlock
                    {
                        Text = firstItem.ErrorName,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 14,
                        Foreground = (SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                    };
                    errorNameBorder.Child = errorNameText;
                    Grid.SetRow(errorNameBorder, DataTableGrid.RowDefinitions.Count - groupItems.Count);
                    Grid.SetColumn(errorNameBorder, 9);
                    Grid.SetRowSpan(errorNameBorder, groupItems.Count);
                    DataTableGrid.Children.Add(errorNameBorder);

                    // Tact 열 (행 병합) - 실제 데이터 개수만큼 병합
                    var tactBorder = new Border
                    {
                        Background = (SolidColorBrush)FindResource("DynamicSurfaceColor"),
                        BorderBrush = (SolidColorBrush)FindResource("DynamicBorderColor"),
                        BorderThickness = new Thickness(0, 0, 1, 1)
                    };
                    var tactText = new TextBlock
                    {
                        Text = firstItem.Tact,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 14,
                        Foreground = (SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                    };
                    tactBorder.Child = tactText;
                    Grid.SetRow(tactBorder, DataTableGrid.RowDefinitions.Count - groupItems.Count);
                    Grid.SetColumn(tactBorder, 10);
                    Grid.SetRowSpan(tactBorder, groupItems.Count);
                    DataTableGrid.Children.Add(tactBorder);

                    // 판정 열 (행 병합) - 실제 데이터 개수만큼 병합
                    var judgmentBorder = new Border
                    {
                        Background = (SolidColorBrush)FindResource("DynamicSurfaceColor"),
                        BorderBrush = (SolidColorBrush)FindResource("DynamicBorderColor"),
                        BorderThickness = new Thickness(0, 0, 0, 1)
                    };
                    var judgmentText = new TextBlock
                    {
                        Text = firstItem.Judgment,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 14,
                        Foreground = (SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                    };
                    judgmentBorder.Child = judgmentText;
                    Grid.SetRow(judgmentBorder, DataTableGrid.RowDefinitions.Count - groupItems.Count);
                    Grid.SetColumn(judgmentBorder, 11);
                    Grid.SetRowSpan(judgmentBorder, groupItems.Count);
                    DataTableGrid.Children.Add(judgmentBorder);

                    // 각 행별로 Point, x, y, L, 전류, 효율 열들 생성 - 실제 데이터 개수만큼
                    for (int i = 0; i < groupItems.Count; i++)
                    {
                        var item = groupItems[i];
                        int currentRow = DataTableGrid.RowDefinitions.Count - groupItems.Count + i;

                        // Point 열
                        var pointBorder = new Border
                        {
                            Background = (SolidColorBrush)FindResource("DynamicSurfaceColor"),
                            BorderBrush = (SolidColorBrush)FindResource("DynamicBorderColor"),
                            BorderThickness = new Thickness(0, 0, 1, 1)
                        };
                        var pointText = new TextBlock
                        {
                            Text = item.Category, // Point 값 (1, 2, 3, 4, 5)
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 14,
                            Foreground = (SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                        };
                        pointBorder.Child = pointText;
                        Grid.SetRow(pointBorder, currentRow);
                        Grid.SetColumn(pointBorder, 3);
                        DataTableGrid.Children.Add(pointBorder);

                        // x, y, L, 전류, 효율 열들
                        string[] values = { item.X, item.Y, item.L, item.Current, item.Efficiency };

                        for (int j = 0; j < values.Length; j++)
                        {
                            var border = new Border
                            {
                                Background = (SolidColorBrush)FindResource("DynamicSurfaceColor"),
                                BorderBrush = (SolidColorBrush)FindResource("DynamicBorderColor"),
                                BorderThickness = new Thickness(0, 0, 1, 1)
                            };
                            var text = new TextBlock
                            {
                                Text = values[j],
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                FontSize = 14,
                                Foreground = (SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                            };
                            border.Child = text;
                            Grid.SetRow(border, currentRow);
                            Grid.SetColumn(border, 4 + j);
                            DataTableGrid.Children.Add(border);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"테이블 생성 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void CreateHeaderRow()
        {
            string[] headers = { "Zone", "Cell ID", "Inner ID", "Point", "x", "y", "L", "전류", "효율", "Error Name", "Tact", "판정" };
            
            for (int i = 0; i < headers.Length; i++)
            {
                Border header = new Border
                {
                    Background = (SolidColorBrush)FindResource("PrimaryColor"),
                    BorderBrush = (SolidColorBrush)FindResource("DynamicBorderColor"),
                    BorderThickness = new Thickness(0, 0, i == headers.Length - 1 ? 0 : 1, 1)
                };
                
                TextBlock headerText = new TextBlock
                {
                    Text = headers[i],
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 14
                };
                
                header.Child = headerText;
                Grid.SetRow(header, 0);
                Grid.SetColumn(header, i);
                DataTableGrid.Children.Add(header);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void GraphTab_Click(object sender, RoutedEventArgs e)
        {
            // Graph 탭 활성화
            GraphTab.Style = (Style)FindResource("ActiveTabButtonStyle");
            
            // Total 탭 비활성화
            TotalTab.Style = (Style)FindResource("TabButtonStyle");
            
            // 콘텐츠 전환
            GraphContent.Visibility = Visibility.Visible;
            TotalContent.Visibility = Visibility.Collapsed;
        }

        private void TotalTab_Click(object sender, RoutedEventArgs e)
        {
            // Total 탭 활성화
            TotalTab.Style = (Style)FindResource("ActiveTabButtonStyle");
            
            // Graph 탭 비활성화
            GraphTab.Style = (Style)FindResource("TabButtonStyle");
            
            // 콘텐츠 전환
            TotalContent.Visibility = Visibility.Visible;
            GraphContent.Visibility = Visibility.Collapsed;
        }

        private void ZoneButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            
            // 모든 Zone 버튼 비활성화
            foreach (Button button in ZoneButtonsPanel.Children.OfType<Button>())
            {
                button.Style = (Style)FindResource("ZoneButtonStyle");
            }
            
            // 클릭된 버튼 활성화
            clickedButton.Style = (Style)FindResource("ActiveZoneButtonStyle");
        }

        private void SetPathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var pathSettingWindow = new PathSettingWindow("IPVS_PATHS", isDarkMode); // 현재 테마 상태 전달
                pathSettingWindow.Owner = Application.Current.MainWindow;
                pathSettingWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (pathSettingWindow.ShowDialog() == true)
                {
                    MessageBox.Show("IPVS 경로 설정이 완료되었습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"경로 설정 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}