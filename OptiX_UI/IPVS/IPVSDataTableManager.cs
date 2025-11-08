using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OptiX.Common;
using OptiX.DLL;
using OptiX.OPTIC;
using static OptiX.OPTIC.OpticDataTableManager;

namespace OptiX.IPVS
{
    /// <summary>
    /// IPVS 페이지 데이터 테이블 관리 클래스
    /// 
    /// 역할:
    /// - 데이터 테이블 생성 (Zone, Cell ID, Inner ID, Point, X, Y, L, 전류, 효율, 에러명, TACT, 판정)
    /// - WAD 콤보박스 관리
    /// - WAD 선택 시 데이터 업데이트
    /// - Zone별 IPVS_data 구조체 데이터 생성 (DLL 결과 → UI)
    /// 
    /// IPVS 특성:
    /// - output.IPVS_data[7][10] = 70개 데이터
    /// - Point 1~10 (카테고리 대신 포인트 사용)
    /// </summary>
    public class IPVSDataTableManager
    {
        private readonly Grid dataTableGrid;
        private readonly ComboBox wadComboBox;
        private readonly IPVSPageViewModel viewModel;
        private bool isDarkMode = false;
        private bool isInitializingWadComboBox = false;

        // Zone별 테스트 상태 (View에서 복사)
        private bool isTestStarted = false;
        private bool[] zoneTestCompleted;
        private bool[] zoneMeasured;

        public IPVSDataTableManager(Grid dataTableGrid, ComboBox wadComboBox, IPVSPageViewModel viewModel)
        {
            this.dataTableGrid = dataTableGrid ?? throw new ArgumentNullException(nameof(dataTableGrid));
            this.wadComboBox = wadComboBox ?? throw new ArgumentNullException(nameof(wadComboBox));
            this.viewModel = viewModel;
        }

        #region Public Methods

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
                System.Diagnostics.Debug.WriteLine("IPVSDataTableManager.UpdateTable() 호출됨");
                
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
        /// 커스텀 데이터 테이블 생성
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
        /// 헤더 행 생성
        /// </summary>
        private void CreateHeaderRow(Grid dataTableGrid)
        {
            // 헤더 행 정의
            dataTableGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(45) });

            string[] headers = { "Zone", "Cell ID", "Inner ID", "Point", "X", "Y", "L", "Current", "Efficiency", "Error Name", "Tact", "Judgment" };

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
        /// 데이터 행 생성
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

                // 각 Zone의 Point 개수만큼 행 생성
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

                    // Point 컬럼 (각 행마다 표시)
                    var pointBorder = new Border
                    {
                        Background = GetResourceBrush("DynamicSurfaceColor", Brushes.White),
                        BorderBrush = GetResourceBrush("DynamicBorderColor", Brushes.LightGray),
                        BorderThickness = new Thickness(1),
                        Child = new TextBlock
                        {
                            Text = currentItem.Point,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontWeight = FontWeights.Medium,
                            Foreground = GetResourceBrush("DynamicTextPrimaryColor", Brushes.Black)
                        }
                    };
                    Grid.SetColumn(pointBorder, 3);
                    Grid.SetRow(pointBorder, currentRow);
                    dataTableGrid.Children.Add(pointBorder);

                    // X 값
                    var xBorder = new Border
                    {
                        Background = GetResourceBrush("DynamicSurfaceColor", Brushes.White),
                        BorderBrush = GetResourceBrush("DynamicBorderColor", Brushes.LightGray),
                        BorderThickness = new Thickness(1),
                        Child = new TextBlock
                        {
                            Text = currentItem.X,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Foreground = GetResourceBrush("DynamicTextPrimaryColor", Brushes.Black)
                        }
                    };
                    Grid.SetColumn(xBorder, 4);
                    Grid.SetRow(xBorder, currentRow);
                    dataTableGrid.Children.Add(xBorder);

                    // Y 값
                    var yBorder = new Border
                    {
                        Background = GetResourceBrush("DynamicSurfaceColor", Brushes.White),
                        BorderBrush = GetResourceBrush("DynamicBorderColor", Brushes.LightGray),
                        BorderThickness = new Thickness(1),
                        Child = new TextBlock
                        {
                            Text = currentItem.Y,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Foreground = GetResourceBrush("DynamicTextPrimaryColor", Brushes.Black)
                        }
                    };
                    Grid.SetColumn(yBorder, 5);
                    Grid.SetRow(yBorder, currentRow);
                    dataTableGrid.Children.Add(yBorder);

