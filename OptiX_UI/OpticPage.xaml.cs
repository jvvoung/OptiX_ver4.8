using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using OptiX.ViewModels;
using OptiX.Models;
using System.Security.Policy;

namespace OptiX
{
    // WAD 각도 enum 정의 (원래 구조체 주석과 일치)
    public enum WadAngle
    {
        Angle0 = 0,    // 0도
        Angle30 = 1,   // 30도
        Angle45 = 2,   // 45도
        Angle60 = 3,   // 60도
        Angle15 = 4,   // 15도
        AngleA = 5,    // A도
        AngleB = 6     // B도
    }

    public partial class OpticPage : UserControl
    {
        private OpticPageViewModel viewModel;
        private bool isTestStarted = false; // 전역 테스트 시작 상태 (기존 호환성 유지)
        private bool[] zoneTestCompleted; // Zone별 테스트 완료 상태 배열
        
        public OpticPage()
        {
            InitializeComponent();
            viewModel = new OpticPageViewModel(this); // 자기 자신을 전달
            DataContext = viewModel;
            
            // Zone별 테스트 완료 상태 배열 초기화
            InitializeZoneTestStates();

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
                InitializeWadComboBox();
                ApplyLanguage(); // 초기 언어 적용
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
                ThemeManager.UpdateDynamicColors(this, true);
            }
            else
            {
                ThemeManager.UpdateDynamicColors(this, false);
            }
            
            // 테이블과 Zone 버튼을 다시 생성하여 올바른 색상 적용 (IPVSPage와 동일)
            CreateCustomTable();
            CreateZoneButtons();
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

            string[] headers = { "Zone", "Cell ID", "Inner ID", "Category", "X", "Y", "L", "Current", "Efficiency", "Error Name", "Tact", "Judgment" };
            
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
                        Grid.SetColumn(cellIdBorder, 1);
                        Grid.SetRow(cellIdBorder, currentRow);
                        Grid.SetRowSpan(cellIdBorder, zoneItems.Count);
                        DataTableGrid.Children.Add(cellIdBorder);
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
                        Grid.SetColumn(innerIdBorder, 2);
                        Grid.SetRow(innerIdBorder, currentRow);
                        Grid.SetRowSpan(innerIdBorder, zoneItems.Count);
                        DataTableGrid.Children.Add(innerIdBorder);
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

                // Settings에서 MTP_ZONE 개수 읽기
                string zoneCountStr = viewModel.iniManager.ReadValue("Settings", "MTP_ZONE", "2");
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
                            System.Diagnostics.Debug.WriteLine($"=== Zone 버튼 클릭 ===");
                            System.Diagnostics.Debug.WriteLine($"클릭된 버튼: Zone {i} (Tag: {zoneIndex})");
                            System.Diagnostics.Debug.WriteLine($"이전 CurrentZone: {viewModel.CurrentZone}");
                            
                            viewModel.CurrentZone = zoneIndex;
                            
                            System.Diagnostics.Debug.WriteLine($"새로운 CurrentZone: {viewModel.CurrentZone}");
                            System.Diagnostics.Debug.WriteLine($"업데이트될 targetZone: {zoneIndex + 1}");

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

        private void InitializeWadComboBox()
        {
            try
            {
                // WAD 콤보박스에 아이템 추가
                WadComboBox.Items.Clear();
                
                // INI 파일에서 WAD 값 읽기
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string exeDir = System.IO.Path.GetDirectoryName(exePath);
                string iniPath = @"D:\\Project\\Recipe\\OptiX.ini";
                
                var iniManager = new IniFileManager(iniPath);
                string wadValues = iniManager.ReadValue("MTP", "WAD", "0,15,30,45");
                
                // 쉼표로 분리하여 배열로 변환
                string[] wadArray = wadValues.Split(',');
                
                // 각 WAD 값에 대해 아이템 추가
                foreach (string wadValue in wadArray)
                {
                    string trimmedValue = wadValue.Trim();
                    if (!string.IsNullOrEmpty(trimmedValue))
                    {
                        WadComboBox.Items.Add(trimmedValue);
                    }
                }
                
                // 기본값 설정
                WadComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WAD 콤보박스 초기화 오류: {ex.Message}");
                
                // 오류 발생 시 기본값 사용
                WadComboBox.Items.Clear();
                string[] defaultWadArray = { "0", "15", "30", "45" };
                foreach (string wadValue in defaultWadArray)
                {
                    WadComboBox.Items.Add(wadValue);
                }
                WadComboBox.SelectedIndex = 0;
            }
        }

