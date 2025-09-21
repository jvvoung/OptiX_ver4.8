using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OptiX.ViewModels;

namespace OptiX
{
    public partial class OpticPage : UserControl
    {
        public event EventHandler BackRequested;
        private OpticPageViewModel viewModel;
        
        public OpticPage()
        {
            InitializeComponent();
            viewModel = new OpticPageViewModel();
            DataContext = viewModel;

            // DataItems 변경 감지 (원래대로)
            viewModel.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(viewModel.DataItems))
                {
                    // DataItems가 변경되었을 때만 테이블 다시 그리기
                    CreateDataRows();
                }
            };

            // 테이블 생성 및 Zone 버튼 생성
            Loaded += (s, e) => {
                CreateCustomTable();
                CreateZoneButtons();
            };
        }

        public void SetDarkMode(bool isDarkMode)
        {
            // ViewModel에 다크모드 상태 전달
            if (viewModel != null)
            {
                viewModel.IsDarkMode = isDarkMode;
            }
            
            // IPVSPage와 동일한 방식으로 다크모드 적용
            if (isDarkMode)
            {
                UpdateDynamicColors(true);
            }
            else
            {
                UpdateDynamicColors(false);
            }
            
            // 테이블과 Zone 버튼을 다시 생성하여 올바른 색상 적용 (IPVSPage와 동일)
            CreateCustomTable();
            CreateZoneButtons();
        }
        
        private void UpdateDynamicColors(bool isDark)
        {
            // IPVSPage와 동일한 동적 색상 팔레트 업데이트
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

        private void CreateCustomTable()
        {
            try
            {
                // 기존 내용 클리어
                DataTableGrid.RowDefinitions.Clear();
                DataTableGrid.Children.Clear();

                // 헤더 행 추가
                CreateHeaderRow();

                // 데이터 행들 추가
                CreateDataRows();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"커스텀 테이블 생성 오류: {ex.Message}");
            }
        }

        private void CreateHeaderRow()
        {
            // 헤더 행 정의
            DataTableGrid.RowDefinitions.Add(new RowDefinition { Height = new System.Windows.GridLength(45) });

            string[] headers = { "Zone", "Inner ID", "Cell ID", "Category", "X", "Y", "L", "Current", "Efficiency", "Error Name", "Tact", "Judgment" };
            
            for (int i = 0; i < headers.Length; i++)
            {
                var headerBorder = new Border
                {
                    Background = (System.Windows.Media.SolidColorBrush)FindResource("PrimaryColor"),
                    BorderBrush = (System.Windows.Media.SolidColorBrush)FindResource("PrimaryColor"),
                    BorderThickness = new System.Windows.Thickness(1),
                    Child = new TextBlock
                    {
                        Text = headers[i],
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        FontWeight = System.Windows.FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Colors.White),
                        FontSize = 13
                    }
                };

                Grid.SetColumn(headerBorder, i);
                Grid.SetRow(headerBorder, 0);
                DataTableGrid.Children.Add(headerBorder);
            }
        }

        private void CreateDataRows()
        {
            if (viewModel?.DataItems == null || viewModel.DataItems.Count == 0) return;

            System.Diagnostics.Debug.WriteLine($"CreateDataRows: 전체 아이템 개수 = {viewModel.DataItems.Count}");

            // 기존 데이터 행들만 제거 (헤더는 유지)
            var existingDataChildren = DataTableGrid.Children.Cast<UIElement>()
                .Where(child => Grid.GetRow(child) > 0).ToList();

            foreach (var child in existingDataChildren)
            {
                DataTableGrid.Children.Remove(child);
            }

            // 기존 행 정의들 제거 (헤더 행은 유지)
            while (DataTableGrid.RowDefinitions.Count > 1)
            {
                DataTableGrid.RowDefinitions.RemoveAt(DataTableGrid.RowDefinitions.Count - 1);
            }

            // Zone별로 그룹화 (모든 Zone 표시)
            var groupedData = viewModel.DataItems.GroupBy(item => item.Zone).ToList();

            foreach (var zoneGroup in groupedData)
            {
                var zoneItems = zoneGroup.ToList();
                var firstItem = zoneItems.First();

                // 각 Zone의 카테고리 개수만큼 행 생성
                for (int i = 0; i < zoneItems.Count; i++)
                {
                    DataTableGrid.RowDefinitions.Add(new RowDefinition { Height = new System.Windows.GridLength(32) });
                    int currentRow = DataTableGrid.RowDefinitions.Count - 1;
                    var currentItem = zoneItems[i];

                    // Zone 컬럼 (첫 번째 행에서만 표시, RowSpan 적용)
                    if (i == 0)
                    {
                        var zoneBorder = new Border
                        {
                            Background = (System.Windows.Media.SolidColorBrush)FindResource("DynamicSurfaceColor"),
                            BorderBrush = (System.Windows.Media.SolidColorBrush)FindResource("DynamicBorderColor"),
                            BorderThickness = new System.Windows.Thickness(1),
                            Child = new TextBlock
                            {
                                Text = firstItem.Zone,
                                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                                FontWeight = System.Windows.FontWeights.SemiBold,
                                Foreground = (System.Windows.Media.SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                            }
                        };
                        Grid.SetColumn(zoneBorder, 0);
                        Grid.SetRow(zoneBorder, currentRow);
                        Grid.SetRowSpan(zoneBorder, zoneItems.Count);
                        DataTableGrid.Children.Add(zoneBorder);
                    }

                    // Inner ID 컬럼 (첫 번째 행에서만 표시, RowSpan 적용)
                    if (i == 0)
                    {
                        var innerIdBorder = new Border
                        {
                            Background = (System.Windows.Media.SolidColorBrush)FindResource("DynamicSurfaceColor"),
                            BorderBrush = (System.Windows.Media.SolidColorBrush)FindResource("DynamicBorderColor"),
                            BorderThickness = new System.Windows.Thickness(1),
                            Child = new TextBlock
                            {
                                Text = firstItem.InnerId,
                                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                                FontWeight = System.Windows.FontWeights.Medium,
                                Foreground = (System.Windows.Media.SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                            }
                        };
                        Grid.SetColumn(innerIdBorder, 1);
                        Grid.SetRow(innerIdBorder, currentRow);
                        Grid.SetRowSpan(innerIdBorder, zoneItems.Count);
                        DataTableGrid.Children.Add(innerIdBorder);
                    }

                    // Cell ID 컬럼 (첫 번째 행에서만 표시, RowSpan 적용)
                    if (i == 0)
                    {
                        var cellIdBorder = new Border
                        {
                            Background = (System.Windows.Media.SolidColorBrush)FindResource("DynamicSurfaceColor"),
                            BorderBrush = (System.Windows.Media.SolidColorBrush)FindResource("DynamicBorderColor"),
                            BorderThickness = new System.Windows.Thickness(1),
                            Child = new TextBlock
                            {
                                Text = firstItem.CellId,
                                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                                FontWeight = System.Windows.FontWeights.Medium,
                                Foreground = (System.Windows.Media.SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                            }
                        };
                        Grid.SetColumn(cellIdBorder, 2);
                        Grid.SetRow(cellIdBorder, currentRow);
                        Grid.SetRowSpan(cellIdBorder, zoneItems.Count);
                        DataTableGrid.Children.Add(cellIdBorder);
                    }

                    // Category 컬럼 (각 행마다 개별 표시)
                    var categoryBorder = new Border
                    {
                        Background = (System.Windows.Media.SolidColorBrush)FindResource("DynamicSurfaceColor"),
                        BorderBrush = (System.Windows.Media.SolidColorBrush)FindResource("DynamicBorderColor"),
                        BorderThickness = new System.Windows.Thickness(1),
                        Child = new TextBlock
                        {
                            Text = currentItem.Category,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                            VerticalAlignment = System.Windows.VerticalAlignment.Center,
                            FontWeight = System.Windows.FontWeights.Medium,
                            Foreground = (System.Windows.Media.SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                        }
                    };
                    Grid.SetColumn(categoryBorder, 3);
                    Grid.SetRow(categoryBorder, currentRow);
                    DataTableGrid.Children.Add(categoryBorder);

                    // X, Y, L, Current, Efficiency 컬럼들 (각 행마다 개별 표시)
                    string[] dataValues = { currentItem.X, currentItem.Y, currentItem.L, currentItem.Current, currentItem.Efficiency };
                    int[] dataColumns = { 4, 5, 6, 7, 8 };

                    for (int j = 0; j < dataValues.Length; j++)
                    {
                        var dataBorder = new Border
                        {
                            Background = (System.Windows.Media.SolidColorBrush)FindResource("DynamicSurfaceColor"),
                            BorderBrush = (System.Windows.Media.SolidColorBrush)FindResource("DynamicBorderColor"),
                            BorderThickness = new System.Windows.Thickness(1),
                            Child = new TextBlock
                            {
                                Text = dataValues[j],
                                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                                FontWeight = System.Windows.FontWeights.Normal,
                                Foreground = (System.Windows.Media.SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                            }
                        };
                        Grid.SetColumn(dataBorder, dataColumns[j]);
                        Grid.SetRow(dataBorder, currentRow);
                        DataTableGrid.Children.Add(dataBorder);
                    }

                    // Error Name, Tact, Judgment 컬럼들 (첫 번째 행에서만 표시, RowSpan 적용)
                    if (i == 0)
                    {
                        string[] mergedValues = { firstItem.ErrorName, firstItem.Tact, firstItem.Judgment };
                        int[] mergedColumns = { 9, 10, 11 };

                        for (int k = 0; k < mergedValues.Length; k++)
                        {
                            var mergedBorder = new Border
                            {
                                Background = (System.Windows.Media.SolidColorBrush)FindResource("DynamicSurfaceColor"),
                                BorderBrush = (System.Windows.Media.SolidColorBrush)FindResource("DynamicBorderColor"),
                                BorderThickness = new System.Windows.Thickness(1),
                                Child = new TextBlock
                                {
                                    Text = mergedValues[k],
                                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                                    FontWeight = System.Windows.FontWeights.Medium,
                                    Foreground = (System.Windows.Media.SolidColorBrush)FindResource("DynamicTextPrimaryColor")
                                }
                            };
                            Grid.SetColumn(mergedBorder, mergedColumns[k]);
                            Grid.SetRow(mergedBorder, currentRow);
                            Grid.SetRowSpan(mergedBorder, zoneItems.Count);
                            DataTableGrid.Children.Add(mergedBorder);
                        }
                    }
                }
            }
        }

        private void CreateZoneButtons()
        {
            try
            {
                var zoneButtonsPanel = this.FindName("ZoneButtonsPanel") as StackPanel;
                if (zoneButtonsPanel == null) return;

                // 기존 버튼들 제거
                zoneButtonsPanel.Children.Clear();

                // INI에서 Zone 개수 읽기
                string zoneCountStr = viewModel.iniManager.ReadValue("MTP", "Zone", "2");
                int zoneCount = int.Parse(zoneCountStr);

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
                        Tag = i - 1 // 0-based index
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

                    zoneButton.Click += (s, e) => {
                        if (s is Button btn && btn.Tag is int zoneIndex)
                        {
                            viewModel.CurrentZone = zoneIndex;

                            // 모든 Zone 버튼 스타일 초기화
                            foreach (var child in zoneButtonsPanel.Children)
                            {
                                if (child is Button childBtn)
                                {
                                    childBtn.Style = (Style)FindResource("ZoneButtonStyle");
                                }
                            }

                            // 선택된 버튼 스타일 변경
                            btn.Style = (Style)FindResource("ActiveZoneButtonStyle");

                            // CreateCustomTable() 호출 제거 - 테이블을 다시 그리지 않음
                            System.Diagnostics.Debug.WriteLine($"Zone {zoneIndex + 1} 선택됨. 테이블 재생성 안함.");
                        }
                    };

                    zoneButtonsPanel.Children.Add(zoneButton);
                }

                // 첫 번째 Zone을 기본 선택
                if (zoneButtonsPanel.Children.Count > 0)
                {
                    viewModel.CurrentZone = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zone 버튼 생성 오류: {ex.Message}");
            }
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
    }
}