                    // L 값
                    var lBorder = new Border
                    {
                        Background = GetResourceBrush("DynamicSurfaceColor", Brushes.White),
                        BorderBrush = GetResourceBrush("DynamicBorderColor", Brushes.LightGray),
                        BorderThickness = new Thickness(1),
                        Child = new TextBlock
                        {
                            Text = currentItem.L,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Foreground = GetResourceBrush("DynamicTextPrimaryColor", Brushes.Black)
                        }
                    };
                    Grid.SetColumn(lBorder, 6);
                    Grid.SetRow(lBorder, currentRow);
                    dataTableGrid.Children.Add(lBorder);

                    // 전류 값
                    var currentBorder = new Border
                    {
                        Background = GetResourceBrush("DynamicSurfaceColor", Brushes.White),
                        BorderBrush = GetResourceBrush("DynamicBorderColor", Brushes.LightGray),
                        BorderThickness = new Thickness(1),
                        Child = new TextBlock
                        {
                            Text = currentItem.Current,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Foreground = GetResourceBrush("DynamicTextPrimaryColor", Brushes.Black)
                        }
                    };
                    Grid.SetColumn(currentBorder, 7);
                    Grid.SetRow(currentBorder, currentRow);
                    dataTableGrid.Children.Add(currentBorder);

                    // 효율 값
                    var efficiencyBorder = new Border
                    {
                        Background = GetResourceBrush("DynamicSurfaceColor", Brushes.White),
                        BorderBrush = GetResourceBrush("DynamicBorderColor", Brushes.LightGray),
                        BorderThickness = new Thickness(1),
                        Child = new TextBlock
                        {
                            Text = currentItem.Efficiency,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Foreground = GetResourceBrush("DynamicTextPrimaryColor", Brushes.Black)
                        }
                    };
                    Grid.SetColumn(efficiencyBorder, 8);
                    Grid.SetRow(efficiencyBorder, currentRow);
                    dataTableGrid.Children.Add(efficiencyBorder);

                    // Error Name 컬럼 (첫 번째 행에서만 표시, RowSpan 적용)
                    if (i == 0)
                    {
                        var errorNameBorder = new Border
                        {
                            Background = GetResourceBrush("DynamicSurfaceColor", Brushes.White),
                            BorderBrush = GetResourceBrush("DynamicBorderColor", Brushes.LightGray),
                            BorderThickness = new Thickness(1),
                            Child = new TextBlock
                            {
                                Text = firstItem.ErrorName,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                FontWeight = FontWeights.Medium,
                                Foreground = GetResourceBrush("DynamicTextPrimaryColor", Brushes.Black)
                            }
                        };
                        Grid.SetColumn(errorNameBorder, 9);
                        Grid.SetRow(errorNameBorder, currentRow);
                        Grid.SetRowSpan(errorNameBorder, zoneItems.Count);
                        dataTableGrid.Children.Add(errorNameBorder);
                    }

                    // TACT 컬럼 (첫 번째 행에서만 표시, RowSpan 적용)
                    if (i == 0)
                    {
                        var tactBorder = new Border
                        {
                            Background = GetResourceBrush("DynamicSurfaceColor", Brushes.White),
                            BorderBrush = GetResourceBrush("DynamicBorderColor", Brushes.LightGray),
                            BorderThickness = new Thickness(1),
                            Child = new TextBlock
                            {
                                Text = firstItem.Tact,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                FontWeight = FontWeights.Medium,
                                Foreground = GetResourceBrush("DynamicTextPrimaryColor", Brushes.Black)
                            }
                        };
                        Grid.SetColumn(tactBorder, 10);
                        Grid.SetRow(tactBorder, currentRow);
                        Grid.SetRowSpan(tactBorder, zoneItems.Count);
                        dataTableGrid.Children.Add(tactBorder);
                    }