        private void WadComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (WadComboBox.SelectedItem != null)
                {
                    string selectedWad = WadComboBox.SelectedItem.ToString();
                    
                    // WAD 값을 배열 인덱스로 변환
                    int wadIndex = GetWadArrayIndex(selectedWad);
                    
                    System.Diagnostics.Debug.WriteLine($"WAD 값 '{selectedWad}'이 선택되었습니다. 배열 인덱스: {wadIndex}");
                    
                    // 선택된 WAD에 해당하는 데이터로 UI 업데이트
                    UpdateDataForWad(wadIndex);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WAD 콤보박스 선택 변경 오류: {ex.Message}");
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("RESET 버튼 클릭됨");
                
                // 모든 Zone의 테스트 완료 상태 초기화
                for (int i = 0; i < zoneTestCompleted.Length; i++)
                {
                    zoneTestCompleted[i] = false;
                }
                
                // 전역 테스트 상태도 초기화
                isTestStarted = false;
                
                // 모든 측정값 클리어
                ClearMeasurementValues();
                
                // 테이블 다시 그리기
                CreateDataRows();
                
                System.Diagnostics.Debug.WriteLine("모든 데이터가 초기화되었습니다.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RESET 버튼 클릭 오류: {ex.Message}");
            }
        }

        private WadAngle GetWadAngle(string wadValue)
        {
            // INI 파일의 WAD 값에 따른 WadAngle enum 매핑
            switch (wadValue)
            {
                case "0": return WadAngle.Angle0;   // 0도
                case "15": return WadAngle.Angle15; // 15도
                case "30": return WadAngle.Angle30; // 30도
                case "45": return WadAngle.Angle45; // 45도
                case "60": return WadAngle.Angle60; // 60도
                case "A": return WadAngle.AngleA;   // A도
                case "B": return WadAngle.AngleB;   // B도
                default: return WadAngle.Angle0;    // 기본값은 0도
            }
        }

        private int GetWadArrayIndex(string wadValue)
        {
            // WadAngle enum을 배열 인덱스로 변환
            WadAngle angle = GetWadAngle(wadValue);
            return (int)angle;
        }

        private int GetPatternArrayIndex(string category)
        {
            // Category에 따른 패턴 배열 인덱스 매핑
            // [17]:패턴 => 0:W, 1:R, 2:G, 3:B, 4:WG, 5:WG2, 6:WG3 ~ 16:WG13
            switch (category)
            {
                case "W": return 0;   // W
                case "R": return 1;   // R
                case "G": return 2;   // G
                case "B": return 3;   // B
                case "WG": return 4;  // WG
                case "WG2": return 5; // WG2
                case "WG3": return 6; // WG3
                case "WG4": return 7; // WG4
                case "WG5": return 8; // WG5
                case "WG6": return 9; // WG6
                case "WG7": return 10; // WG7
                case "WG8": return 11; // WG8
                case "WG9": return 12; // WG9
                case "WG10": return 13; // WG10
                case "WG11": return 14; // WG11
                case "WG12": return 15; // WG12
                case "WG13": return 16; // WG13
                default: return 0;    // 기본값은 W
            }
        }

        private void UpdateDataForWad(int wadIndex)
        {
            try
            {
                // WadAngle enum으로 변환
                WadAngle angle = (WadAngle)wadIndex;
                
                System.Diagnostics.Debug.WriteLine($"WAD 각도 {angle} (인덱스: {wadIndex})에 해당하는 데이터로 UI 업데이트");
                int currentZone = viewModel.CurrentZone;
                // 테스트가 진행 중이거나 어떤 Zone이라도 테스트 완료된 경우
                if (isTestStarted&& !zoneTestCompleted[currentZone])
                {
                    // 테스트가 진행 중인 경우: 현재 선택된 Zone만 업데이트
                    
                    System.Diagnostics.Debug.WriteLine($"테스트 진행 중. 현재 Zone {currentZone + 1}만 업데이트");
                    GenerateDataFromStruct(wadIndex, currentZone);
                }
                else if (zoneTestCompleted.Any(completed => completed))
                {
                    // 테스트 완료된 모든 Zone들을 업데이트
                    for (int zone = 0; zone < zoneTestCompleted.Length; zone++)
                    {
                        if (zoneTestCompleted[zone])
                        {
                            System.Diagnostics.Debug.WriteLine($"Zone {zone + 1}이 테스트 완료됨. WAD {wadIndex}로 데이터 업데이트");
                            GenerateDataFromStruct(wadIndex, zone);
                        }
                    }
                }
                else
                {
                    // 테스트가 시작되지 않았고 어떤 Zone도 완료되지 않았으면 빈 데이터로 유지
                    ClearMeasurementValues();
                }
                
                // 테이블 다시 그리기
                CreateDataRows();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WAD 데이터 업데이트 오류: {ex.Message}");
            }
        }

