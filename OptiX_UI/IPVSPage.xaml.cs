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
        public event EventHandler BackRequested;
        
        private IniFileManager iniManager;
        private ObservableCollection<DataTableItem> dataItems;
        private bool isDarkMode = false;
        private List<string> wadValues = new List<string>();
        private int currentSelectedZone = 1; // 현재 선택된 Zone (기본값: 1)
        
        public IPVSPage()
        {
            InitializeComponent();
            InitializeIniManager();
            LoadDataFromIni();
            InitializeDataTable();
            LoadThemeFromIni();
            InitializeWAD();
            
            // 초기 언어 적용
            Loaded += (s, e) => ApplyLanguage();
        }


        private void InitializeIniManager()
        {
            // 실행 파일 기준 상대 경로로 INI 파일 찾기
        string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string exeDir = System.IO.Path.GetDirectoryName(exePath);
        string iniPath = @"D:\\Project\\Recipe\\OptiX.ini";
            iniManager = new IniFileManager(iniPath);
        }

        private void LoadDataFromIni()
        {
            try
            {
                // Settings 섹션에서 IPVS_ZONE과 MAX_POINT 읽기
                string zoneCountStr = GlobalDataManager.GetValue("Settings", "IPVS_ZONE", "2");
                string maxPointStr = GlobalDataManager.GetValue("IPVS", "MAX_POINT", "5");

                int zoneCount = int.Parse(zoneCountStr);
                int maxPoint = int.Parse(maxPointStr);

                dataItems = new ObservableCollection<DataTableItem>();

                // Zone과 Point에 따라 데이터 생성
                for (int zone = 1; zone <= zoneCount; zone++)
                {
                    for (int point = 1; point <= maxPoint; point++)
                    {
                        // IPVS 섹션에서 Cell ID와 Inner ID 로드 (초기에는 빈 값)
                        string cellId = ""; // IPVS는 TEST START 후에만 데이터 표시
                        string innerId = ""; // IPVS는 TEST START 후에만 데이터 표시
                        
                        dataItems.Add(new DataTableItem
                        {
                            Zone = zone.ToString(), // 모든 행에 Zone 표시 (그룹화를 위해)
                            InnerId = innerId, // MTP 섹션에서 로드한 Inner ID
                            CellId = cellId, // MTP 섹션에서 로드한 Cell ID
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
                ThemeManager.UpdateDynamicColors(this, true);
                UpdateTableColors(true);
            }
            else
            {
                // 라이트모드 색상 적용
                ThemeManager.UpdateDynamicColors(this, false);
                UpdateTableColors(false);
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
        
        public void SetDarkMode(bool isDarkMode)
        {
            this.isDarkMode = isDarkMode;
            
            if (isDarkMode)
            {
                ThemeManager.UpdateDynamicColors(this, true);
            }
            else
            {
                ThemeManager.UpdateDynamicColors(this, false);
            }
            
            // 테이블을 다시 생성하여 올바른 색상 적용
            InitializeDataTable();
        }
        

        private void CreateZoneButtons()
        {
            // 기존 Zone 버튼들 제거
            ZoneButtonsPanel.Children.Clear();

            try
            {
                // Settings에서 IPVS_ZONE 개수 읽기
                string zoneCountStr = iniManager.ReadValue("Settings", "IPVS_ZONE", "2");
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
                        Tag = i - 1  // 0-based index로 저장
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

        /// <summary>
        /// IPVS Setting 버튼 클릭 - CellIdInputWindow 열기 (IPVS 섹션 사용)
        /// </summary>
        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // IPVS용 설정 창 열기 (현재 선택된 Zone, IPVS 섹션 사용)
                var settingWindow = new CellIdInputWindow(currentSelectedZone, isDarkMode, "IPVS");
                settingWindow.Owner = System.Windows.Application.Current.MainWindow;
                settingWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;

                // Non-Modal로 열기 (메인 프로그램 계속 동작)
                settingWindow.Show();
                
                System.Diagnostics.Debug.WriteLine($"IPVS Zone {currentSelectedZone} 설정 창 열림");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"IPVS 설정 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// IPVS Path 버튼 클릭 - PathSettingWindow 열기 (IPVS_PATHS 섹션 사용)
        /// </summary>
        private void PathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // IPVS용 경로 설정 창 열기 (IPVS_PATHS 섹션 사용)
                var pathWindow = new PathSettingWindow("IPVS_PATHS", isDarkMode);
                pathWindow.Owner = System.Windows.Application.Current.MainWindow;
                pathWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;

                // Non-Modal로 열기 (메인 프로그램 계속 동작)
                pathWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"IPVS 경로 설정 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void InitializeWAD()
        {
            try
            {
                // INI 파일에서 WAD 값들 읽어오기 (IPVS 섹션에서)
                string wadString = iniManager.ReadValue("IPVS", "WAD", "0,15,30,45,60");
                wadValues = wadString.Split(',').Select(x => x.Trim()).ToList();
                
                // 콤보박스에 WAD 값들 추가
                WadComboBox.Items.Clear();
                foreach (string wad in wadValues)
                {
                    WadComboBox.Items.Add(wad);
                }
                
                // 첫 번째 값을 기본 선택으로 설정
                if (WadComboBox.Items.Count > 0)
                {
                    WadComboBox.SelectedIndex = 0;
                }
                
                System.Diagnostics.Debug.WriteLine($"IPVS WAD 초기화 완료: {string.Join(", ", wadValues)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IPVS WAD 초기화 오류: {ex.Message}");
                // 오류 시 기본값 설정
                wadValues = new List<string> { "0", "15", "30", "45", "60" };
                WadComboBox.Items.Clear();
                foreach (string wad in wadValues)
                {
                    WadComboBox.Items.Add(wad);
                }
                if (WadComboBox.Items.Count > 0)
                {
                    WadComboBox.SelectedIndex = 0;
                }
            }
        }

        private void WadComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (WadComboBox.SelectedItem != null)
                {
                    string selectedWad = WadComboBox.SelectedItem.ToString();
                    System.Diagnostics.Debug.WriteLine($"IPVS WAD 선택됨: {selectedWad}");
                    
                    // WAD 값이 변경되었을 때 필요한 작업 수행
                    // 예: 데이터 테이블 업데이트, 그래프 업데이트 등
                    UpdateDataTableForWAD(selectedWad);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IPVS WAD 선택 변경 오류: {ex.Message}");
            }
        }

        private void UpdateDataTableForWAD(string selectedWad)
        {
            try
            {
                // WAD 값에 따라 데이터 테이블 업데이트
                // 여기에 WAD에 따른 데이터 처리 로직 구현
                System.Diagnostics.Debug.WriteLine($"IPVS 데이터 테이블을 WAD {selectedWad}에 맞게 업데이트");
                
                // 예시: 데이터 테이블의 특정 컬럼 업데이트
                // 실제 구현은 데이터 구조에 따라 달라질 수 있음
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IPVS 데이터 테이블 업데이트 오류: {ex.Message}");
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // WAD 콤보박스를 첫 번째 값으로 리셋
                if (WadComboBox.Items.Count > 0)
                {
                    WadComboBox.SelectedIndex = 0;
                    System.Diagnostics.Debug.WriteLine("IPVS WAD가 리셋되었습니다.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IPVS WAD 리셋 오류: {ex.Message}");
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

        private void ZoneButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            
            // 현재 선택된 Zone 업데이트 (Tag에서 0-based index를 가져와서 1-based로 변환)
            if (clickedButton.Tag != null && int.TryParse(clickedButton.Tag.ToString(), out int zoneIndex))
            {
                currentSelectedZone = zoneIndex + 1; // 0-based → 1-based
                System.Diagnostics.Debug.WriteLine($"IPVS Zone {currentSelectedZone} 선택됨");
            }
            
            // 모든 Zone 버튼 비활성화
            foreach (Button button in ZoneButtonsPanel.Children.OfType<Button>())
            {
                button.Style = (Style)FindResource("ZoneButtonStyle");
            }
            
            // 클릭된 버튼 활성화
            clickedButton.Style = (Style)FindResource("ActiveZoneButtonStyle");
        }


        /// <summary>
        /// IPVS Test Start 버튼 클릭 - IPVS 섹션에서 Cell ID와 Inner ID 로드
        /// </summary>
        private void TestStartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // IPVS 섹션에서 Cell ID와 Inner ID 로드하여 테이블 업데이트
                string zoneCountStr = GlobalDataManager.GetValue("Settings", "IPVS_ZONE", "2");
                string maxPointStr = GlobalDataManager.GetValue("IPVS", "MAX_POINT", "9");
                
                int zoneCount = int.Parse(zoneCountStr);
                int maxPoint = int.Parse(maxPointStr);
                
                // 기존 데이터 업데이트
                foreach (var item in dataItems)
                {
                    int zone = int.Parse(item.Zone);
                    string cellId = GlobalDataManager.GetValue("IPVS", $"CELL_ID_ZONE_{zone}", "");
                    string innerId = GlobalDataManager.GetValue("IPVS", $"INNER_ID_ZONE_{zone}", "");
                    
                    item.CellId = cellId;
                    item.InnerId = innerId;
                }
                
                // 테이블 다시 그리기
                InitializeDataTable();
                
                MessageBox.Show("IPVS 테스트가 시작되었습니다!", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"IPVS 테스트 시작 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// IPVSPage에 언어 적용
        /// </summary>
        public void ApplyLanguage()
        {
            try
            {
                // 뒤로가기 버튼
                if (BackButton != null)
                {
                    var textBlock = BackButton.Content as TextBlock;
                    if (textBlock != null)
                        textBlock.Text = LanguageManager.GetText("IPVSPage.Back");
                }

                // WAD 라벨
                var wadLabel = FindName("WadLabel") as System.Windows.Controls.TextBlock;
                if (wadLabel != null)
                    wadLabel.Text = LanguageManager.GetText("IPVSPage.WAD");

                // RESET 버튼
                if (ResetButton != null)
                    ResetButton.Content = LanguageManager.GetText("IPVSPage.Reset");

                // 컨트롤 패널 버튼들
                var settingButton = FindName("SettingButton") as System.Windows.Controls.Button;
                if (settingButton != null)
                    settingButton.Content = LanguageManager.GetText("IPVSPage.Setting");

                var pathButton = FindName("PathButton") as System.Windows.Controls.Button;
                if (pathButton != null)
                    pathButton.Content = LanguageManager.GetText("IPVSPage.Path");

                var startButton = FindName("StartButton") as System.Windows.Controls.Button;
                if (startButton != null)
                    startButton.Content = LanguageManager.GetText("IPVSPage.Start");

                var stopButton = FindName("StopButton") as System.Windows.Controls.Button;
                if (stopButton != null)
                    stopButton.Content = LanguageManager.GetText("IPVSPage.Stop");

                var chartButton = FindName("ChartButton") as System.Windows.Controls.Button;
                if (chartButton != null)
                    chartButton.Content = LanguageManager.GetText("IPVSPage.Chart");

                var reportButton = FindName("ReportButton") as System.Windows.Controls.Button;
                if (reportButton != null)
                    reportButton.Content = LanguageManager.GetText("IPVSPage.Report");

                var exitButton = FindName("ExitButton") as System.Windows.Controls.Button;
                if (exitButton != null)
                    exitButton.Content = LanguageManager.GetText("IPVSPage.Exit");

                // 특성 판정 현황 제목
                var judgmentStatusTitle = FindName("JudgmentStatusTitle") as System.Windows.Controls.TextBlock;
                if (judgmentStatusTitle != null)
                    judgmentStatusTitle.Text = LanguageManager.GetText("IPVSPage.CharacteristicJudgmentStatus");

                // 수량, 발생률 헤더
                var quantityHeader = FindName("QuantityHeader") as System.Windows.Controls.TextBlock;
                if (quantityHeader != null)
                    quantityHeader.Text = LanguageManager.GetText("IPVSPage.Quantity");

                var occurrenceRateHeader = FindName("OccurrenceRateHeader") as System.Windows.Controls.TextBlock;
                if (occurrenceRateHeader != null)
                    occurrenceRateHeader.Text = LanguageManager.GetText("IPVSPage.OccurrenceRate");

                // 컨트롤 패널 제목
                var controlPanelTitle = FindName("ControlPanelTitle") as System.Windows.Controls.TextBlock;
                if (controlPanelTitle != null)
                    controlPanelTitle.Text = LanguageManager.GetText("IPVSPage.ControlPanel");

                // 데이터 테이블 제목 (IPVS는 고정, 데이터 테이블만 동적)
                var dataTableTitle = FindName("DataTableTitle") as System.Windows.Controls.TextBlock;
                if (dataTableTitle != null)
                    dataTableTitle.Text = $"📊 IPVS {LanguageManager.GetText("IPVSPage.DataTable")}";

                System.Diagnostics.Debug.WriteLine($"IPVSPage 언어 적용 완료: {LanguageManager.CurrentLanguage}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IPVSPage 언어 적용 오류: {ex.Message}");
            }
        }
    }
}