                    // Judgment 컬럼 (첫 번째 행에서만 표시, RowSpan 적용)
                    if (i == 0)
                    {
                        var judgmentBorder = new Border
                        {
                            Background = GetResourceBrush("DynamicSurfaceColor", Brushes.White),
                            BorderBrush = GetResourceBrush("DynamicBorderColor", Brushes.LightGray),
                            BorderThickness = new Thickness(1),
                            Child = new TextBlock
                            {
                                Text = firstItem.Judgment,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                FontWeight = FontWeights.SemiBold,
                                Foreground = GetResourceBrush("DynamicTextPrimaryColor", Brushes.Black)
                            }
                        };
                        Grid.SetColumn(judgmentBorder, 11);
                        Grid.SetRow(judgmentBorder, currentRow);
                        Grid.SetRowSpan(judgmentBorder, zoneItems.Count);
                        dataTableGrid.Children.Add(judgmentBorder);
                    }
                }
            }
        }

        /// <summary>
        /// WAD ComboBox 초기화
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
                string wadValues = GlobalDataManager.GetValue("IPVS", "WAD", "0,15,30,45");
                
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
                
                System.Diagnostics.Debug.WriteLine($"WAD 콤보박스 초기화 완료: {wadArray.Length}개 항목");
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
        /// WAD ComboBox 선택 변경 이벤트 핸들러
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
                        int wadIndex = IPVSHelpers.GetWadArrayIndex(selectedWadValue);

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
        public void UpdateDataForWad(int wadIndex)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"IPVS WAD 변경: 인덱스 {wadIndex}");

                // Zone 개수 가져오기
                int zoneCount = int.Parse(GlobalDataManager.GetValue("Settings", "IPVS_ZONE", "2"));

                // 저장된 데이터가 있는 모든 Zone에 대해 DataItems만 업데이트 (판정/그래프는 업데이트 안함)
                bool anyZoneUpdated = false;
                for (int zoneIndex = 0; zoneIndex < zoneCount; zoneIndex++)
                {
                    int zoneNumber = zoneIndex + 1; // 1-based
                    var storedOutput = SeqExecutionManager.GetStoredZoneResult(zoneNumber);

                    if (storedOutput.HasValue)
                    {
                        // 저장된 데이터가 있으면 POINT==1 데이터만 업데이트
                        System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} WAD {wadIndex}로 데이터 업데이트 (판정/그래프 제외)");
                        UpdatePointDataOnly(storedOutput.Value, zoneNumber, wadIndex);
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

                // 테이블 재생성 (UI 업데이트)
                if (anyZoneUpdated && dataTableGrid != null)
                {
                    System.Diagnostics.Debug.WriteLine("WAD 변경으로 테이블 재생성");
                    CreateCustomTable();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WAD 데이터 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// POINT 데이터만 업데이트 (판정/그래프 제외)
        /// </summary>
        private void UpdatePointDataOnly(Output output, int zoneNumber, int wadIndex)
        {
            try
            {
                var targetZone = zoneNumber.ToString();
                
                // IPVS_data 확인
                if (output.IPVS_data == null || output.IPVS_data.Length == 0)
                {
                    return;
                }

                // TACT 계산
                DateTime zoneSeqStartTime = SeqExecutionManager.GetZoneSeqStartTime(zoneNumber);
                DateTime zoneSeqEndTime = SeqExecutionManager.GetZoneSeqEndTime(zoneNumber);
                
                if (zoneSeqEndTime == default(DateTime) || zoneSeqEndTime < zoneSeqStartTime)
                {
                    zoneSeqEndTime = DateTime.Now;
                }
                
                double tactSeconds = (zoneSeqEndTime - zoneSeqStartTime).TotalSeconds;
                string tactValue = tactSeconds.ToString("F3");

                // POINT==1 행만 업데이트
                int dataIndex = wadIndex * 10 + 0;
                if (dataIndex < output.IPVS_data.Length)
                {
                    var pattern = output.IPVS_data[dataIndex];
                    
                    var existingItem = viewModel.DataItems.FirstOrDefault(item =>
                        item.Zone == targetZone && item.Point == "1");

                    if (existingItem != null)
                    {
                        existingItem.X = pattern.x.ToString("F2");
                        existingItem.Y = pattern.y.ToString("F2");
                        existingItem.L = pattern.L.ToString("F2");
                        existingItem.Current = pattern.cur.ToString("F3");
                        existingItem.Efficiency = pattern.eff.ToString("F2");
                        existingItem.Tact = tactValue;
                        // Cell ID, Inner ID, Judgment는 유지
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"POINT 데이터 업데이트 오류: {ex.Message}");
            }
        }
        public void GenerateDataFromStruct(int wadIndex, int zoneIndex)
        {
            // 구조체 data[wadIndex][patternIndex]에 맞는 데이터 생성
            // 실제로는 DLL에서 data[wadIndex][patternIndex]를 가져와야 함

            if (viewModel?.DataItems == null) return;

            try
            {
                // INI 파일에서 설정 읽기
                // Category 목록 읽기
                string Point = GlobalDataManager.GetValue("IPVS", "MAX_POINT", "5");
                int pointValue = int.Parse(Point);
                string[] Points = Point.Split(',').Select(c => c.Trim()).ToArray();

                // 지정된 Zone만 업데이트 (zoneIndex는 0-based)
                int targetZone = zoneIndex + 1; // 1-based로 변환
                System.Diagnostics.Debug.WriteLine($"Zone {targetZone} (인덱스: {zoneIndex}) 업데이트");

                // Zone별 Cell ID, Inner ID 읽기
                string cellId = GlobalDataManager.GetValue("IPVS", $"CELL_ID_ZONE_{targetZone}", "");
                string innerId = GlobalDataManager.GetValue("IPVS", $"INNER_ID_ZONE_{targetZone}", "");

                System.Diagnostics.Debug.WriteLine($"Zone {targetZone} - Cell ID: {cellId}, Inner ID: {innerId}");

                //25.11.08 - WAD 변경 시 기존 ErrorName, Tact, Judgment 값 보존
                // 해당 Zone의 기존 데이터에서 ErrorName, Tact, Judgment 값 저장
                var itemsToRemove = viewModel.DataItems.Where(item => item.Zone == targetZone.ToString()).ToList();
                string existingErrorName = "";
                string existingTact = "";
                string existingJudgment = "";

                if (itemsToRemove.Count > 0)
                {
                    // 기존 Zone 데이터의 ErrorName, Tact, Judgment 값 저장
                    existingErrorName = itemsToRemove[0].ErrorName ?? "";
                    existingTact = itemsToRemove[0].Tact ?? "";
                    existingJudgment = itemsToRemove[0].Judgment ?? "";
                }

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
                for (int i = 0; i < pointValue; i++)
                {
                    // Category를 패턴 인덱스로 변환
                    int patternIndex = i;

                    // 실제 저장된 DLL 결과 데이터 사용 (더미 데이터 생성하지 않음)
                    var storedOutput = SeqExecutionManager.GetStoredZoneResult(targetZone);
                    var structData = GetActualStructData(storedOutput, wadIndex, patternIndex, targetZone);

                    var item = new DataTableItem
                    {
                        Zone = targetZone.ToString(),
                        CellId = cellId,
                        InnerId = innerId,
                        Point = (i + 1).ToString(),
                        X = structData.X,
                        Y = structData.Y,
                        L = structData.L,
                        Current = structData.Current,
                        Efficiency = structData.Efficiency,
                        ErrorName = existingErrorName, // 기존 값 유지
                        Tact = existingTact, // 기존 값 유지
                        Judgment = existingJudgment // 기존 값 유지
                    };

                    // 올바른 위치에 삽입 (Zone 순서 유지)
                    viewModel.DataItems.Insert(insertIndex + i, item);
                }

                //25.11.08 - 중복 판정 로직 제거 (DllResultHandler에서 이미 판정 완료)
                // WAD 변경 시에도 판정값은 동일하므로 기존 판정값 유지 (재계산 불필요)
                // Zone 전체 판정은 DllResultHandler.ProcessIPVSResult()에서 한 번만 수행됨
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"구조체 데이터 생성 오류: {ex.Message}");

                // 오류 발생 시 기본값 사용
                GenerateDefaultEmptyData();
            }
        }
        private StructPatternData GetActualStructData(Output? storedOutput, int wadIndex, int patternIndex, int zoneNumber)
        {
            // 실제 저장된 DLL 결과 데이터 사용 (더미 데이터 생성하지 않음)
            if (storedOutput.HasValue && storedOutput.Value.data != null)
            {
                // data 배열에서 올바른 인덱스로 패턴 데이터 가져오기
                // data[wadIndex * 10 + patternIndex] 형태로 저장됨
                int dataIndex = wadIndex * 10 + patternIndex;

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
        public void GenerateDefaultEmptyData()
        {
            // 오류 발생 시 사용할 기본 빈 데이터
            if (viewModel?.DataItems == null) return;

            viewModel.DataItems.Clear();

            var categories = new[] { "W", "WG", "R", "G", "B" };

            int zoneCount = int.Parse(GlobalDataManager.GetValue("Settings", "IPVS_ZONE", "2"));
            int POINTCount = int.Parse(GlobalDataManager.GetValue("IPVS", "MAX_POINT", "5"));

            // Zone별 빈 데이터 생성
            for (int zoneNum = 1; zoneNum <= zoneCount; zoneNum++)
            {
                for (int i = 0; i < POINTCount; i++)
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
        /// 데이터 테이블 초기화
        /// </summary>
        public void ResetDataTable()
        {
            try
            {
                isInitializingWadComboBox = true;
                
                // WAD 콤보박스를 0도로 초기화
                if (wadComboBox != null)
                {
                    wadComboBox.SelectedIndex = 0;
                }

                // ViewModel 데이터 클리어
                if (viewModel?.DataItems != null)
                {
                    viewModel.DataItems.Clear();
                }

                // DataItems 재초기화 (Cell ID/Inner ID 포함)
                int zoneCount = int.Parse(GlobalDataManager.GetValue("Settings", "IPVS_ZONE", "2"));
                int maxPoint = int.Parse(GlobalDataManager.GetValue("IPVS", "MAX_POINT", "5"));

                for (int zone = 1; zone <= zoneCount; zone++)
                {
                    // Cell ID와 Inner ID는 테스트 후에만 표시 (OPTIC과 동일)
                    
                    for (int point = 1; point <= maxPoint; point++)
                    {
                        viewModel.DataItems.Add(new DataTableItem
                        {
                            Zone = zone.ToString(),
                            CellId = "",          // 테스트 전에는 비어있음
                            InnerId = "",         // 테스트 전에는 비어있음
                            Point = point.ToString(),
                            X = "0.0",
                            Y = "0.0",
                            L = "0.0",
                            Current = "0.0",
                            Efficiency = "0.0",
                            ErrorName = "",
                            Tact = "0",
                            Judgment = "WAIT"
                        });
                    }
                }

                // 테이블 재생성
                CreateCustomTable();

                isInitializingWadComboBox = false;
                
                System.Diagnostics.Debug.WriteLine("데이터 테이블 초기화 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"데이터 테이블 초기화 오류: {ex.Message}");
                isInitializingWadComboBox = false;
            }
        }

        #endregion

        #region DLL 결과 업데이트 메서드

        //25.11.08 - IPVS도 OPTIC과 동일하게 패턴별 측정값만 업데이트하는 메서드 추가
        /// <summary>
        /// 포인트별 측정값만 업데이트 (X, Y, L, Current, Efficiency)
        /// </summary>
        public void UpdatePatternData(
            string zone,
            string point,
            string x,
            string y,
            string l,
            string current,
            string efficiency)
        {
            var existingItem = viewModel.DataItems.FirstOrDefault(item =>
                item.Zone == zone && item.Point == point);

            if (existingItem != null)
            {
                existingItem.X = x;
                existingItem.Y = y;
                existingItem.L = l;
                existingItem.Current = current;
                existingItem.Efficiency = efficiency;
            }
        }

        //25.11.08 - Zone 전체 Cell 정보 업데이트 메서드 추가 (cellId, innerId는 모든 포인트에 동일)
        /// <summary>
        /// Zone 전체 Cell 정보 업데이트 (CellId, InnerId)
        /// </summary>
        public void UpdateZoneCellInfo(string zone, string cellId, string innerId)
        {
            var zoneItems = viewModel.DataItems.Where(item => item.Zone == zone).ToList();
            foreach (var item in zoneItems)
            {
                item.CellId = cellId;
                item.InnerId = innerId;
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
        
        //25.11.08 - ZoneTestResult 구조체를 사용하도록 오버로드 메서드 추가
        /// <summary>
        /// Zone 전체 FullTest 결과 업데이트 (ZoneTestResult 구조체 사용)
        /// </summary>
        public void UpdateZoneFullTestResult(string zone, ZoneTestResult result)
        {
            var zoneItems = viewModel.DataItems.Where(item => item.Zone == zone).ToList();
            foreach (var item in zoneItems)
            {
                item.ErrorName = result.ErrorName;
                item.Tact = result.Tact;
                item.Judgment = result.Judgment;
                
                //25.11.08 - 향후 세부 판정 필드 추가 시 여기에 업데이트 로직 추가
                // item.ColorJudgment = result.ColorJudgment;
                // item.LuminanceJudgment = result.LuminanceJudgment;
                // 등등...
            }
        }

        /// <summary>
        /// Zone 전체 FullTest 결과 업데이트 (개별 파라미터 - 하위 호환성 유지)
        /// </summary>
        public void UpdateZoneFullTestResult(string zone, string errorName, string tact, string judgment)
        {
            // ZoneTestResult 구조체로 변환하여 호출
            var result = ZoneTestResult.Create(errorName, tact, judgment);
            UpdateZoneFullTestResult(zone, result);
        }

        #endregion
    }
}