        private void GenerateDataFromStruct(int wadIndex, int zoneIndex)
        {
            // 구조체 data[wadIndex][patternIndex]에 맞는 데이터 생성
            // 실제로는 DLL에서 data[wadIndex][patternIndex]를 가져와야 함
            
            if (viewModel?.DataItems == null) return;
            
            try
            {
                // INI 파일에서 설정 읽기
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string exeDir = System.IO.Path.GetDirectoryName(exePath);
                string iniPath = @"D:\\Project\\Recipe\\OptiX.ini";
                
                var iniManager = new IniFileManager(iniPath);
                
                // Category 목록 읽기
                string categoryStr = GlobalDataManager.GetValue("MTP", "Category", "W,R,G,B");
                string[] categories = categoryStr.Split(',').Select(c => c.Trim()).ToArray();
                
                // 지정된 Zone만 업데이트 (zoneIndex는 0-based)
                int targetZone = zoneIndex + 1; // 1-based로 변환
                System.Diagnostics.Debug.WriteLine($"Zone {targetZone} (인덱스: {zoneIndex}) 업데이트");
                
                // Zone별 Cell ID, Inner ID 읽기
                string cellId = GlobalDataManager.GetValue("MTP", $"CELL_ID_ZONE_{targetZone}", "");
                string innerId = GlobalDataManager.GetValue("MTP", $"INNER_ID_ZONE_{targetZone}", "");
                
                System.Diagnostics.Debug.WriteLine($"Zone {targetZone} - Cell ID: {cellId}, Inner ID: {innerId}");
                
                // 해당 Zone의 기존 데이터만 제거하고 올바른 위치에 새 데이터 삽입
                var itemsToRemove = viewModel.DataItems.Where(item => item.Zone == targetZone.ToString()).ToList();
                int insertIndex = 0;
                
                if (itemsToRemove.Count > 0)
                {
                    // 첫 번째 제거할 아이템의 인덱스 찾기
                    insertIndex = viewModel.DataItems.IndexOf(itemsToRemove[0]);
                    
                    // 해당 Zone의 모든 데이터 제거
                    foreach (var item in itemsToRemove)
                    {
                        viewModel.DataItems.Remove(item);
                    }
                }
                
                // 해당 Zone의 각 Category에 대해 구조체 데이터 생성
                for (int i = 0; i < categories.Length; i++)
                {
                    // Category를 패턴 인덱스로 변환
                    int patternIndex = GetPatternArrayIndex(categories[i]);
                    
                    // 구조체 data[wadIndex][patternIndex]에서 데이터 가져오기
                    // 실제로는 DLL 호출: data[wadIndex][patternIndex].x, .y, .l, .current, .efficiency
                    var structData = GetStructData(wadIndex, patternIndex);
                    
                    var item = new DataTableItem
                    {
                        Zone = targetZone.ToString(),
                        CellId = cellId,
                        InnerId = innerId,
                        Category = categories[i],
                        X = structData.X,
                        Y = structData.Y,
                        L = structData.L,
                        Current = structData.Current,
                        Efficiency = structData.Efficiency,
                        ErrorName = structData.ErrorName,
                        Tact = structData.Tact,
                        Judgment = structData.Judgment
                    };
                    
                    // 올바른 위치에 삽입 (Zone 순서 유지)
                    viewModel.DataItems.Insert(insertIndex + i, item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"구조체 데이터 생성 오류: {ex.Message}");
                
                // 오류 발생 시 기본값 사용
                GenerateDefaultEmptyData();
            }
        }

        private StructPatternData GetStructData(int wadIndex, int patternIndex)
        {
            // C++ 구조체 로직과 일치: data[i][j].x = i * 17 + j + 1
            // 실제로는 DLL에서 data[wadIndex][patternIndex]를 가져와야 함
            
            if (!isTestStarted)
            {
                return new StructPatternData
                {
                    X = "",
                    Y = "",
                    L = "",
                    Current = "",
                    Efficiency = "",
                    ErrorName = "",
                    Tact = "",
                    Judgment = ""
                };
            }
            
            // C++ 구조체 순서: data[i][j] = i * 17 + j + 1
            int baseValue = wadIndex * 17 + patternIndex + 1;
            
            System.Diagnostics.Debug.WriteLine($"GetStructData: wadIndex={wadIndex}, patternIndex={patternIndex}, baseValue={baseValue}");
            
            return new StructPatternData
            {
                X = baseValue.ToString(),
                Y = (baseValue + 1).ToString(),
                L = (baseValue + 2).ToString(),
                Current = (baseValue + 3).ToString(),
                Efficiency = (baseValue + 4).ToString(),
                ErrorName = "",
                Tact = "0",
                Judgment = "OK"
            };
        }

        // Zone별 테스트 상태 초기화
        private void InitializeZoneTestStates()
        {
            try
            {
                // INI 파일에서 Zone 개수 읽기
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string exeDir = System.IO.Path.GetDirectoryName(exePath);
                string iniPath = @"D:\\Project\\Recipe\\OptiX.ini";
                
                var iniManager = new IniFileManager(iniPath);
                int zoneCount = int.Parse(iniManager.ReadValue("MTP", "Zone", "2"));
                
                // Zone별 테스트 완료 상태 배열 초기화 (모두 false)
                zoneTestCompleted = new bool[zoneCount];
                for (int i = 0; i < zoneCount; i++)
                {
                    zoneTestCompleted[i] = false;
                }
                
                System.Diagnostics.Debug.WriteLine($"Zone별 테스트 상태 초기화 완료. Zone 개수: {zoneCount}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zone별 테스트 상태 초기화 오류: {ex.Message}");
                // 기본값으로 2개 Zone 설정
                zoneTestCompleted = new bool[2];
            }
        }

        // 테스트 시작 메서드
        public void StartTest()
        {
            isTestStarted = true;
            System.Diagnostics.Debug.WriteLine("테스트 시작됨");
            
            // 현재 선택된 WAD로 데이터 업데이트
            if (WadComboBox.SelectedIndex >= 0)
            {
                UpdateDataForWad(WadComboBox.SelectedIndex);
            }
        }
        
        // 테스트 중지 메서드
        public void StopTest()
        {
            isTestStarted = false;
            System.Diagnostics.Debug.WriteLine("테스트 중지됨");
            
            // 모든 측정값 클리어
            ClearMeasurementValues();
            CreateDataRows();
        }
        
        // 특정 Zone의 테스트 완료 상태 설정
        public void SetZoneTestCompleted(int zoneIndex, bool completed)
        {
            if (zoneIndex >= 0 && zoneIndex < zoneTestCompleted.Length)
            {
                zoneTestCompleted[zoneIndex] = completed;
                System.Diagnostics.Debug.WriteLine($"Zone {zoneIndex + 1} 테스트 완료 상태: {completed}");
            }
        }
        
        // 특정 Zone의 테스트 완료 상태 확인
        public bool IsZoneTestCompleted(int zoneIndex)
        {
            if (zoneIndex >= 0 && zoneIndex < zoneTestCompleted.Length)
            {
                return zoneTestCompleted[zoneIndex];
            }
            return false;
        }
        

        // 구조체 패턴 데이터를 위한 클래스
        public class StructPatternData
        {
            public string X { get; set; } = "";
            public string Y { get; set; } = "";
            public string L { get; set; } = "";
            public string Current { get; set; } = "";
            public string Efficiency { get; set; } = "";
            public string ErrorName { get; set; } = "";
            public string Tact { get; set; } = "";
            public string Judgment { get; set; } = "";
        }


        private void ClearMeasurementValues()
        {
            // WAD 변경 시 측정값만 클리어하고 구조는 유지
            if (viewModel?.DataItems == null) return;
            
            foreach (var item in viewModel.DataItems)
            {
                // 측정값만 클리어 (Zone, Cell ID, Inner ID, Category는 유지)
                item.X = "";
                item.Y = "";
                item.L = "";
                item.Current = "";
                item.Efficiency = "";
                item.ErrorName = "";
                item.Tact = "";
                item.Judgment = "";
            }
        }

        private void GenerateDefaultEmptyData()
        {
            // 오류 발생 시 사용할 기본 빈 데이터
            if (viewModel?.DataItems == null) return;
            
            viewModel.DataItems.Clear();
            
            var categories = new[] { "W", "WG", "R", "G", "B" };
            
            // Zone 1 빈 데이터 생성
            for (int i = 0; i < categories.Length; i++)
            {
                var item = new DataTableItem
                {
                    Zone = "1",
                    CellId = "",
                    InnerId = "",
                    Category = categories[i],
                    X = "",
                    Y = "",
                    L = "",
                    Current = "",
                    Efficiency = "",
                    ErrorName = "",
                    Tact = "",
                    Judgment = ""
                };
                viewModel.DataItems.Add(item);
            }
            
            // Zone 2 빈 데이터 생성
            for (int i = 0; i < categories.Length; i++)
            {
                var item = new DataTableItem
                {
                    Zone = "2",
                    CellId = "",
                    InnerId = "",
                    Category = categories[i],
                    X = "",
                    Y = "",
                    L = "",
                    Current = "",
                    Efficiency = "",
                    ErrorName = "",
                    Tact = "",
                    Judgment = ""
                };
                viewModel.DataItems.Add(item);
            }
        }

        /// <summary>
        /// OpticPage에 언어 적용
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
                        textBlock.Text = LanguageManager.GetText("OpticPage.Back");
                }

