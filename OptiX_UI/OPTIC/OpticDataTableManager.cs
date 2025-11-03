using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OptiX.Common;
using OptiX.DLL;

namespace OptiX.OPTIC
{
    /// <summary>
    /// OPTIC 페이지 데이터 테이블 관리 클래스
    /// 
    /// 역할:
    /// - 데이터 테이블 생성 (헤더, 데이터 행)
    /// - WAD 콤보박스 관리 (0도, 30도, 45도 등 선택)
    /// - WAD 선택 시 데이터 업데이트
    /// - Zone별 구조체 데이터 생성 (DLL 결과 → UI)
    /// - 측정값 초기화 및 기본값 생성
    /// - 다크모드 전환 시 색상 업데이트
    /// 
    /// 사용하는 UI 요소:
    /// - DataTableGrid (Grid) - 메인 데이터 테이블
    /// - WADComboBox (ComboBox) - WAD 선택 콤보박스
    /// 
    /// 의존성:
    /// - OpticPageViewModel (데이터 소스)
    /// - DllManager (구조체 데이터 읽기)
    /// - GlobalDataManager (Zone 정보 읽기)
    /// </summary>
    public class OpticDataTableManager
    {
        private readonly Grid dataTableGrid;
        private readonly ComboBox wadComboBox;
        private readonly OpticPageViewModel viewModel;
        private bool isDarkMode = false;
        private bool isInitializingWadComboBox = false;

        // Zone별 테스트 상태 (View에서 복사)
        private bool isTestStarted = false;
        private bool[] zoneTestCompleted;
        private bool[] zoneMeasured;

        public OpticDataTableManager(Grid dataTableGrid, ComboBox wadComboBox, OpticPageViewModel viewModel)
        {
            this.dataTableGrid = dataTableGrid ?? throw new ArgumentNullException(nameof(dataTableGrid));
            this.wadComboBox = wadComboBox ?? throw new ArgumentNullException(nameof(wadComboBox));
            this.viewModel = viewModel;
        }

