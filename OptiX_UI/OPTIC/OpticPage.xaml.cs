using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Windows.Shapes;
using OptiX.Main;
using OptiX.Common;
using OptiX.OPTIC;

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
        // ViewModel
        private OpticPageViewModel viewModel;
        
        // Manager 인스턴스들 (리팩토링)
        private OpticDataTableManager dataTableManager;
        private GraphManager graphManager;
        private MonitorManager monitorManager;
        private JudgmentStatusManager judgmentStatusManager;
        private ZoneButtonManager zoneButtonManager;
        private OpticSeqExecutor seqExecutor;
        
        // 상태 변수들
        private bool[] zoneTestCompleted; // Zone별 테스트 완료 상태 배열
        private bool[] zoneMeasured; // Zone별 실제 측정 데이터 획득 여부
        private bool isDarkMode = false; // 다크모드 상태
        
        public OpticPage()
        {
            InitializeComponent();
            
            // ViewModel 초기화 (순환 참조 제거 - this 전달 안 함)
            viewModel = new OpticPageViewModel();
            DataContext = viewModel;
            
            // ViewModel 이벤트 구독
            SubscribeToViewModelEvents();
            
            // Manager 인스턴스 생성 및 초기화
            InitializeManagers();
            
            // Zone별 테스트 완료 상태 배열 초기화
            zoneButtonManager.InitializeZoneTestStates();
            zoneTestCompleted = zoneButtonManager.GetZoneTestCompletedArray();
            zoneMeasured = zoneButtonManager.GetZoneMeasuredArray();
            
            // null 체크 및 재초기화
            if (zoneTestCompleted == null || zoneMeasured == null)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Zone 배열 초기화 실패 - 기본값으로 재초기화");
                int zoneCount = int.Parse(GlobalDataManager.GetValue("Settings", "MTP_ZONE", "2"));
                zoneTestCompleted = new bool[zoneCount];
                zoneMeasured = new bool[zoneCount];
            }

            // DataItems 변경 감지 (원래대로)
            viewModel.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(viewModel.DataItems))
                {
                    // DataItems가 변경되었을 때만 테이블 다시 그리기
                    dataTableManager.CreateCustomTable();
                }
            };

            // 테이블 생성 및 Zone 버튼 생성
            Loaded += (s, e) => {
                System.Diagnostics.Debug.WriteLine("OpticPage Loaded 이벤트 시작");
                
                // Manager를 통해 초기화
                dataTableManager.CreateCustomTable();
                zoneButtonManager.CreateZoneButtons();
                dataTableManager.InitializeWadComboBox();
                monitorManager.InitializeMonitorArea(isDarkMode);
                ApplyLanguage(); // 초기 언어 적용 (View 전용)
                
                System.Diagnostics.Debug.WriteLine("기본 초기화 완료, 그래프 탭 활성화 시작");
                
                // GRAPH와 MONITOR 탭 활성화 (초기화)
                ActivateGraphAndMonitorTabs();
                
                // 페이지 로드 후 기존 데이터 복원 (추가 보장)
                Dispatcher.BeginInvoke(new Action(() => {
                    System.Diagnostics.Debug.WriteLine("페이지 로드 후 기존 Graph 데이터 복원 시도");
                    graphManager.RestoreExistingGraphData();
                }), DispatcherPriority.Loaded);
                
                System.Diagnostics.Debug.WriteLine("OpticPage Loaded 이벤트 완료");
            };
        }
        
        /// <summary>
        /// Manager 인스턴스 초기화
        /// </summary>
        private void InitializeManagers()
        {
            // UI 요소 가져오기
            var dataTableGrid = this.FindName("DataTableGrid") as Grid;
            var wadComboBox = this.FindName("WadComboBox") as ComboBox;
            var graphContent = this.FindName("GraphContent") as Grid;
            var graphScrollViewer = this.FindName("GraphScrollViewer") as ScrollViewer;
            var zoneButtonsPanel = this.FindName("ZoneButtonsPanel") as StackPanel;
            var monitorGrid = this.FindName("MonitorGrid") as Grid;
            var judgmentStatusContainer = this.FindName("JudgmentStatusContainer") as Grid;
            
            
            // Manager 생성 (필요한 UI 요소만 전달)
            dataTableManager = new OpticDataTableManager(dataTableGrid, wadComboBox, viewModel);
            // GraphManager 생성
            if (graphContent != null && graphScrollViewer != null)
            {
                graphManager = new GraphManager(graphContent, graphScrollViewer);
            }
            // MonitorGrid가 존재하는 경우에만 MonitorManager 생성
            if (monitorGrid != null)
            {
                monitorManager = new MonitorManager(monitorGrid, this);
            }
            // JudgmentStatusManager 생성
            if (judgmentStatusContainer != null)
            {
                var statusTextBlocks = new Dictionary<string, (TextBlock quantity, TextBlock rate)>
                {
                    ["Total"] = (this.FindName("TotalQuantity") as TextBlock, this.FindName("TotalRate") as TextBlock),
                    ["OK"] = (this.FindName("OKQuantity") as TextBlock, this.FindName("OKRate") as TextBlock),
                    ["R/J"] = (this.FindName("RJQuantity") as TextBlock, this.FindName("RJRate") as TextBlock),
                    ["PTN"] = (this.FindName("PTNQuantity") as TextBlock, this.FindName("PTNRate") as TextBlock)
                };
                judgmentStatusManager = new JudgmentStatusManager(statusTextBlocks, judgmentStatusContainer, this, InspectionType.OPTIC);
            }
            // ZoneButtonManager 생성
            if (zoneButtonsPanel != null)
            {
                zoneButtonManager = new ZoneButtonManager(
                    zoneButtonsPanel,
                    (zone) => viewModel.CurrentZone = zone,
                    () => viewModel.CurrentZone,
                    () => {
                        // RESET 시 초기화
                        viewModel.ClearGraphData(graphManager);
                        judgmentStatusManager?.ResetCounters(); // 카운터 초기화는 Manager가 처리
                    }
                );
                zoneButtonManager.SetDataTableManager(dataTableManager);
            }
            seqExecutor = new OpticSeqExecutor(UpdateGraphDisplay, dataTableManager, viewModel, graphManager);
                       
            // 현재 다크모드 상태 가져오기 (MainWindow에서)
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    bool currentDarkMode = mainWindow.IsDarkMode;
                    this.isDarkMode = currentDarkMode;
                    
                    // 모든 Manager에 다크모드 상태 전달
                    graphManager?.SetDarkMode(currentDarkMode);
                    dataTableManager?.SetDarkMode(currentDarkMode);
                    zoneButtonManager?.SetDarkMode(currentDarkMode);
                    monitorManager?.SetDarkMode(currentDarkMode);
                    judgmentStatusManager?.SetDarkMode(currentDarkMode);
                    
                    System.Diagnostics.Debug.WriteLine($"페이지 초기 다크모드 상태 설정: {currentDarkMode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"다크모드 상태 가져오기 오류: {ex.Message}");
            }
            
            System.Diagnostics.Debug.WriteLine("모든 Manager 초기화 완료");
        }

        /// <summary>
        /// GRAPH와 MONITOR 탭을 페이지 로드 시 활성화 및 그래프 영역 초기화
        /// </summary>
        private void ActivateGraphAndMonitorTabs()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ActivateGraphAndMonitorTabs 시작");
                
                var graphTabBtn = FindName("GraphTab") as Button;
                var monitorTabBtn = FindName("MonitorTab") as Button;
                var totalTabBtn = FindName("TotalTab") as Button;
                
                System.Diagnostics.Debug.WriteLine($"탭 버튼 찾기 결과: GraphTab={graphTabBtn != null}, MonitorTab={monitorTabBtn != null}, TotalTab={totalTabBtn != null}");
                
                // 모든 탭 버튼이 존재하는지 확인
                if (graphTabBtn != null && monitorTabBtn != null && totalTabBtn != null)
                {
                    // 모든 탭 활성화 (스타일 적용)
                    graphTabBtn.IsEnabled = true;
                    monitorTabBtn.IsEnabled = true;
                    totalTabBtn.IsEnabled = true;
                    
                    System.Diagnostics.Debug.WriteLine("GRAPH와 MONITOR 탭이 활성화되었습니다.");
                    
                    // 그래프 영역 초기화 (Manager를 통해)
                    graphManager.InitializeGraphArea();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("탭 버튼을 찾을 수 없습니다.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"탭 활성화 오류: {ex.Message}");
            }
        }

        // === Graph 관련 메서드 (Manager로 위임) ===
        // CreateZoneWithJudgmentRows, CreateZoneGraphSection은 GraphManager에서 처리됩니다.


        /// <summary>
        /// Monitor 영역 초기화 (Manager로 위임)
        /// </summary>
        private void InitializeMonitorArea(bool darkMode)
        {
            monitorManager.InitializeMonitorArea(darkMode);
        }

        /// <summary>
        /// Monitor 로그 수신 이벤트 (Manager로 위임)
        /// </summary>
        private void OnMonitorLogReceived(int zoneIndex, string text)
        {
            monitorManager.OnMonitorLogReceived(zoneIndex, text);
        }

        /// <summary>
        /// Monitor 상태 표시기 업데이트 (Manager로 위임)
        /// </summary>
        private void UpdateStatusIndicator(int zoneIndex, string text)
        {
            monitorManager.UpdateStatusIndicator(zoneIndex, text);
        }

        /// <summary>
        /// Monitor에 텍스트 추가 (Manager로 위임)
        /// </summary>
        private void AppendMonitor(int zoneIndex, string line)
        {
            monitorManager.AppendMonitor(zoneIndex, line);
        }

        /// <summary>
        /// 다크모드 설정 (모든 Manager에 전파)
        /// </summary>
        public void SetDarkMode(bool isDarkMode)
        {
            // ViewModel에 다크모드 상태 전달
            if (viewModel != null)
            {
                viewModel.IsDarkMode = isDarkMode;
            }
            
            // ThemeManager로 다크모드 적용
            ThemeManager.UpdateDynamicColors(this, isDarkMode);
            
            // 각 Manager에 다크모드 전파
            dataTableManager?.SetDarkMode(isDarkMode);
            zoneButtonManager?.SetDarkMode(isDarkMode);
            monitorManager?.SetDarkMode(isDarkMode);
            graphManager?.SetDarkMode(isDarkMode);
            judgmentStatusManager?.SetDarkMode(isDarkMode);
            
            // 클래스 변수에 다크모드 상태 저장
            this.isDarkMode = isDarkMode;
            
            System.Diagnostics.Debug.WriteLine($"다크모드 설정 완료: {isDarkMode}");
        }

        // === 데이터 테이블 관련 메서드 ===

        /// <summary>
        /// 커스텀 테이블 생성 (Manager로 위임)
        /// </summary>
        private void CreateCustomTable()
        {
            dataTableManager.CreateCustomTable();
        }

        // CreateHeaderRow와 CreateDataRows는 dataTableManager에서 처리됩니다.

        /// <summary>
        /// Zone 버튼 생성 (Manager로 위임)
        /// </summary>
        private void CreateZoneButtons()
        {
            zoneButtonManager.CreateZoneButtons();
        }

        private void GraphTab_Click(object sender, RoutedEventArgs e)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            var graphTabBtn = FindName("GraphTab") as Button;
            var monitorTabBtn = FindName("MonitorTab") as Button;
            var totalTabBtn = FindName("TotalTab") as Button;
            var monitorContent = FindName("MonitorContent") as UIElement;
            var graphScrollViewer = FindName("GraphScrollViewer") as ScrollViewer;
            var graphContent = FindName("GraphContent") as Grid;
            var totalContent = FindName("TotalContent") as UIElement;

            // 스타일 변경 (즉시 적용 - 우선순위 높음)
            if (graphTabBtn != null) 
            {
                graphTabBtn.Style = (Style)FindResource("ActiveTabButtonStyle");
                //graphTabBtn.UpdateLayout(); // 강제 렌더링
            }
            if (monitorTabBtn != null) monitorTabBtn.Style = (Style)FindResource("TabButtonStyle");
            if (totalTabBtn != null) totalTabBtn.Style = (Style)FindResource("TabButtonStyle");

            // Visibility 변경 (즉시 적용)
            if (monitorContent != null) monitorContent.Visibility = Visibility.Collapsed;
            if (graphScrollViewer != null) 
            {
                graphScrollViewer.Visibility = Visibility.Visible;
               // graphScrollViewer.UpdateLayout(); // 강제 렌더링
            }
            if (totalContent != null) totalContent.Visibility = Visibility.Collapsed;
            
            // 그래프 영역 다크모드 적용
            graphManager?.SetDarkMode(this.isDarkMode);
            
            sw.Stop();
            System.Diagnostics.Debug.WriteLine($"[성능] 탭 전환 완료: {sw.ElapsedMilliseconds}ms");

            // Graph 탭 활성화 시 데이터 포인트 업데이트
            try
            {
                var graphSw = System.Diagnostics.Stopwatch.StartNew();
                
                if (graphContent != null)
                {
                    // GraphManager에서 그래프 데이터 가져와서 포인트 업데이트
                    var graphDataPoints = graphManager?.GetDataPoints();
                    if (graphDataPoints != null && graphDataPoints.Count > 0)
                    {
                        UpdateGraphDisplay(graphDataPoints);
                        graphSw.Stop();
                        System.Diagnostics.Debug.WriteLine($"[성능] 그래프 데이터 업데이트 완료: {graphSw.ElapsedMilliseconds}ms ({graphDataPoints.Count}개 포인트)");
                    }
                    else
                    {
                        // 데이터가 없어도 복원 시도 (혹시 놓친 데이터가 있을 수 있음)
                        System.Diagnostics.Debug.WriteLine("Graph 데이터가 없음 - 복원 시도");
                        graphManager.RestoreExistingGraphData();
                        
                        graphSw.Stop();
                        System.Diagnostics.Debug.WriteLine($"[성능] 그래프 영역 표시 (데이터 없음): {graphSw.ElapsedMilliseconds}ms");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Graph 탭 클릭 오류: {ex.Message}");
            }
        }


        /// <summary>
        /// TOTAL 탭 클릭 이벤트
        /// </summary>
        private void TotalTab_Click(object sender, RoutedEventArgs e)
        {
            var graphTabBtn = FindName("GraphTab") as Button;
            var monitorTabBtn = FindName("MonitorTab") as Button;
            var totalTabBtn = FindName("TotalTab") as Button;
            var monitorContent = FindName("MonitorContent") as UIElement;
            var graphScrollViewer = FindName("GraphScrollViewer") as ScrollViewer;
            var totalContent = FindName("TotalContent") as UIElement;

            if (totalTabBtn != null) totalTabBtn.Style = (Style)FindResource("ActiveTabButtonStyle");
            if (monitorTabBtn != null) monitorTabBtn.Style = (Style)FindResource("TabButtonStyle");
            if (graphTabBtn != null) graphTabBtn.Style = (Style)FindResource("TabButtonStyle");

            if (monitorContent != null) monitorContent.Visibility = Visibility.Collapsed;
            if (graphScrollViewer != null) graphScrollViewer.Visibility = Visibility.Collapsed;
            if (totalContent != null) totalContent.Visibility = Visibility.Visible;
        }

        private void MonitorTab_Click(object sender, RoutedEventArgs e)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            
            var graphTabBtn = FindName("GraphTab") as Button;
            var monitorTabBtn = FindName("MonitorTab") as Button;
            var totalTabBtn = FindName("TotalTab") as Button;
            var monitorContent = FindName("MonitorContent") as UIElement;
            var graphScrollViewer = FindName("GraphScrollViewer") as ScrollViewer;
            var totalContent = FindName("TotalContent") as UIElement;

            // 스타일 변경 (즉시 적용 - 우선순위 높음)
            if (monitorTabBtn != null) 
            {
                monitorTabBtn.Style = (Style)FindResource("ActiveTabButtonStyle");
                monitorTabBtn.UpdateLayout(); // 강제 렌더링
            }
            if (graphTabBtn != null) graphTabBtn.Style = (Style)FindResource("TabButtonStyle");
            if (totalTabBtn != null) totalTabBtn.Style = (Style)FindResource("TabButtonStyle");

            // Visibility 변경 (즉시 적용)
            if (monitorContent != null) 
            {
                monitorContent.Visibility = Visibility.Visible;
                monitorContent.UpdateLayout(); // 강제 렌더링
            }
            if (graphScrollViewer != null) graphScrollViewer.Visibility = Visibility.Collapsed;
            if (totalContent != null) totalContent.Visibility = Visibility.Collapsed;
            
            sw.Stop();
            System.Diagnostics.Debug.WriteLine($"[성능] MONITOR 탭 전환 완료: {sw.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// WAD ComboBox 초기화 (Manager로 위임)
        /// </summary>
        private void InitializeWadComboBox()
        {
            dataTableManager.InitializeWadComboBox();
        }

        /// <summary>
        /// WAD ComboBox 선택 변경 이벤트 (Manager로 위임)
        /// </summary>
        private void WadComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dataTableManager.OnWadComboBoxSelectionChanged(sender, e);
        }

        /// <summary>
        /// RESET 버튼 클릭 이벤트 (Manager로 위임)
        /// </summary>
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            zoneButtonManager.OnResetButtonClick(sender, e);
        }

        // ClearGraphData, ClearAllGraphCanvases, ShowResetCompleteMessage는 각 Manager에서 처리됩니다.

        // === Helper 메서드들 (dataTableManager로 이동됨) ===
        // GetWadAngle, GetWadArrayIndex, GetPatternArrayIndex는 dataTableManager에서 처리됩니다.

        // UpdateDataForWad, GenerateDataFromStruct, GetActualStructData, InitializeZoneTestStates는 각 Manager에서 처리됩니다.

        // 테스트 시작 메서드 (UI 스레드 블로킹 방지)
        /// <summary>
        /// 테스트 시작 (seqExecutor로 위임)
        /// </summary>
        public void StartTest()
        {
            seqExecutor?.StartTest();
        }

        // StartTestAsync, CreateAllResultLogs, ExecuteSeqForZone은 seqExecutor에서 처리됩니다.
        
        /// <summary>
        /// 테스트 중지 (seqExecutor로 위임)
        /// </summary>
        public void StopTest()
        {
            seqExecutor?.StopTest();
        }
        
        /// <summary>
        /// Zone 테스트 완료 상태 설정 (seqExecutor로 위임)
        /// </summary>
        public void SetZoneTestCompleted(int zoneIndex, bool completed)
        {
            // seqExecutor null 체크
            if (seqExecutor != null)
            {
                seqExecutor.SetZoneTestCompleted(zoneIndex, completed);
            }
            
            // View의 zoneTestCompleted도 동기화 (null 체크 추가)
            if (zoneTestCompleted != null && zoneIndex >= 0 && zoneIndex < zoneTestCompleted.Length)
            {
                zoneTestCompleted[zoneIndex] = completed;
            }
        }
        
        /// <summary>
        /// Zone 테스트 완료 상태 확인 (seqExecutor로 위임)
        /// </summary>
        public bool IsZoneTestCompleted(int zoneIndex)
        {
            return seqExecutor?.IsZoneTestCompleted(zoneIndex) ?? false;
        }
        

        // ClearMeasurementValues, GenerateDefaultEmptyData는 dataTableManager에서 처리됩니다.

        /// <summary>
        /// OpticPage에 언어 적용 (LanguageHelper 사용)
        /// </summary>
        public void ApplyLanguage()
        {
            try
            {
                // 뒤로가기 버튼 (Button 내부에 TextBlock이 있는 구조)
                LanguageHelper.ApplyToButtonWithTextBlock(this, "BackButton", "OpticPage.Back");

                // 데이터 테이블 관련
                LanguageHelper.ApplyToTextBlocks(this,
                    ("DataTableTitle", "OpticPage.CharacteristicDataTable"),
                    ("WadLabel", "OpticPage.WAD")
                );

                // RESET 버튼
                LanguageHelper.ApplyToButton(this, "ResetButton", "OpticPage.Reset");

                // 컨트롤 패널 버튼들 (일괄 적용)
                LanguageHelper.ApplyToButtons(this,
                    ("SettingButton", "OpticPage.Setting"),
                    ("PathButton", "OpticPage.Path"),
                    ("StartButton", "OpticPage.Start"),
                    ("StopButton", "OpticPage.Stop"),
                    ("ChartButton", "OpticPage.Chart"),
                    ("ReportButton", "OpticPage.Report"),
                    ("ExitButton", "OpticPage.Exit")
                );

                // 특성 판정 현황 관련
                LanguageHelper.ApplyToTextBlocks(this,
                    ("JudgmentStatusTitle", "OpticPage.CharacteristicJudgmentStatus"),
                    ("QuantityHeader", "OpticPage.Quantity"),
                    ("OccurrenceRateHeader", "OpticPage.OccurrenceRate")
                );

                // 컨트롤 패널 제목
                LanguageHelper.ApplyToTextBlock(this, "ControlPanelTitle", "OpticPage.ControlPanel");

                System.Diagnostics.Debug.WriteLine($"OpticPage 언어 적용 완료: {LanguageManager.CurrentLanguage}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpticPage 언어 적용 오류: {ex.Message}");
            }
        }

        #region 판정 현황 표 업데이트 (Manager로 위임)
        /// <summary>
        /// 판정 현황 표의 특정 행 업데이트 (OpticJudgmentStatusManager로 위임)
        /// </summary>
        /// <param name="rowName">행 이름 (Total, OK, R/J, PTN)</param>
        /// <param name="quantity">수량</param>
        /// <param name="rate">발생률</param>
        public void UpdateJudgmentStatusRow(string rowName, string quantity, string rate)
        {
            judgmentStatusManager?.UpdateJudgmentStatusRow(rowName, quantity, rate);
        }
        #endregion

        #region 그래프 영역 업데이트 (OpticGraphManager로 위임)
        /// <summary>
        /// 그래프 표시 업데이트 (OpticGraphManager로 위임)
        /// </summary>
        public void UpdateGraphDisplay(List<GraphManager.GraphDataPoint> dataPoints)
        {
            graphManager?.UpdateGraphDisplay(dataPoints);
        }
        #endregion

        #region ViewModel 이벤트 구독 (순환 참조 제거)
        
        /// <summary>
        /// ViewModel 이벤트 구독
        /// </summary>
        private void SubscribeToViewModelEvents()
        {
            if (viewModel == null) return;

            // 테스트 시작 요청 이벤트
            viewModel.TestStartRequested += OnTestStartRequested;

            // 판정 현황 업데이트 요청 이벤트
            viewModel.JudgmentStatusUpdateRequested += OnJudgmentStatusUpdateRequested;

            // 그래프 표시 업데이트 요청 이벤트
            viewModel.GraphDisplayUpdateRequested += OnGraphDisplayUpdateRequested;

            System.Diagnostics.Debug.WriteLine("ViewModel 이벤트 구독 완료");
        }

        /// <summary>
        /// 테스트 시작 요청 이벤트 핸들러
        /// </summary>
        private void OnTestStartRequested(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("테스트 시작 요청 이벤트 받음");
            StartTest();
        }

        /// <summary>
        /// 판정 현황 업데이트 요청 이벤트 핸들러
        /// </summary>
        private void OnJudgmentStatusUpdateRequested(object sender, JudgmentStatusUpdateEventArgs e)
        {
            // 판정 결과 전달 방식 (새 방식)
            if (!string.IsNullOrEmpty(e.Judgment))
            {
                // JudgmentStatusManager가 카운터 관리
                judgmentStatusManager.IncrementCounter(e.Judgment);
            }
            // 레거시 방식 (행별 업데이트)
            else if (!string.IsNullOrEmpty(e.RowName))
            {
                UpdateJudgmentStatusRow(e.RowName, e.Quantity, e.Rate);
            }
        }

        /// <summary>
        /// 그래프 표시 업데이트 요청 이벤트 핸들러
        /// </summary>
        private void OnGraphDisplayUpdateRequested(object sender, GraphDisplayUpdateEventArgs e)
        {
            UpdateGraphDisplay(e.DataPoints);
        }

        #endregion

        #region Manager 접근자
        /// <summary>
        /// DataTableManager 반환 (SeqExecutor에서 사용)
        /// </summary>
        public OpticDataTableManager GetDataTableManager()
        {
            return dataTableManager;
        }
        #endregion
    }
}