                // 데이터 테이블 제목
                var dataTableTitle = FindName("DataTableTitle") as System.Windows.Controls.TextBlock;
                if (dataTableTitle != null)
                    dataTableTitle.Text = LanguageManager.GetText("OpticPage.CharacteristicDataTable");
                
                // WAD 라벨
                var wadLabel = FindName("WadLabel") as System.Windows.Controls.TextBlock;
                if (wadLabel != null)
                    wadLabel.Text = LanguageManager.GetText("OpticPage.WAD");

                // RESET 버튼
                if (ResetButton != null)
                    ResetButton.Content = LanguageManager.GetText("OpticPage.Reset");

                // 컨트롤 패널 버튼들
                var settingButton = FindName("SettingButton") as System.Windows.Controls.Button;
                if (settingButton != null)
                    settingButton.Content = LanguageManager.GetText("OpticPage.Setting");

                var pathButton = FindName("PathButton") as System.Windows.Controls.Button;
                if (pathButton != null)
                    pathButton.Content = LanguageManager.GetText("OpticPage.Path");

                var startButton = FindName("StartButton") as System.Windows.Controls.Button;
                if (startButton != null)
                    startButton.Content = LanguageManager.GetText("OpticPage.Start");

                var stopButton = FindName("StopButton") as System.Windows.Controls.Button;
                if (stopButton != null)
                    stopButton.Content = LanguageManager.GetText("OpticPage.Stop");

