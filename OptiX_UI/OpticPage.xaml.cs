using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace OptiX
{
    /// <summary>
    /// OpticPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class OpticPage : UserControl
    {
        public event EventHandler? BackRequested;
        
        private IniFileManager iniManager;
        private ObservableCollection<DataTableItem> dataItems;
        
        public OpticPage()
        {
            InitializeComponent();
            InitializeIniManager();
            LoadDataFromIni();
            InitializeDataTable();
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
                // MTP 섹션에서 Zone과 Category 읽기
                string zoneCountStr = iniManager.ReadValue("MTP", "Zone", "2");
                string categoriesStr = iniManager.ReadValue("MTP", "Category", "R,G,B");

                int zoneCount = int.Parse(zoneCountStr);
                string[] categories = categoriesStr.Split(',');

                dataItems = new ObservableCollection<DataTableItem>();

                // Zone과 Category에 따라 데이터 생성
                for (int zone = 1; zone <= zoneCount; zone++)
                {
                    for (int i = 0; i < categories.Length; i++)
                    {
                        string category = categories[i].Trim();
                        dataItems.Add(new DataTableItem
                        {
                            Zone = zone.ToString(), // 모든 행에 Zone 표시 (그룹화를 위해)
                            InnerId = "", // Inner ID는 빈 값으로 설정
                            CellId = "", // Cell ID는 빈 값으로 설정
                            Category = category,
                            X = "",
                            Y = "",
                            L = "",
                            Current = "",
                            Efficiency = "",
                            ErrorName = "",
                            Tact = "",
                            Judgment = "",
                            IsFirstInGroup = i == 0, // 그룹의 첫 번째 행인지 표시
                            GroupSize = categories.Length // 그룹 크기
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

        private void CreateZoneButtons()
        {
            // 기존 Zone 버튼들 제거
            ZoneButtonsPanel.Children.Clear();

            try
            {
                // INI 파일에서 Zone 개수 읽기
                string zoneCountStr = iniManager.ReadValue("MTP", "Zone", "2");
                int zoneCount = int.Parse(zoneCountStr);

                // Zone 개수만큼 동그라미 버튼 생성
                for (int i = 1; i <= zoneCount; i++)
                {
                    var zoneButton = new Button
                    {
                        Content = i.ToString(),
                        MinWidth = 40, // 원래 크기
                        Width = double.NaN, // 자동 크기 조정
                        Height = 40, // 원래 크기
                        FontSize = 16, // 원래 크기
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(8, 0, 8, 0), // 원래 크기
                        Tag = i
                    };

                    // 동그라미 모양으로 만들기
                    zoneButton.Template = CreateCircleButtonTemplate();
                    
                    // 첫 번째 버튼은 활성화 상태
                    if (i == 1)
                    {
                        zoneButton.Background = new SolidColorBrush(Color.FromRgb(32, 178, 170));
                        zoneButton.Foreground = Brushes.White;
                    }
                    else
                    {
                        zoneButton.Background = new SolidColorBrush(Color.FromRgb(225, 229, 233));
                        zoneButton.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
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

        private ControlTemplate CreateCircleButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            
            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "border";
            border.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderBrushProperty, new Binding("BorderBrush") { RelativeSource = RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderThicknessProperty, new Binding("BorderThickness") { RelativeSource = RelativeSource.TemplatedParent });
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(20)); // 원래 크기
            
            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetBinding(ContentPresenter.ContentProperty, new Binding("Content") { RelativeSource = RelativeSource.TemplatedParent });
            contentPresenter.SetBinding(ContentPresenter.HorizontalAlignmentProperty, new Binding("HorizontalContentAlignment") { RelativeSource = RelativeSource.TemplatedParent });
            contentPresenter.SetBinding(ContentPresenter.VerticalAlignmentProperty, new Binding("VerticalContentAlignment") { RelativeSource = RelativeSource.TemplatedParent });
            
            border.AppendChild(contentPresenter);
            template.VisualTree = border;
            
            return template;
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

                // INI에서 카테고리 개수 읽기
                string categoriesStr = iniManager.ReadValue("MTP", "Category", "R,G,B");
                string[] categories = categoriesStr.Split(',');
                int categoryCount = categories.Length;

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
                        Background = Brushes.White,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                        BorderThickness = new Thickness(0, 0, 1, 1)
                    };
                    var zoneText = new TextBlock
                    {
                        Text = firstItem.Zone,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontWeight = FontWeights.Bold,
                        FontSize = 14
                    };
                    zoneBorder.Child = zoneText;
                    Grid.SetRow(zoneBorder, DataTableGrid.RowDefinitions.Count - groupItems.Count);
                    Grid.SetColumn(zoneBorder, 0);
                    Grid.SetRowSpan(zoneBorder, groupItems.Count);
                    DataTableGrid.Children.Add(zoneBorder);

                    // Cell ID 열 (행 병합) - 실제 데이터 개수만큼 병합
                    var cellIdBorder = new Border
                    {
                        Background = Brushes.White,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                        BorderThickness = new Thickness(0, 0, 1, 1)
                    };
                    var cellIdText = new TextBlock
                    {
                        Text = firstItem.CellId,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 14
                    };
                    cellIdBorder.Child = cellIdText;
                    Grid.SetRow(cellIdBorder, DataTableGrid.RowDefinitions.Count - groupItems.Count);
                    Grid.SetColumn(cellIdBorder, 1);
                    Grid.SetRowSpan(cellIdBorder, groupItems.Count);
                    DataTableGrid.Children.Add(cellIdBorder);

                    // Inner ID 열 (행 병합) - 실제 데이터 개수만큼 병합
                    var innerIdBorder = new Border
                    {
                        Background = Brushes.White,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                        BorderThickness = new Thickness(0, 0, 1, 1)
                    };
                    var innerIdText = new TextBlock
                    {
                        Text = firstItem.InnerId,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 14
                    };
                    innerIdBorder.Child = innerIdText;
                    Grid.SetRow(innerIdBorder, DataTableGrid.RowDefinitions.Count - groupItems.Count);
                    Grid.SetColumn(innerIdBorder, 2);
                    Grid.SetRowSpan(innerIdBorder, groupItems.Count);
                    DataTableGrid.Children.Add(innerIdBorder);

                    // Error Name 열 (행 병합) - 실제 데이터 개수만큼 병합
                    var errorNameBorder = new Border
                    {
                        Background = Brushes.White,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                        BorderThickness = new Thickness(0, 0, 1, 1)
                    };
                    var errorNameText = new TextBlock
                    {
                        Text = firstItem.ErrorName,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 14
                    };
                    errorNameBorder.Child = errorNameText;
                    Grid.SetRow(errorNameBorder, DataTableGrid.RowDefinitions.Count - groupItems.Count);
                    Grid.SetColumn(errorNameBorder, 9);
                    Grid.SetRowSpan(errorNameBorder, groupItems.Count);
                    DataTableGrid.Children.Add(errorNameBorder);

                    // Tact 열 (행 병합) - 실제 데이터 개수만큼 병합
                    var tactBorder = new Border
                    {
                        Background = Brushes.White,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                        BorderThickness = new Thickness(0, 0, 1, 1)
                    };
                    var tactText = new TextBlock
                    {
                        Text = firstItem.Tact,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 14
                    };
                    tactBorder.Child = tactText;
                    Grid.SetRow(tactBorder, DataTableGrid.RowDefinitions.Count - groupItems.Count);
                    Grid.SetColumn(tactBorder, 10);
                    Grid.SetRowSpan(tactBorder, groupItems.Count);
                    DataTableGrid.Children.Add(tactBorder);

                    // 판정 열 (행 병합) - 실제 데이터 개수만큼 병합
                    var judgmentBorder = new Border
                    {
                        Background = Brushes.White,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                        BorderThickness = new Thickness(0, 0, 0, 1)
                    };
                    var judgmentText = new TextBlock
                    {
                        Text = firstItem.Judgment,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 14
                    };
                    judgmentBorder.Child = judgmentText;
                    Grid.SetRow(judgmentBorder, DataTableGrid.RowDefinitions.Count - groupItems.Count);
                    Grid.SetColumn(judgmentBorder, 11);
                    Grid.SetRowSpan(judgmentBorder, groupItems.Count);
                    DataTableGrid.Children.Add(judgmentBorder);

                    // 각 행별로 항목, x, y, L, 전류, 효율 열들 생성 - 실제 데이터 개수만큼
                    for (int i = 0; i < groupItems.Count; i++)
                    {
                        var item = groupItems[i];
                        int currentRow = DataTableGrid.RowDefinitions.Count - groupItems.Count + i;

                        // 항목 열
                        var categoryBorder = new Border
                        {
                            Background = Brushes.White,
                            BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                            BorderThickness = new Thickness(0, 0, 1, 1)
                        };
                        var categoryText = new TextBlock
                        {
                            Text = item.Category,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 14
                        };
                        categoryBorder.Child = categoryText;
                        Grid.SetRow(categoryBorder, currentRow);
                        Grid.SetColumn(categoryBorder, 3);
                        DataTableGrid.Children.Add(categoryBorder);

                        // x, y, L, 전류, 효율 열들
                        string[] values = { item.X, item.Y, item.L, item.Current, item.Efficiency };

                        for (int j = 0; j < values.Length; j++)
                        {
                            var border = new Border
                            {
                                Background = Brushes.White,
                                BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                                BorderThickness = new Thickness(0, 0, 1, 1)
                            };
                            var text = new TextBlock
                            {
                                Text = values[j],
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                FontSize = 14
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
            // Zone
            Border zoneHeader = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 178, 170)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                BorderThickness = new Thickness(0, 0, 1, 1)
            };
            TextBlock zoneHeaderText = new TextBlock
            {
                Text = "Zone",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            zoneHeader.Child = zoneHeaderText;
            Grid.SetRow(zoneHeader, 0);
            Grid.SetColumn(zoneHeader, 0);
            DataTableGrid.Children.Add(zoneHeader);
            
            // Cell ID
            Border cellIdHeader = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 178, 170)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                BorderThickness = new Thickness(0, 0, 1, 1)
            };
            TextBlock cellIdHeaderText = new TextBlock
            {
                Text = "Cell ID",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            cellIdHeader.Child = cellIdHeaderText;
            Grid.SetRow(cellIdHeader, 0);
            Grid.SetColumn(cellIdHeader, 1);
            DataTableGrid.Children.Add(cellIdHeader);
            
            // Inner ID
            Border innerIdHeader = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 178, 170)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                BorderThickness = new Thickness(0, 0, 1, 1)
            };
            TextBlock innerIdHeaderText = new TextBlock
            {
                Text = "Inner ID",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            innerIdHeader.Child = innerIdHeaderText;
            Grid.SetRow(innerIdHeader, 0);
            Grid.SetColumn(innerIdHeader, 2);
            DataTableGrid.Children.Add(innerIdHeader);
            
            // 항목
            Border categoryHeader = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 178, 170)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                BorderThickness = new Thickness(0, 0, 1, 1)
            };
            TextBlock categoryHeaderText = new TextBlock
            {
                Text = "항목",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            categoryHeader.Child = categoryHeaderText;
            Grid.SetRow(categoryHeader, 0);
            Grid.SetColumn(categoryHeader, 3);
            DataTableGrid.Children.Add(categoryHeader);
            
            // x
            Border xHeader = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 178, 170)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                BorderThickness = new Thickness(0, 0, 1, 1)
            };
            TextBlock xHeaderText = new TextBlock
            {
                Text = "x",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            xHeader.Child = xHeaderText;
            Grid.SetRow(xHeader, 0);
            Grid.SetColumn(xHeader, 4);
            DataTableGrid.Children.Add(xHeader);
            
            // y
            Border yHeader = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 178, 170)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                BorderThickness = new Thickness(0, 0, 1, 1)
            };
            TextBlock yHeaderText = new TextBlock
            {
                Text = "y",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            yHeader.Child = yHeaderText;
            Grid.SetRow(yHeader, 0);
            Grid.SetColumn(yHeader, 5);
            DataTableGrid.Children.Add(yHeader);
            
            // L
            Border lHeader = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 178, 170)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                BorderThickness = new Thickness(0, 0, 1, 1)
            };
            TextBlock lHeaderText = new TextBlock
            {
                Text = "L",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            lHeader.Child = lHeaderText;
            Grid.SetRow(lHeader, 0);
            Grid.SetColumn(lHeader, 6);
            DataTableGrid.Children.Add(lHeader);
            
            // 전류
            Border currentHeader = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 178, 170)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                BorderThickness = new Thickness(0, 0, 1, 1)
            };
            TextBlock currentHeaderText = new TextBlock
            {
                Text = "전류",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            currentHeader.Child = currentHeaderText;
            Grid.SetRow(currentHeader, 0);
            Grid.SetColumn(currentHeader, 7);
            DataTableGrid.Children.Add(currentHeader);
            
            // 효율
            Border efficiencyHeader = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 178, 170)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                BorderThickness = new Thickness(0, 0, 1, 1)
            };
            TextBlock efficiencyHeaderText = new TextBlock
            {
                Text = "효율",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            efficiencyHeader.Child = efficiencyHeaderText;
            Grid.SetRow(efficiencyHeader, 0);
            Grid.SetColumn(efficiencyHeader, 8);
            DataTableGrid.Children.Add(efficiencyHeader);
            
            // Error Name
            Border errorNameHeader = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 178, 170)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                BorderThickness = new Thickness(0, 0, 1, 1)
            };
            TextBlock errorNameHeaderText = new TextBlock
            {
                Text = "Error Name",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            errorNameHeader.Child = errorNameHeaderText;
            Grid.SetRow(errorNameHeader, 0);
            Grid.SetColumn(errorNameHeader, 9);
            DataTableGrid.Children.Add(errorNameHeader);
            
            // Tact
            Border tactHeader = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 178, 170)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                BorderThickness = new Thickness(0, 0, 1, 1)
            };
            TextBlock tactHeaderText = new TextBlock
            {
                Text = "Tact",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            tactHeader.Child = tactHeaderText;
            Grid.SetRow(tactHeader, 0);
            Grid.SetColumn(tactHeader, 10);
            DataTableGrid.Children.Add(tactHeader);
            
            // 판정
            Border judgmentHeader = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 178, 170)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(225, 229, 233)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            TextBlock judgmentHeaderText = new TextBlock
            {
                Text = "판정",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            judgmentHeader.Child = judgmentHeaderText;
            Grid.SetRow(judgmentHeader, 0);
            Grid.SetColumn(judgmentHeader, 11);
            DataTableGrid.Children.Add(judgmentHeader);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void GraphTab_Click(object sender, RoutedEventArgs e)
        {
            // Graph 탭 활성화
            GraphTab.Background = new SolidColorBrush(Color.FromRgb(32, 178, 170)); // #20B2AA
            GraphTab.Foreground = Brushes.White;
            
            // Total 탭 비활성화
            TotalTab.Background = new SolidColorBrush(Color.FromRgb(225, 229, 233)); // #E1E5E9
            TotalTab.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)); // #666666
            
            // 콘텐츠 전환
            GraphContent.Visibility = Visibility.Visible;
            TotalContent.Visibility = Visibility.Collapsed;
        }

        private void TotalTab_Click(object sender, RoutedEventArgs e)
        {
            // Total 탭 활성화
            TotalTab.Background = new SolidColorBrush(Color.FromRgb(32, 178, 170)); // #20B2AA
            TotalTab.Foreground = Brushes.White;
            
            // Graph 탭 비활성화
            GraphTab.Background = new SolidColorBrush(Color.FromRgb(225, 229, 233)); // #E1E5E9
            GraphTab.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)); // #666666
            
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
                button.Background = new SolidColorBrush(Color.FromRgb(225, 229, 233));
                button.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
            }
            
            // 클릭된 버튼 활성화
            clickedButton.Background = new SolidColorBrush(Color.FromRgb(32, 178, 170));
            clickedButton.Foreground = new SolidColorBrush(Colors.White);
        }
    }

    // 데이터 테이블 아이템 클래스
    public class DataTableItem
    {
        public string Zone { get; set; } = "";
        public string InnerId { get; set; } = "";
        public string CellId { get; set; } = "";
        public string Category { get; set; } = "";
        public string X { get; set; } = "";
        public string Y { get; set; } = "";
        public string L { get; set; } = "";
        public string Current { get; set; } = "";
        public string Efficiency { get; set; } = "";
        public string ErrorName { get; set; } = "";
        public string Tact { get; set; } = "";
        public string Judgment { get; set; } = "";
        public bool IsFirstInGroup { get; set; } = false;
        public int GroupSize { get; set; } = 1;
    }
}