        /// <summary>
        /// 다크모드 상태 설정 및 테이블 갱신
        /// </summary>
        public void SetDarkMode(bool darkMode)
        {
            this.isDarkMode = darkMode;
            
            // 다크모드 변경 시 테이블 즉시 재생성
            try
            {
                CreateCustomTable();
                System.Diagnostics.Debug.WriteLine($"다크모드 변경으로 데이터 테이블 갱신: {darkMode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"다크모드 테이블 갱신 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Zone 테스트 상태 설정 (외부에서 호출)
        /// </summary>
        public void SetTestStates(bool testStarted, bool[] completed, bool[] measured)
        {
            this.isTestStarted = testStarted;
            this.zoneTestCompleted = completed;
            this.zoneMeasured = measured;
        }

        /// <summary>
        /// 테이블 업데이트 (다크모드 전환 시)
        /// </summary>
        public void UpdateTable()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("OpticDataTableManager.UpdateTable() 호출됨");
                
                // 기존 테이블 재생성
                CreateCustomTable();
                
                System.Diagnostics.Debug.WriteLine("테이블 업데이트 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"테이블 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 리소스에서 SolidColorBrush를 안전하게 가져옴
        /// </summary>
        private SolidColorBrush GetResourceBrush(string resourceKey, Brush fallback)
        {
            try
            {
                // 1. DataTableGrid(Page 레벨)에서 찾기 시도
                if (dataTableGrid?.TryFindResource(resourceKey) is SolidColorBrush brush)
                    return brush;
                
                // 2. Application에서 찾기 시도
                if (Application.Current.TryFindResource(resourceKey) is SolidColorBrush appBrush)
                    return appBrush;
                
                System.Diagnostics.Debug.WriteLine($"⚠️ 리소스 '{resourceKey}' 못 찾음 - fallback 사용");
                return new SolidColorBrush(((SolidColorBrush)fallback).Color);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 리소스 '{resourceKey}' 로드 오류: {ex.Message}");
                return new SolidColorBrush(((SolidColorBrush)fallback).Color);
            }
        }

        /// <summary>
        /// 커스텀 데이터 테이블 생성 (OpticPage.xaml.cs에서 복사)
        /// </summary>
        public void CreateCustomTable()
        {
            try
            {
                if (dataTableGrid == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ DataTableGrid를 찾을 수 없습니다!");
                    return;
                }

                // 기존 내용 클리어
                dataTableGrid.RowDefinitions.Clear();
                dataTableGrid.Children.Clear();

                // 헤더 행 추가
                CreateHeaderRow(dataTableGrid);

                // 데이터 행들 추가 (데이터가 있을 때만)
                if (viewModel?.DataItems != null && viewModel.DataItems.Count > 0)
                {
                    CreateDataRows(dataTableGrid);
                    System.Diagnostics.Debug.WriteLine($"✅ 데이터가 있어서 테이블 행 생성함 ({viewModel.DataItems.Count}개)");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ 데이터가 없어서 헤더만 생성함");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 커스텀 테이블 생성 오류: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 헤더 행 생성 (OpticPage.xaml.cs에서 복사)
        /// </summary>
        private void CreateHeaderRow(Grid dataTableGrid)
        {
            // 헤더 행 정의
            dataTableGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(45) });

            string[] headers = { "Zone", "Cell ID", "Inner ID", "Category", "X", "Y", "L", "Current", "Efficiency", "Error Name", "Tact", "Judgment" };
            
            // 리소스 안전하게 가져오기
            SolidColorBrush primaryColor = GetResourceBrush("PrimaryColor", Brushes.DarkBlue);
            
            for (int i = 0; i < headers.Length; i++)
            {
                var headerBorder = new Border
                {
                    Background = primaryColor,
                    BorderBrush = primaryColor,
                    BorderThickness = new Thickness(1),
                    Child = new TextBlock
                    {
                        Text = headers[i],
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Colors.White),
                        FontSize = 13
                    }
                };

                Grid.SetColumn(headerBorder, i);
                Grid.SetRow(headerBorder, 0);
                dataTableGrid.Children.Add(headerBorder);
            }
        }

        /// <summary>
        /// 데이터 행 생성 (OpticPage.xaml.cs에서 복사)
        /// </summary>
        private void CreateDataRows(Grid dataTableGrid)
        {
            if (viewModel?.DataItems == null || viewModel.DataItems.Count == 0) 
            {
                System.Diagnostics.Debug.WriteLine("CreateDataRows: DataItems가 비어있음 - 테이블 생성하지 않음");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"CreateDataRows: 전체 아이템 개수 = {viewModel.DataItems.Count}");

            // 기존 데이터 행들만 제거 (헤더는 유지)
            var existingDataChildren = dataTableGrid.Children.Cast<UIElement>()
                .Where(child => Grid.GetRow(child) > 0).ToList();

            foreach (var child in existingDataChildren)
            {
                dataTableGrid.Children.Remove(child);
            }

            // 기존 행 정의들 제거 (헤더 행은 유지)
            while (dataTableGrid.RowDefinitions.Count > 1)
            {
                dataTableGrid.RowDefinitions.RemoveAt(dataTableGrid.RowDefinitions.Count - 1);
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
                    dataTableGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
                    int currentRow = dataTableGrid.RowDefinitions.Count - 1;
                    var currentItem = zoneItems[i];

                    // Zone 컬럼 (첫 번째 행에서만 표시, RowSpan 적용)
                    if (i == 0)
                    {
                        var zoneBorder = new Border
                        {
                            Background = GetResourceBrush("DynamicSurfaceColor", Brushes.White),
                            BorderBrush = GetResourceBrush("DynamicBorderColor", Brushes.LightGray),
                            BorderThickness = new Thickness(1),
                            Child = new TextBlock
                            {
                                Text = firstItem.Zone,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                FontWeight = FontWeights.SemiBold,
                                Foreground = GetResourceBrush("DynamicTextPrimaryColor", Brushes.Black)
                            }
                        };
                        Grid.SetColumn(zoneBorder, 0);
                        Grid.SetRow(zoneBorder, currentRow);
                        Grid.SetRowSpan(zoneBorder, zoneItems.Count);
                        dataTableGrid.Children.Add(zoneBorder);
                    }

                    // Cell ID 컬럼 (첫 번째 행에서만 표시, RowSpan 적용)
                    if (i == 0)
                    {
                        var cellIdBorder = new Border
                        {
                            Background = GetResourceBrush("DynamicSurfaceColor", Brushes.White),
                            BorderBrush = GetResourceBrush("DynamicBorderColor", Brushes.LightGray),
                            BorderThickness = new Thickness(1),
                            Child = new TextBlock
                            {
                                Text = firstItem.CellId,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                FontWeight = FontWeights.Medium,
                                Foreground = GetResourceBrush("DynamicTextPrimaryColor", Brushes.Black)
                            }
                        };
                        Grid.SetColumn(cellIdBorder, 1);
                        Grid.SetRow(cellIdBorder, currentRow);
                        Grid.SetRowSpan(cellIdBorder, zoneItems.Count);
                        dataTableGrid.Children.Add(cellIdBorder);
                    }

                    // Inner ID 컬럼 (첫 번째 행에서만 표시, RowSpan 적용)
                    if (i == 0)
                    {
                        var innerIdBorder = new Border
                        {
                            Background = GetResourceBrush("DynamicSurfaceColor", Brushes.White),
                            BorderBrush = GetResourceBrush("DynamicBorderColor", Brushes.LightGray),
                            BorderThickness = new Thickness(1),
                            Child = new TextBlock
                            {
                                Text = firstItem.InnerId,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                FontWeight = FontWeights.Medium,
                                Foreground = GetResourceBrush("DynamicTextPrimaryColor", Brushes.Black)
                            }
                        };
                        Grid.SetColumn(innerIdBorder, 2);
                        Grid.SetRow(innerIdBorder, currentRow);
                        Grid.SetRowSpan(innerIdBorder, zoneItems.Count);
                        dataTableGrid.Children.Add(innerIdBorder);
                    }

                    // Category 컬럼 (각 행마다 개별 표시)
                    var categoryBorder = new Border
            {
                Background = GetResourceBrush("DynamicSurfaceColor", Brushes.White),
                BorderBrush = GetResourceBrush("DynamicBorderColor", Brushes.LightGray),
                        BorderThickness = new Thickness(1),
                        Child = new TextBlock
                        {
                            Text = currentItem.Category,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontWeight = FontWeights.Medium,
                            Foreground = GetResourceBrush("DynamicTextPrimaryColor", Brushes.Black)
                        }
                    };
                    Grid.SetColumn(categoryBorder, 3);
                    Grid.SetRow(categoryBorder, currentRow);
                    dataTableGrid.Children.Add(categoryBorder);

                    // X, Y, L, Current, Efficiency 컬럼들 (각 행마다 개별 표시)
                    string[] dataValues = { currentItem.X, currentItem.Y, currentItem.L, currentItem.Current, currentItem.Efficiency };
                    int[] dataColumns = { 4, 5, 6, 7, 8 };

                    for (int j = 0; j < dataValues.Length; j++)
                    {
                        var dataBorder = new Border
            {
                Background = GetResourceBrush("DynamicSurfaceColor", Brushes.White),
                BorderBrush = GetResourceBrush("DynamicBorderColor", Brushes.LightGray),
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                                Text = dataValues[j],
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                                FontWeight = FontWeights.Normal,
                    Foreground = GetResourceBrush("DynamicTextPrimaryColor", Brushes.Black)
                }
            };
                        Grid.SetColumn(dataBorder, dataColumns[j]);
                        Grid.SetRow(dataBorder, currentRow);
                        dataTableGrid.Children.Add(dataBorder);
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
                                Background = GetResourceBrush("DynamicSurfaceColor", Brushes.White),
                                BorderBrush = GetResourceBrush("DynamicBorderColor", Brushes.LightGray),
                                BorderThickness = new Thickness(1),
                                Child = new TextBlock
                                {
                                    Text = mergedValues[k],
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    FontWeight = FontWeights.Medium,
                                    Foreground = GetResourceBrush("DynamicTextPrimaryColor", Brushes.Black)
                                }
                            };
                            Grid.SetColumn(mergedBorder, mergedColumns[k]);
                            Grid.SetRow(mergedBorder, currentRow);
                            Grid.SetRowSpan(mergedBorder, zoneItems.Count);
                            dataTableGrid.Children.Add(mergedBorder);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// WAD 콤보박스 초기화 (OpticPage.xaml.cs에서 복사)
        /// </summary>
        public void InitializeWadComboBox()
        {
            try
            {
                // WAD 콤보박스 초기화 시작 플래그 설정
                isInitializingWadComboBox = true;

                // wadComboBox는 이미 필드로 가지고 있음
                if (wadComboBox == null) return;

                // WAD 콤보박스에 아이템 추가
                wadComboBox.Items.Clear();
                
                // INI 파일에서 WAD 값 읽기
                string wadValues = GlobalDataManager.GetValue("MTP", "WAD", "0,15,30,45");
                
                // 쉼표로 분리하여 배열로 변환
                string[] wadArray = wadValues.Split(',');
                
                // 각 WAD 값에 대해 아이템 추가
                foreach (string wadValue in wadArray)
                {
                    string trimmedValue = wadValue.Trim();
                    if (!string.IsNullOrEmpty(trimmedValue))
                    {
                        wadComboBox.Items.Add(trimmedValue);
                    }
                }
                
                // 기본값 설정 (이때 SelectionChanged 이벤트가 발생하지만 플래그로 차단됨)
                wadComboBox.SelectedIndex = 0;

                // WAD 콤보박스 초기화 완료 플래그 해제
                isInitializingWadComboBox = false;
                
                System.Diagnostics.Debug.WriteLine("WAD 콤보박스 초기화 완료 - 더미 데이터 생성하지 않음");
            }
            catch (Exception ex)
            {
                // 오류 발생 시에도 플래그 해제
                isInitializingWadComboBox = false;
                System.Diagnostics.Debug.WriteLine($"WAD 콤보박스 초기화 오류: {ex.Message}");
                
                // 오류 발생 시 기본값 사용
                // wadComboBox는 이미 필드로 가지고 있음
                if (wadComboBox != null)
                {
                    wadComboBox.Items.Clear();
                    string[] defaultWadArray = { "0", "15", "30", "45" };
                    foreach (string wadValue in defaultWadArray)
                    {
                        wadComboBox.Items.Add(wadValue);
                    }
                    wadComboBox.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// WAD 콤보박스 선택 변경 이벤트 (OpticPage.xaml.cs에서 복사)
        /// </summary>
        public void OnWadComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // WAD 콤보박스 초기화 중이면 더미 데이터 생성하지 않음
                if (isInitializingWadComboBox)
                {
                    System.Diagnostics.Debug.WriteLine("WAD 콤보박스 초기화 중이므로 더미 데이터 생성하지 않음");
                    return;
                }

                var comboBox = sender as ComboBox;
                if (comboBox != null && comboBox.SelectedItem != null)
                {
                    string selectedWadValue = comboBox.SelectedItem.ToString();
                    
                    if (!string.IsNullOrEmpty(selectedWadValue))
                    {
                        // WAD 값을 배열 인덱스로 변환
                        int wadIndex = OpticHelpers.GetWadArrayIndex(selectedWadValue);
                        
                        System.Diagnostics.Debug.WriteLine($"WAD 값 '{selectedWadValue}'이 선택되었습니다. 배열 인덱스: {wadIndex}");
                        
                        // ViewModel의 SelectedWadIndex 업데이트
                        viewModel.SelectedWadIndex = wadIndex;
                        
                        // 선택된 WAD에 해당하는 데이터로 UI 업데이트
                        UpdateDataForWad(wadIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WAD 콤보박스 선택 변경 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 특정 WAD에 대한 데이터 업데이트 (OpticPage.xaml.cs에서 복사)
        /// </summary>
        public void UpdateDataForWad(int wadIndex)
        {
            try
            {
                // WadAngle enum으로 변환
                WadAngle angle = (WadAngle)wadIndex;
                
                System.Diagnostics.Debug.WriteLine($"WAD 각도 {angle} (인덱스: {wadIndex})에 해당하는 데이터로 UI 업데이트");
                System.Diagnostics.Debug.WriteLine($"현재 상태 - isTestStarted: {isTestStarted}, zoneTestCompleted: [{string.Join(", ", zoneTestCompleted ?? new bool[0])}], zoneMeasured: [{string.Join(", ", zoneMeasured ?? new bool[0])}]");
                
                // Zone 개수 가져오기
                int zoneCount = int.Parse(GlobalDataManager.GetValue("Settings", "MTP_ZONE", "2"));
                
                // 저장된 데이터가 있는 모든 Zone 업데이트
                bool anyZoneUpdated = false;
                for (int zoneIndex = 0; zoneIndex < zoneCount; zoneIndex++)
                {
                    int zoneNumber = zoneIndex + 1; // 1-based
                    var storedOutput = SeqExecutionManager.GetStoredZoneResult(zoneNumber);
                    
                    if (storedOutput.HasValue)
                    {
                        // 저장된 데이터가 있으면 테스트 상태와 관계없이 표시
                        System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber}에 저장된 DLL 결과 데이터가 있음. WAD {wadIndex}로 업데이트");
                        GenerateDataFromStruct(wadIndex, zoneIndex);
                        anyZoneUpdated = true;
                    }
                }
                
                // 저장된 데이터가 없는 경우 기존 로직 사용
                if (!anyZoneUpdated)
                {
                    int currentZone = viewModel.CurrentZone;
                    
                    if (isTestStarted && zoneTestCompleted != null && currentZone >= 0 && currentZone < zoneTestCompleted.Length && !zoneTestCompleted[currentZone])
                    {
                        // 테스트 진행 중: 실제 측정이 발생한 존만 업데이트
                        if (zoneMeasured != null && currentZone >= 0 && currentZone < zoneMeasured.Length && zoneMeasured[currentZone])
                        {
                            System.Diagnostics.Debug.WriteLine($"테스트 진행 중. Zone {currentZone + 1}에 측정 데이터가 있어 업데이트");
                            GenerateDataFromStruct(wadIndex, currentZone);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"테스트 진행 중. 아직 Zone {currentZone + 1}에 측정 데이터 없음 → 스킵");
                        }
                    }
                    else if (zoneTestCompleted != null && zoneTestCompleted.Any(completed => completed))
                    {
                        // 테스트 완료된 존들 업데이트 (측정 여부와 관계없이)
                        for (int zone = 0; zone < zoneTestCompleted.Length; zone++)
                        {
                            if (zoneTestCompleted[zone])
                            {
                                System.Diagnostics.Debug.WriteLine($"Zone {zone + 1}이 테스트 완료됨. WAD {wadIndex}로 데이터 업데이트 (측정 여부 무관)");
                                GenerateDataFromStruct(wadIndex, zone);
                            }
                        }
                    }
                    else
                    {
                        // 테스트가 시작되지 않았고 저장된 데이터도 없으면 빈 데이터로 유지
                        System.Diagnostics.Debug.WriteLine($"저장된 데이터가 없고 테스트도 진행되지 않음 → 빈 데이터로 유지");
                        ClearMeasurementValues();
                    }
                }
                
                // 테이블 다시 그리기
                System.Diagnostics.Debug.WriteLine($"CreateDataRows 호출 전 - DataItems 개수: {viewModel?.DataItems?.Count ?? 0}");
                if (dataTableGrid != null)
                {
                    CreateDataRows(dataTableGrid);
                }
                System.Diagnostics.Debug.WriteLine($"CreateDataRows 호출 후 - DataItems 개수: {viewModel?.DataItems?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WAD 데이터 업데이트 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 구조체 데이터로부터 UI 데이터 생성 (OpticPage.xaml.cs에서 복사)
        /// </summary>
        public void GenerateDataFromStruct(int wadIndex, int zoneIndex)
        {
            // 구조체 data[wadIndex][patternIndex]에 맞는 데이터 생성
            // 실제로는 DLL에서 data[wadIndex][patternIndex]를 가져와야 함
            
            if (viewModel?.DataItems == null) return;
            
            try
            {
                // INI 파일에서 설정 읽기
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
                    int patternIndex = OpticHelpers.GetPatternArrayIndex(categories[i]);
                    
                    // 실제 저장된 DLL 결과 데이터 사용 (더미 데이터 생성하지 않음)
                    var storedOutput = SeqExecutionManager.GetStoredZoneResult(targetZone);
                    var structData = GetActualStructData(storedOutput, wadIndex, patternIndex, targetZone);
                    
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
                
                // Zone 전체 판정을 모든 아이템에 적용 (일관성 유지)
                var zoneItems = viewModel.DataItems.Where(item => item.Zone == targetZone.ToString()).ToList();
                if (zoneItems.Count > 0)
                {
                    // Zone 전체 판정 계산
                    var storedOutput = SeqExecutionManager.GetStoredZoneResult(targetZone);
                    if (storedOutput.HasValue)
                    {
                        int[,] resultArray = new int[7, 17];
                        for (int wad = 0; wad < 7; wad++)
                        {
                            for (int pattern = 0; pattern < 17; pattern++)
                            {
                                int index = wad * 17 + pattern;
                                if (index < storedOutput.Value.data.Length)
                                {
                                    resultArray[wad, pattern] = storedOutput.Value.data[index].result;
                                }
                            }
                        }
                        string zoneJudgment = OpticJudgment.Instance.JudgeZoneFromResults(resultArray);
                        
                        // 모든 아이템에 Zone 전체 판정 적용
                        foreach (var item in zoneItems)
                        {
                            item.Judgment = zoneJudgment;
                        }
                        System.Diagnostics.Debug.WriteLine($"Zone {targetZone} WAD 변경 시 전체 판정 적용: {zoneJudgment}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"구조체 데이터 생성 오류: {ex.Message}");
                
                // 오류 발생 시 기본값 사용
                GenerateDefaultEmptyData();
            }
        }

        /// <summary>
        /// 실제 구조체 데이터 가져오기 (OpticPage.xaml.cs에서 복사)
        /// </summary>
        private StructPatternData GetActualStructData(Output? storedOutput, int wadIndex, int patternIndex, int zoneNumber)
        {
            // 실제 저장된 DLL 결과 데이터 사용 (더미 데이터 생성하지 않음)
            if (storedOutput.HasValue && storedOutput.Value.data != null)
            {
                // data 배열에서 올바른 인덱스로 패턴 데이터 가져오기
                // data[wadIndex * 17 + patternIndex] 형태로 저장됨
                int dataIndex = wadIndex * 17 + patternIndex;
                
                if (dataIndex < storedOutput.Value.data.Length)
                {
                    var pattern = storedOutput.Value.data[dataIndex];
                    
                    // Zone별로 동일한 TACT 계산 (wadIndex와 관계없이 Zone 기준)
                    DateTime startTime = SeqExecutionManager.GetZoneSeqStartTime(zoneNumber);
                    DateTime endTime = SeqExecutionManager.GetZoneSeqEndTime(zoneNumber);
                    
                    // Race Condition 방지: 종료 시간이 아직 설정되지 않았으면 현재 시간 사용
                    if (endTime == default(DateTime) || endTime < startTime)
                    {
                        endTime = DateTime.Now;
                    }
                    
                    double tactSeconds = (endTime - startTime).TotalSeconds;
                    
                    return new StructPatternData
                    {
                        X = pattern.x.ToString("F2"),
                        Y = pattern.y.ToString("F2"),
                        L = pattern.L.ToString("F2"),
                        Current = pattern.cur.ToString("F3"),
                        Efficiency = pattern.eff.ToString("F2"),
                        ErrorName = "", // Zone 전체 테스트 완료 시 업데이트됨
                        Tact = "", // Zone 전체 테스트 완료 시 업데이트됨
                        Judgment = "" // Zone 전체 테스트 완료 시 업데이트됨
                    };
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"데이터 인덱스 {dataIndex}가 범위를 벗어남 (총 길이: {storedOutput.Value.data.Length})");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"저장된 출력 데이터가 없음 또는 data 배열이 null");
            }
            
            // 저장된 데이터가 없으면 빈 데이터 반환 (더미 데이터 생성하지 않음)
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

        /// <summary>
        /// 측정값 초기화
        /// </summary>
        public void ClearMeasurementValues()
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

        /// <summary>
        /// 기본 빈 데이터 생성
        /// </summary>
        public void GenerateDefaultEmptyData()
        {
            // 오류 발생 시 사용할 기본 빈 데이터
            if (viewModel?.DataItems == null) return;
            
                viewModel.DataItems.Clear();

            var categories = new[] { "W", "WG", "R", "G", "B" };
            
            int zoneCount = int.Parse(GlobalDataManager.GetValue("Settings", "MTP_ZONE", "2"));
            
            // Zone별 빈 데이터 생성
            for (int zoneNum = 1; zoneNum <= zoneCount; zoneNum++)
            {
                for (int i = 0; i < categories.Length; i++)
                {
                    var item = new DataTableItem
                    {
                        Zone = zoneNum.ToString(),
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
        }

        /// <summary>
        /// 구조체 패턴 데이터를 위한 클래스
        /// </summary>
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

        /// <summary>
        /// 데이터 테이블 초기화 (RESET 버튼 클릭 시)
        /// </summary>
        public void ResetDataTable()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("데이터 테이블 초기화 시작");
                
                // DataItems의 모든 데이터를 빈 값으로 초기화
                foreach (var item in viewModel.DataItems)
                {
                    item.CellId = "";
                    item.InnerId = "";
                    item.X = "";
                    item.Y = "";
                    item.L = "";
                    item.Current = "";
                    item.Efficiency = "";
                    item.ErrorName = "";
                    item.Tact = "";
                    item.Judgment = "";
                }
                
                // WAD 콤보박스를 첫 번째 값으로 리셋 (SelectionChanged 이벤트 차단)
                if (wadComboBox != null && wadComboBox.Items.Count > 0)
                {
                    isInitializingWadComboBox = true; // 이벤트 차단 플래그 설정
                    try
                    {
                        wadComboBox.SelectedIndex = 0;
                        viewModel.SelectedWadIndex = 0;
                    }
                    finally
                    {
                        isInitializingWadComboBox = false; // 플래그 해제
                    }
                }
                
                // 테이블 UI 다시 그리기
                CreateCustomTable();
                
                System.Diagnostics.Debug.WriteLine("데이터 테이블 초기화 완료 - 데이터는 빈 상태 유지");
            }
            catch (Exception ex)
            {
                isInitializingWadComboBox = false; // 예외 발생 시에도 플래그 해제
                System.Diagnostics.Debug.WriteLine($"데이터 테이블 초기화 오류: {ex.Message}");
            }
        }

        // 불필요한 메서드 제거됨 - 원본 로직은 ViewModel과 OpticSeqExecutor에서 처리

        #region DLL 결과 업데이트 메서드

        /// <summary>
        /// 개별 데이터 아이템 업데이트 (DllResultHandler에서 호출)
        /// </summary>
        public void UpdateDataItem(
            string zone,
            string category,
            string x,
            string y,
            string l,
            string current,
            string efficiency,
            string cellId,
            string innerId,
            string tact,
            string judgment)
        {
            var existingItem = viewModel.DataItems.FirstOrDefault(item =>
                item.Zone == zone && item.Category == category);

            if (existingItem != null)
            {
                existingItem.X = x;
                existingItem.Y = y;
                existingItem.L = l;
                existingItem.Current = current;
                existingItem.Efficiency = efficiency;
                existingItem.CellId = cellId;
                existingItem.InnerId = innerId;
                existingItem.ErrorName = "";
                existingItem.Tact = tact;
                existingItem.Judgment = judgment;
            }
        }

        /// <summary>
        /// Zone 전체 판정 업데이트 (DllResultHandler에서 호출)
        /// </summary>
        public void UpdateZoneJudgment(string zone, string judgment)
        {
            var zoneItems = viewModel.DataItems.Where(item => item.Zone == zone).ToList();
            foreach (var item in zoneItems)
            {
                item.Judgment = judgment;
            }
        }
        
        /// <summary>
        /// Zone 전체 FullTest 결과 업데이트 (ErrorName, Tact, Judgment)
        /// </summary>
        public void UpdateZoneFullTestResult(string zone, string errorName, string tact, string judgment)
        {
            var zoneItems = viewModel.DataItems.Where(item => item.Zone == zone).ToList();
            foreach (var item in zoneItems)
            {
                item.ErrorName = errorName;
                item.Tact = tact;
                item.Judgment = judgment;
            }
        }

        #endregion
    }
}