                var chartButton = FindName("ChartButton") as System.Windows.Controls.Button;
                if (chartButton != null)
                    chartButton.Content = LanguageManager.GetText("OpticPage.Chart");

                var reportButton = FindName("ReportButton") as System.Windows.Controls.Button;
                if (reportButton != null)
                    reportButton.Content = LanguageManager.GetText("OpticPage.Report");

                var exitButton = FindName("ExitButton") as System.Windows.Controls.Button;
                if (exitButton != null)
                    exitButton.Content = LanguageManager.GetText("OpticPage.Exit");

                // 특성 판정 현황 제목
                var judgmentStatusTitle = FindName("JudgmentStatusTitle") as System.Windows.Controls.TextBlock;
                if (judgmentStatusTitle != null)
                    judgmentStatusTitle.Text = LanguageManager.GetText("OpticPage.CharacteristicJudgmentStatus");

                // 수량, 발생률 헤더
                var quantityHeader = FindName("QuantityHeader") as System.Windows.Controls.TextBlock;
                if (quantityHeader != null)
                    quantityHeader.Text = LanguageManager.GetText("OpticPage.Quantity");

                var occurrenceRateHeader = FindName("OccurrenceRateHeader") as System.Windows.Controls.TextBlock;
                if (occurrenceRateHeader != null)
                    occurrenceRateHeader.Text = LanguageManager.GetText("OpticPage.OccurrenceRate");

                // 컨트롤 패널 제목
                var controlPanelTitle = FindName("ControlPanelTitle") as System.Windows.Controls.TextBlock;
                if (controlPanelTitle != null)
                    controlPanelTitle.Text = LanguageManager.GetText("OpticPage.ControlPanel");

                System.Diagnostics.Debug.WriteLine($"OpticPage 언어 적용 완료: {LanguageManager.CurrentLanguage}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpticPage 언어 적용 오류: {ex.Message}");
            }
        }
    }
}
